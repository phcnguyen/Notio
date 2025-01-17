﻿using Notio.Common.Connection;
using Notio.Common.Connection.Args;
using Notio.Common.Connection.Enums;
using Notio.Common.Memory;
using Notio.Cryptography;
using Notio.Infrastructure.Identification;
using Notio.Infrastructure.Time;
using Notio.Network.Connection.Args;
using Notio.Shared.Memory.Cache;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Notio.Network.Connection;

public class Connection : IConnection, IDisposable
{
    private const byte HEADER_LENGHT = 2;
    private const short KEY_RSA_SIZE = 4096;
    private const short KEY_RSA_SIZE_BYTES = KEY_RSA_SIZE / 8;

    private readonly UniqueId _id;
    private readonly Socket _socket;
    private readonly Lock _receiveLock;
    private readonly NetworkStream _stream;
    private readonly BinaryCache _cacheOutgoingPacket;
    private readonly ReaderWriterLockSlim _rwLockState;
    private readonly IBufferPool _bufferAllocator;
    private readonly DateTimeOffset _connectedTimestamp;
    private readonly FifoCache<byte[]> _cacheIncomingPacket;

    private byte[] _buffer;
    private bool _disposed;
    private long _lastPingTime;
    private byte[] _aes256Key;
    private Rsa4096? _rsa4096;
    private ConnectionState _state;
    private CancellationTokenSource _ctokens;

    /// <summary>
    /// Khởi tạo một đối tượng Connection mới.
    /// </summary>
    /// <param name="socket">Socket kết nối.</param>
    /// <param name="bufferAllocator">Bộ cấp phát bộ nhớ đệm.</param>
    public Connection(Socket socket, IBufferPool bufferAllocator)
    {
        _socket = socket;
        _receiveLock = new Lock();
        _stream = new NetworkStream(socket);
        _bufferAllocator = bufferAllocator;
        _id = UniqueId.NewId(TypeId.Session);
        _cacheOutgoingPacket = new BinaryCache(20);
        _cacheIncomingPacket = new FifoCache<byte[]>(20);
        _ctokens = new CancellationTokenSource();
        _rwLockState = new ReaderWriterLockSlim();
        _connectedTimestamp = DateTimeOffset.UtcNow;

        _disposed = false;
        _aes256Key = [];
        _state = ConnectionState.Connecting;
        _buffer = _bufferAllocator.Rent(1024); // byte
        _lastPingTime = (long)Clock.UnixTime.TotalMilliseconds;
    }

    /// <summary>
    /// Thời gian kết nối.
    /// </summary>
    public DateTimeOffset Timestamp => _connectedTimestamp;

    /// <summary>
    /// Khóa mã hóa AES 256.
    /// </summary>
    public byte[] EncryptionKey => _aes256Key;

    /// <summary>
    /// Thời gian ping cuối cùng.
    /// </summary>
    public long LastPingTime => _lastPingTime;

    /// <summary>
    /// Id.
    /// </summary>
    public string Id => _id.ToString(true);

    /// <summary>
    /// Điểm cuối từ xa của kết nối.
    /// </summary>
    public string RemoteEndPoint
    {
        get
        {
            return
                (_socket?.Connected ?? false)
                ? _socket.RemoteEndPoint?.ToString()
                ?? "0.0.0.0" : "Disconnected";
        }
    }

    /// <summary>
    /// Gói tin đến.
    /// </summary>
    public byte[] IncomingPacket
    {
        get
        {
            if (_cacheIncomingPacket.Count > 0)
                return _cacheIncomingPacket.GetValue();
            return [];
        }
    }

    /// <summary>
    /// Trạng thái kết nối.
    /// </summary>
    public ConnectionState State
    {
        get
        {
            _rwLockState.EnterReadLock();
            try
            {
                return _state;
            }
            finally
            {
                _rwLockState.ExitReadLock();
            }
        }
    }

    public event EventHandler<IErrorEventArgs>? OnErrorEvent;

    public event EventHandler<IConnctEventArgs>? OnProcessEvent;

    public event EventHandler<IConnctEventArgs>? OnCloseEvent;

    public event EventHandler<IConnctEventArgs>? OnPostProcessEvent;

