using Nalix.Common.Package;
using Nalix.Common.Package.Enums;
using System;

namespace Nalix.Network.Package;

public readonly partial struct Packet : IPacketFactory<Packet>
{
    /// <inheritdoc />
    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    static Packet IPacketFactory<Packet>.Create(ushort id, PacketCode code)
        => new(id, code, []);

    /// <inheritdoc />
    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    static Packet IPacketFactory<Packet>.Create(
        ushort id, ushort code, byte type, byte flags, byte priority, Memory<byte> payload)
        => new(id, code, type, flags, priority, payload);

    /// <inheritdoc />
    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    static Packet IPacketFactory<Packet>.Create(
        ushort id, PacketCode code, PacketType type, PacketFlags flags, PacketPriority priority, Memory<byte> payload)
        => new(id, code, type, flags, priority, payload);
}
