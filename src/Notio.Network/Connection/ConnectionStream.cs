using Notio.Common.Logging;
using Notio.Common.Memory;
using Notio.Shared.Memory.Caches;
using Notio.Shared.Time;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Notio.Network.Connection;

/// <summary>
/// Manages the network stream and handles sending/receiving data with caching and logging.
/// </summary>
internal class ConnectionStream : IDisposable
{
    private readonly ILogger? _logger;
    private readonly NetworkStream _stream;
    private readonly IBufferPool _bufferPool;

    private byte[] _buffer;
    private bool _disposed;

    /// <summary>
    /// Event triggered when a new packet is added to the cache.
    /// </summary>
    public Action? PacketCached;

    /// <summary>
    /// Event triggered when the connection is disconnected.
    /// </summary>
    public Action? Disconnected;

    /// <summary>
    /// Gets the last ping time in milliseconds.
    /// </summary>
    public long LastPingTime { get; private set; }

    /// <summary>
    /// Caches for outgoing packets.
    /// </summary>
    public readonly BinaryCache CacheOutgoing = new(20);

    /// <summary>
    /// Caches for incoming packets.
    /// </summary>
    public readonly FifoCache<ReadOnlyMemory<byte>> CacheIncoming = new(40);

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionStream"/> class.
    /// </summary>
    /// <param name="socket">The socket.</param>
    /// <param name="bufferPool">The buffer pool.</param>
    /// <param name="logger">The logger (optional).</param>
    public ConnectionStream(Socket socket, IBufferPool bufferPool, ILogger? logger = null)
    {
        _logger = logger;
        _bufferPool = bufferPool;
        _buffer = _bufferPool.Rent();
        _stream = new NetworkStream(socket);
        _logger?.Debug("ConnectionStream initialized.");
    }

    /// <summary>
    /// Begins receiving data asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public void BeginReceive(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            _logger?.Debug("BeginReceive called on disposed ConnectionStream.");
            return;
        }

        try
        {
            _logger?.Debug("Starting asynchronous read operation.");
            _stream.ReadAsync(_buffer, 0, 2, cancellationToken)
                   .ContinueWith(async (task, state) =>
                   {
                       ConnectionStream self = (ConnectionStream)state!;
                       try
                       {
                           await self.OnReceiveCompleted(task, cancellationToken);
                       }
                       catch (IOException ex)
                       when (ex.InnerException is SocketException se &&
                             se.SocketErrorCode == SocketError.ConnectionReset)
                       {
                           self._logger?.Debug("Connection forcibly closed by remote host.");
                           self.Disconnected?.Invoke();
                       }
                       catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset)
                       {
                           self._logger?.Debug("Socket connection reset.");
                           self.Disconnected?.Invoke();
                       }
                       catch (Exception ex)
                       {
                           self._logger?.Error(ex);
                       }
                   }, this, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.Error(ex);
        }
    }

    /// <summary>
    /// Sends a data synchronously.
    /// </summary>
    /// <param name="data">The data to send.</param>
    /// <returns>true if the data was sent successfully; otherwise, false.</returns>
    public bool Send(ReadOnlyMemory<byte> data)
    {
        try
        {
            if (data.IsEmpty) return false;

            _logger?.Debug("Sending data synchronously.");
            _stream.Write(data.Span);

            CacheOutgoing.Add(data.Span[0..4].ToArray(), data);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.Error("Send failed", ex);
            return false;
        }
    }

    /// <summary>
    /// Sends a data asynchronously.
    /// </summary>
    /// <param name="data">The data to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous send operation. The value of the TResult parameter contains true if the data was sent successfully; otherwise, false.</returns>
    public async Task<bool> SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        try
        {
            if (data.IsEmpty) return false;

            _logger?.Debug("Sending data asynchronously.");
            await _stream.WriteAsync(data, cancellationToken);

            CacheOutgoing.Add(data.Span[0..4].ToArray(), data);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.Error("SendAsync failed", ex);
            return false;
        }
    }

    /// <summary>
    /// Handles the completion of data reception.
    /// </summary>
    /// <param name="task">The task representing the read operation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task OnReceiveCompleted(Task<int> task, CancellationToken cancellationToken)
    {
        if (task.IsCanceled || _disposed) return;

        try
        {
            int totalBytesRead = await task;
            if (totalBytesRead == 0)
            {
                _logger?.Debug("Client closed connection gracefully.");
                // Close the connection on the server when the client disconnects
                Disconnected?.Invoke();
                return;
            }

            if (totalBytesRead < 2) return;

            ushort size = BitConverter.ToUInt16(_buffer, 0);
            _logger?.Debug($"Received packet size: {size} bytes.");

            if (size > _bufferPool.MaxBufferSize)
            {
                _logger?.Error($"Data length ({size} bytes) exceeds the maximum allowed buffer size ({_bufferPool.MaxBufferSize} bytes).");
                return;
            }

            if (size > _buffer.Length)
            {
                _logger?.Debug("Renting a larger buffer to accommodate the packet size.");
                _bufferPool.Return(_buffer);
                _buffer = _bufferPool.Rent(size);
            }

            while (totalBytesRead < size)
            {
                int bytesRead = await _stream.ReadAsync(_buffer.AsMemory(totalBytesRead, size - totalBytesRead), cancellationToken);
                if (bytesRead == 0)
                {
                    _logger?.Debug("Client closed connection while reading.");
                    Disconnected?.Invoke();
                    return;
                }

                totalBytesRead += bytesRead;
            }

            if (totalBytesRead == size)
            {
                _logger?.Debug("Packet received completely.");
                CacheIncoming.Add(_buffer.AsMemory(0, totalBytesRead));

                LastPingTime = (long)Clock.UnixTime().TotalMilliseconds;
                PacketCached?.Invoke();
            }
            else
            {
                _logger?.Error("Incomplete packet received.");
            }
        }
        catch (IOException ex)
        when (ex.InnerException is SocketException se &&
              se.SocketErrorCode == SocketError.ConnectionReset)
        {
            _logger?.Debug("Connection forcibly closed by remote host.");
            Disconnected?.Invoke();
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset)
        {
            _logger?.Debug("Socket connection reset.");
            Disconnected?.Invoke();
        }
        catch (Exception ex)
        {
            _logger?.Error(ex);
        }
        finally
        {
            this.BeginReceive(cancellationToken);
        }
    }

    /// <summary>
    /// Disposes the resources used by the <see cref="ConnectionStream"/> instance.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the managed and unmanaged resources.
    /// </summary>
    /// <param name="disposing">If true, releases managed resources; otherwise, only releases unmanaged resources.</param>
    private void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _logger?.Debug("Disposing ConnectionStream.");
            _bufferPool.Return(_buffer);
            _stream.Dispose();

            CacheOutgoing.Clear();
            CacheIncoming.Clear();
        }

        _disposed = true;
    }
}