    /// <summary>
    /// Bắt đầu nhận dữ liệu không đồng bộ.
    /// </summary>
    /// <param name="cancellationToken">Token hủy bỏ.</param>
    public void BeginReceive(CancellationToken cancellationToken = default)
    {
        if (_disposed) return;

        lock (_receiveLock)
        {
            if (!_socket.Connected || !_stream.CanRead) return;
        }

        if (cancellationToken != default)
        {
            _ctokens.Dispose();
            _ctokens = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }

        try
        {
            lock (_receiveLock)
            {
                _stream.ReadAsync(_buffer, 0, HEADER_LENGHT, _ctokens.Token)
                       .ContinueWith(OnReceiveCompleted, _ctokens.Token);
            }
        }
        catch (ObjectDisposedException ex)
        {
            OnErrorEvent?.Invoke(this, new ConnectionErrorEventArgs(ConnectionError.StreamClosed, ex.Message));
            return;
        }
        catch (Exception ex)
        {
            OnErrorEvent?.Invoke(this, new ConnectionErrorEventArgs(ConnectionError.ReadError, ex.Message));
        }
    }

    /// <summary>
    /// Đóng kết nối nhận dữ liệu.
    /// </summary>
    public void Close()
    {
        try
        {
            // TODO: Remove this connection from the pool.

            // Kiểm tra trạng thái kết nối thực tế của socket.
            if (_socket == null || !_socket.Connected || _socket.Poll(1000, SelectMode.SelectRead) && _socket.Available == 0)
            {
                // Đảm bảo stream được đóng nếu socket không còn kết nối.
                _stream?.Close();

                // Thông báo trước khi đóng socket.
                OnCloseEvent?.Invoke(this, new ConnectionEventArgs(this));
            }
        }
        catch (Exception ex)
        {
            // Log lỗi.
            OnErrorEvent?.Invoke(this, new ConnectionErrorEventArgs(ConnectionError.CloseError, ex.Message));
        }
    }

    public void Disconnect(string? reason = null)
    {
        try
        {
            _ctokens.Cancel();  // Hủy bỏ token khi kết nối đang chờ hủy
            this.CloseSocket();
        }
        catch (Exception ex)
        {
            OnErrorEvent?.Invoke(this, new ConnectionErrorEventArgs(ConnectionError.CloseError, ex.Message));
        }
        finally
        {
            _state = ConnectionState.Disconnected;
        }
    }

    public void Send(ReadOnlySpan<byte> message)
    {
        try
        {
            if (_state == ConnectionState.Authenticated)
            {
                using var memoryBuffer = Aes256.CtrMode.Encrypt(_aes256Key, message);
                message = memoryBuffer.Memory.Span;
            }

            Span<byte> key = stackalloc byte[10];
            message[..4].CopyTo(key);
            message[(message.Length - 5)..].CopyTo(key);

            if (!_cacheOutgoingPacket.TryGetValue(key, out ReadOnlyMemory<byte>? cachedData))
                _cacheOutgoingPacket.Add(key, message.ToArray());

            _stream.Write(message);
        }
        catch (Exception ex)
        {
            OnErrorEvent?.Invoke(this, new ConnectionErrorEventArgs(ConnectionError.SendError, ex.Message));
        }
    }

    public async Task SendAsync(byte[] message, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_state == ConnectionState.Authenticated)
                message = Aes256.CtrMode.Encrypt(_aes256Key, message).Memory.ToArray();

            Span<byte> key = stackalloc byte[10];
            message.AsSpan(0, 4).CopyTo(key);
            message.AsSpan(message.Length - 5).CopyTo(key);

            if (!_cacheOutgoingPacket.TryGetValue(key, out ReadOnlyMemory<byte>? cachedData))
                _cacheOutgoingPacket.Add(key, message);

            await _stream.WriteAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            OnErrorEvent?.Invoke(this, new ConnectionErrorEventArgs(ConnectionError.SendError, ex.Message));
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            this.Disconnect();

