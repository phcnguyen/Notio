using Notio.Common.Connection;
using Notio.Common.Package;
using Notio.Network.Dispatcher.Options;
using System;
using System.Threading.Tasks;

namespace Notio.Network.Dispatcher;

/// <summary>
/// Ultra-high performance packet dispatcher with advanced dependency injection (DI) integration and async support.
/// This implementation uses reflection to map packet command IDs to controller methods.
/// </summary>
/// <remarks>
/// The <see cref="PacketDispatcher{TPacket}"/> processes incoming packets and invokes corresponding handlers
/// based on the registered command IDs. It logs errors and warnings when handling failures or unregistered commands.
/// </remarks>
/// <param name="options">
/// A delegate used to configure <see cref="PacketDispatcherOptions{TPacket}"/> before processing packets.
/// </param>
public sealed class PacketDispatcher<TPacket>(Action<PacketDispatcherOptions<TPacket>> options)
    : PacketDispatcherBase<TPacket>(options), IPacketDispatcher<TPacket> where TPacket : class
{
    /// <inheritdoc />
    public void HandlePacket(byte[]? packet, IConnection connection)
    {
        if (packet == null)
        {
            Logger?.Error($"No packet data provided from Ip: {connection.RemoteEndPoint}.");
            return;
        }

        HandlePacket(Options.Deserialization(packet), connection).Wait();
    }

    /// <inheritdoc />
    public void HandlePacket(ReadOnlyMemory<byte>? packet, IConnection connection)
    {
        if (packet == null)
        {
            Logger?.Error($"No packet data provided from Ip: {connection.RemoteEndPoint}.");
            return;
        }

        HandlePacket(Options.Deserialization(packet), connection).Wait();
    }

    /// <inheritdoc />
    public async Task HandlePacket(TPacket? packet, IConnection connection)
    {
        if (packet == null)
        {
            Logger?.Error($"No packet data provided from Ip: {connection.RemoteEndPoint}.");
            return;
        }

        if (packet is not IPacket ipacket)
        {
            Logger?.Error($"Invalid packet type: {packet.GetType().Name} from Ip: {connection.RemoteEndPoint}.");
            return;
        }

        ushort commandId = ipacket.Id;

        if (Options.TryGetPacketHandler(commandId, out var handler))
        {
            Logger?.Debug($"Invoking handler for Id: {commandId}");

            try
            {
                await handler!(packet, connection).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger?.Error($"Error handling packet with Id {commandId}: {ex.Message}", ex);
            }
        }
        else
        {
            Logger?.Warn($"No handler found for Id {commandId}");
        }
    }
}
