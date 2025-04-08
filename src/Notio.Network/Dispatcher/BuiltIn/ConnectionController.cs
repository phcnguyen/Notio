using Notio.Common.Attributes;
using Notio.Common.Compression;
using Notio.Common.Connection;
using Notio.Common.Constants;
using Notio.Common.Cryptography;
using Notio.Common.Logging;
using Notio.Common.Package;
using Notio.Common.Package.Enums;
using Notio.Common.Security;
using Notio.Network.Core;
using Notio.Network.Core.Packets;
using System;
using System.Runtime.CompilerServices;

namespace Notio.Network.Dispatcher.BuiltIn;

/// <summary>
/// Provides handlers for managing connection-level configuration commands, 
/// such as setting compression and encryption modes during the handshake phase.
/// This controller is designed to be used with Dependency Injection and supports logging.
/// </summary>
[PacketController]
public class ConnectionController(ILogger? logger)
{
    private readonly ILogger? _logger = logger;

    /// <summary>
    /// Handles a request to set the compression mode for the current connection.
    /// The mode is expected as the first byte of the packet's binary payload.
    /// </summary>
    /// <param name="packet">The incoming packet containing the compression mode.</param>
    /// <param name="connection">The active client connection.</param>
    /// <returns>A response packet indicating success or failure.</returns>
    [PacketEncryption(false)]
    [PacketTimeout(Timeouts.Instant)]
    [PacketPermission(PermissionLevel.Guest)]
    [PacketId((ushort)InternalProtocolCommand.SetCompressionMode)]
    public Memory<byte> SetCompressionMode(IPacket packet, IConnection connection)
        => SetMode<CompressionMode>(packet, connection);

    /// <summary>
    /// Handles a request to set the encryption mode for the current connection.
    /// The mode is expected as the first byte of the packet's binary payload.
    /// </summary>
    /// <param name="packet">The incoming packet containing the encryption mode.</param>
    /// <param name="connection">The active client connection.</param>
    /// <returns>A response packet indicating success or failure.</returns>
    [PacketEncryption(false)]
    [PacketTimeout(Timeouts.Instant)]
    [PacketPermission(PermissionLevel.Guest)]
    [PacketId((ushort)InternalProtocolCommand.SetEncryptionMode)]
    public Memory<byte> SetEncryptionMode(IPacket packet, IConnection connection)
        => SetMode<EncryptionMode>(packet, connection);

    private Memory<byte> SetMode<TEnum>(IPacket packet, IConnection connection)
        where TEnum : struct, Enum
    {
        if (packet.Type != PacketType.Binary)
        {
            _logger?.Debug("Invalid packet type [{0}] for SetMode<{1}> from {2}",
                packet.Type, typeof(TEnum).Name, connection.RemoteEndPoint);
            return PacketBuilder.String(PacketCode.PacketType);
        }

        if (packet.Payload.Length < 1)
        {
            _logger?.Debug("Missing payload byte in SetMode<{0}> from {1}",
                typeof(TEnum).Name, connection.RemoteEndPoint);
            return PacketBuilder.String(PacketCode.InvalidPayload);
        }

        byte value = packet.Payload.Span[0];

        if (!Enum.IsDefined(typeof(TEnum), value))
        {
            _logger?.Debug("Invalid enum value [{0}] in SetMode<{1}> from {2}",
                value, typeof(TEnum).Name, connection.RemoteEndPoint);
            return PacketBuilder.String(PacketCode.InvalidPayload);
        }

        TEnum enumValue = Unsafe.As<byte, TEnum>(ref value);

        if (typeof(TEnum) == typeof(CompressionMode))
            connection.ComMode = Unsafe.As<TEnum, CompressionMode>(ref enumValue);
        else if (typeof(TEnum) == typeof(EncryptionMode))
            connection.EncMode = Unsafe.As<TEnum, EncryptionMode>(ref enumValue);

        _logger?.Debug("Set {0} to [{1}] for {2}", typeof(TEnum).Name, value, connection.RemoteEndPoint);
        return PacketBuilder.String(PacketCode.Success);
    }
}