            _bufferAllocator.Return(_buffer);
            _buffer = [];
            _aes256Key = [];
        }
        catch (Exception ex)
        {
            OnErrorEvent?.Invoke(this, new ConnectionErrorEventArgs(ConnectionError.CloseError, ex.Message));
        }
        finally
        {
            _disposed = true;
            _ctokens.Dispose();
            _stream.Dispose();
            _socket.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    private void CloseSocket()
    {
        try
        {
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }
        catch (Exception ex)
        {
            OnErrorEvent?.Invoke(this, new ConnectionErrorEventArgs(ConnectionError.CloseError, ex.Message));
        }
    }

    private void UpdateState(ConnectionState newState)
    {
        _rwLockState.EnterWriteLock();
        try
        {
            _state = newState;
        }
        finally
        {
            _rwLockState.ExitWriteLock();
        }
    }

    private void ResizeBuffer(int newSize)
    {
        byte[] newBuffer = _bufferAllocator.Rent(newSize);
        Array.Copy(_buffer, newBuffer, _buffer.Length);
        _bufferAllocator.Return(_buffer);
        _buffer = newBuffer;
    }

    private async Task OnReceiveCompleted(Task<int> task)
    {
        if (task.IsCanceled || _disposed) return;

        try
        {
            int totalBytesRead = task.Result;
            ushort size = BitConverter.ToUInt16(_buffer, 0);

            if (size > _bufferAllocator.MaxBufferSize)
            {
                OnErrorEvent?.Invoke(this, new ConnectionErrorEventArgs(ConnectionError.DataTooLarge,
                    $"Data length ({size} bytes) exceeds the maximum allowed buffer size ({_bufferAllocator.MaxBufferSize} bytes)."));
                return;
            }

            if (size > _buffer.Length)
                this.ResizeBuffer(Math.Max(_buffer.Length * 2, size));

            while (totalBytesRead < size)
            {
                int bytesRead = await _stream.ReadAsync(_buffer.AsMemory(totalBytesRead, size - totalBytesRead), _ctokens.Token);
                if (bytesRead == 0) break;
                totalBytesRead += bytesRead;
            }

            if (!_ctokens.Token.IsCancellationRequested)
            {
                _lastPingTime = (long)Clock.UnixTime.TotalMilliseconds;
            }

            await this.HandleConnectionStateAsync(size, totalBytesRead);
            this.BeginReceive();
        }
        catch (Exception ex)
        {
            OnErrorEvent?.Invoke(this, new ConnectionErrorEventArgs(ConnectionError.ReadError, ex.Message));
        }
    }

    private async Task HandleConnectionStateAsync(ushort size, int totalBytesRead)
    {
        switch (_state)
        {
            case ConnectionState.Connecting:
                break;

            case ConnectionState.Connected:
                await this.HandleConnectedState(totalBytesRead, size);
                break;

            case ConnectionState.Authenticated:
                this.HandleAuthenticatedState(totalBytesRead);
                break;

            default:
                break;
        }
    }

    private async Task HandleConnectedState(int totalBytesRead, ushort size)
    {
        try
        {
            if (size < KEY_RSA_SIZE_BYTES) return;

            _rsa4096 = new Rsa4096(KEY_RSA_SIZE);
            _aes256Key = Aes256.GenerateKey();
            _rsa4096.ImportPublicKey(_buffer
                .Skip(Math.Max(0, totalBytesRead - KEY_RSA_SIZE_BYTES))
                .Take(KEY_RSA_SIZE_BYTES).ToArray());

            byte[] key = _rsa4096.Encrypt(_aes256Key);
            await _stream.WriteAsync(key, _ctokens.Token);

            this.UpdateState(ConnectionState.Connected);
        }
        catch (Exception ex)
        {
            HandleError(ConnectionError.AuthenticationError, ex.Message);
        }
    }

    private void HandleAuthenticatedState(int totalBytesRead)
    {
        try
        {
            _cacheIncomingPacket.Add(_buffer.Take(totalBytesRead).ToArray());
            this.OnProcessEvent?.Invoke(this, new ConnectionEventArgs(this));
        }
        catch (Exception ex)
        {
            HandleError(ConnectionError.DecryptionError, ex.Message);
        }
    }

    private void HandleError(ConnectionError error, string message)
    {
        this.OnErrorEvent?.Invoke(this, new ConnectionErrorEventArgs(error, message));
        this.UpdateState(ConnectionState.Connecting);
    }
}