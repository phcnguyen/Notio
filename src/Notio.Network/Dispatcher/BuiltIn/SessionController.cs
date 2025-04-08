using Notio.Common.Connection;
using Notio.Common.Constants;
using Notio.Common.Package;
using Notio.Common.Package.Attributes;
using Notio.Common.Package.Enums;
using Notio.Common.Security;
using Notio.Network.Core;
using Notio.Network.Core.Packets;
using Notio.Network.Dispatcher.Dto;
using System;

namespace Notio.Network.Dispatcher.BuiltIn;

/// <summary>
/// Provides handlers for managing connection-level configuration commands, 
/// such as setting compression and encryption modes during the handshake phase.
/// This controller is designed to be used with Dependency Injection and supports logging.
/// </summary>
[PacketController]
public static class SessionController
{
    /// <summary>
    /// Handles a client-initiated disconnect request.
    /// </summary>
    [PacketEncryption(false)]
    [PacketTimeout(Timeouts.Short)]
    [PacketPermission(PermissionLevel.Guest)]
    [PacketRateGroup(nameof(SessionController))]
    [PacketId((ushort)InternalProtocolCommand.Disconnect)]
    [PacketRateLimit(MaxRequests = 2, LockoutDurationSeconds = 20)]
    public static void Disconnect(IPacket _, IConnection connection)
        => connection.Disconnect("Client disconnect request");

    /// <summary>
    /// Responds with the current connection status (compression, encryption, etc).
    /// </summary>
    [PacketEncryption(false)]
    [PacketTimeout(Timeouts.Short)]
    [PacketPermission(PermissionLevel.Guest)]
    [PacketRateGroup(nameof(SessionController))]
    [PacketId((ushort)InternalProtocolCommand.ConnectionStatus)]
    [PacketRateLimit(MaxRequests = 2, LockoutDurationSeconds = 20)]
    public static Memory<byte> GetCurrentModes(IPacket _, IConnection connection)
    {
        ConnectionStatusDto status = new()
        {
            ComMode = connection.ComMode,
            EncMode = connection.EncMode
        };

        return PacketBuilder.Json(PacketCode.Success, status, NotioJsonContext.Default.ConnectionStatusDto);
    }

    /// <summary>
    /// Returns the round-trip time (RTT) of the connection in milliseconds.
    /// </summary>
    [PacketEncryption(false)]
    [PacketTimeout(Timeouts.Short)]
    [PacketPermission(PermissionLevel.Guest)]
    [PacketRateGroup(nameof(SessionController))]
    [PacketId((ushort)InternalProtocolCommand.PingTime)]
    [PacketRateLimit(MaxRequests = 2, LockoutDurationSeconds = 20)]
    public static Memory<byte> GetPingTime(IPacket _, IConnection connection)
    {
        long rtt = connection.LastPingTime; // e.g. 32 (ms)
        return PacketBuilder.String(PacketCode.Success, $"Ping: {rtt} ms");
    }
}
