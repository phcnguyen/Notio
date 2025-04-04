using Notio.Network.Configurations;
using Notio.Shared.Configuration;
using Notio.Shared.Memory.Caches;
using System;

namespace Notio.Network.Transport;

/// <summary>
/// Manages packet caching.
/// </summary>
internal sealed class TransportCache : IDisposable
{
    private static TransportCacheConfig Config => ConfigurationStore.Instance.Get<TransportCacheConfig>();

    /// <summary>
    /// Gets or sets the last ping time in milliseconds.
    /// </summary>
    public long LastPingTime { get; set; }

    /// <summary>
    /// Event triggered when a new packet is added to the cache.
    /// </summary>
    public event Action? PacketCached;

    /// <summary>
    /// Caches for outgoing packets.
    /// </summary>
    public readonly BinaryCache Outgoing = new(Config.Outgoing);

    /// <summary>
    /// Caches for incoming packets.
    /// </summary>
    public readonly FifoCache<ReadOnlyMemory<byte>> Incoming = new(Config.Incoming);

    /// <summary>
    /// Adds an outgoing packet to the cache.
    /// </summary>
    /// <param name="data">The packet data to be added.</param>
    public void PushOutgoing(ReadOnlyMemory<byte> data)
    {
        Span<byte> key = stackalloc byte[8];
        data.Span[0..4].CopyTo(key);
        data.Span[^4..].CopyTo(key[4..]);

        Outgoing.Add(key.ToArray(), data);
    }

    /// <summary>
    /// Adds an incoming packet to the cache.
    /// </summary>
    /// <param name="data">The received packet data to be added.</param>
    public void PushIncoming(ReadOnlyMemory<byte> data)
    {
        Incoming.Add(data);
        PacketCached?.Invoke();
    }

    /// <summary>
    /// Disposes the resources used by the <see cref="TransportCache"/> instance.
    /// </summary>
    public void Dispose()
    {
        Incoming.Clear();
        Outgoing.Clear();

        Incoming.Dispose();
        Outgoing.Dispose();
    }
}
