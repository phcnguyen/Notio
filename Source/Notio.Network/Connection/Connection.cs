﻿using Notio.Common.Connection;
using Notio.Common.Connection.Enums;
using Notio.Common.Logging.Interfaces;
using Notio.Common.Memory.Pools;
using Notio.Common.Models;
using Notio.Cryptography.Ciphers.Symmetric;
using Notio.Shared.Identification;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Notio.Network.Connection;

public sealed class Connection : IConnection, IDisposable
{
    private readonly Socket _socket;
    private readonly ILogger? _logger;
    private readonly ConnectionStreamManager _streamManager;
    private readonly CancellationTokenSource _ctokens = new();
    private readonly UniqueId _id = UniqueId.NewId(TypeId.Session);
    private readonly DateTimeOffset _connectedTimestamp = DateTimeOffset.UtcNow;

    private bool _disposed;

    public Connection(Socket socket, IBufferPool bufferAllocator, ILogger? logger = null)
    {
        _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        _logger = logger;
        _streamManager = new ConnectionStreamManager(socket, bufferAllocator, logger);

        _streamManager.OnDataReceived += OnDataReceived;
        _streamManager.OnNewPacketCached = () =>
        {
            OnProcessEvent?.Invoke(this, new ConnectionEventArgs(this));
        };
    }

    /// <inheritdoc />
    public string Id => _id.ToString(true);

    /// <inheritdoc />
    public byte[] EncryptionKey { get; set; } = [];

    /// <inheritdoc />
    public long PingTime => _streamManager.LastPingTime;

    /// <inheritdoc />
    public DateTimeOffset Timestamp => _connectedTimestamp;

    /// <inheritdoc />
    public event EventHandler<IConnectEventArgs>? OnCloseEvent;

    /// <inheritdoc />
    public event EventHandler<IConnectEventArgs>? OnProcessEvent;

    /// <inheritdoc />
    public event EventHandler<IConnectEventArgs>? OnPostProcessEvent;

    /// <inheritdoc />
    public Authoritys Authority { get; set; } = Authoritys.Guests;

    /// <inheritdoc />
    public ConnectionState State { get; set; } = ConnectionState.Connected;

    /// <inheritdoc />
    public string RemoteEndPoint
        => _socket.Connected ? _socket.RemoteEndPoint?.ToString() ?? "0.0.0.0" : "Disconnected";

    /// <inheritdoc />
    public byte[]? IncomingPacket
    {
        get
        {
            if (_streamManager.CacheIncomingPacket.TryGetValue(out var data))
                return data;
            return null;
        }
    }

    /// <inheritdoc />
    public void BeginReceive(CancellationToken cancellationToken = default)
        => _streamManager.BeginReceive(cancellationToken);

    /// <inheritdoc />
    public void Send(ReadOnlyMemory<byte> message)
    {
        if (this.State == ConnectionState.Authenticated)
        {
            try
            {
                message = Aes256.GcmMode.Encrypt(message, EncryptionKey);
            }
            catch
            {
                this.State = ConnectionState.Connected;
                return;
            }
        }

        if (_streamManager.Send(message.Span))
            OnPostProcessEvent?.Invoke(this, new ConnectionEventArgs(this));
    }

    /// <inheritdoc />
    public async Task SendAsync(byte[] message, CancellationToken cancellationToken = default)
    {
        if (this.State == ConnectionState.Authenticated)
        {
            try
            {
                message = Aes256.GcmMode.Encrypt(message, EncryptionKey).ToArray();
            }
            catch
            {
                this.State = ConnectionState.Connected;
                return;
            }
        }

        if (await _streamManager.SendAsync(message, cancellationToken))
            OnPostProcessEvent?.Invoke(this, new ConnectionEventArgs(this));
    }

    /// <inheritdoc />
    public void Close()
    {
        try
        {
            if (_socket.Connected && (!_socket.Poll(1000, SelectMode.SelectRead) || _socket.Available > 0))
            {
                return;
            }

            OnCloseEvent?.Invoke(this, new ConnectionEventArgs(this));
        }
        catch (Exception ex)
        {
            _logger?.Error(ex);
        }
    }

    /// <inheritdoc />
    public void Disconnect(string? reason = null)
    {
        try
        {
            if (_disposed) return;

            _ctokens.Cancel();
            this.State = ConnectionState.Disconnected;
            OnCloseEvent?.Invoke(this, new ConnectionEventArgs(this));
        }
        catch (Exception ex)
        {
            _logger?.Error(ex);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            Disconnect();
        }
        catch (Exception ex)
        {
            _logger?.Error(ex);
        }
        finally
        {
            _ctokens.Dispose();
            _streamManager.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    private ReadOnlyMemory<byte> OnDataReceived(ReadOnlyMemory<byte> data)
    {
        if (this.State == ConnectionState.Authenticated)
        {
            try
            {
                return Aes256.GcmMode.Decrypt(data, EncryptionKey);
            }
            catch
            {
                this.State = ConnectionState.Connected;
            }
        }

        return data;
    }
}