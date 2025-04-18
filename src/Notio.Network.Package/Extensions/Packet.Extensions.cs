using Notio.Common.Package;
using Notio.Network.Package.Serialization;
using Notio.Network.Package.Utilities;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Notio.Network.Package.Extensions;

/// <summary>
/// Provides extension methods for working with IPacket instances.
/// </summary>
public static class PacketExtensions
{
    /// <summary>
    /// Verifies if the checksum in the byte array matches the computed checksum.
    /// </summary>
    /// <param name="packet">The byte array representing the packet.</param>
    /// <returns>Returns true if the checksum is valid; otherwise, false.</returns>
    public static bool IsValidChecksum(this byte[] packet)
        => PacketVerifier.IsValidChecksum(packet);

    /// <summary>
    /// Serializes a <see cref="IPacket"/> into a byte array.
    /// </summary>
    /// <param name="packet">The packet to serialize.</param>
    /// <returns>A byte array representing the serialized packet.</returns>
    public static byte[] Serialize(this in Packet packet)
        => PacketSerializer.Serialize(in packet);

    /// <summary>
    /// Deserializes a packet from a <see cref="ReadOnlySpan{T}"/> of bytes.
    /// </summary>
    /// <param name="data">The byte span containing packet data.</param>
    /// <returns>A <see cref="IPacket"/> instance created from the data.</returns>
    public static IPacket Deserialize(this ReadOnlySpan<byte> data)
        => PacketSerializer.Deserialize(data);

    /// <summary>
    /// Deserializes a packet from a <see cref="ReadOnlyMemory{T}"/> of bytes.
    /// </summary>
    /// <param name="data">The memory segment containing packet data.</param>
    /// <returns>A <see cref="IPacket"/> instance created from the data.</returns>
    public static IPacket Deserialize(this ReadOnlyMemory<byte> data)
        => PacketSerializer.Deserialize(data);

    /// <summary>
    /// Deserializes a packet from a byte array.
    /// </summary>
    /// <param name="data">The byte array containing packet data.</param>
    /// <returns>A <see cref="IPacket"/> instance created from the data.</returns>
    public static Packet Deserialize(this byte[] data)
        => PacketSerializer.Deserialize(data);

    /// <summary>
    /// Attempts to serialize a packet into a provided span of bytes.
    /// </summary>
    /// <param name="packet">The packet to serialize.</param>
    /// <param name="destination">The destination span where the serialized data will be stored.</param>
    /// <param name="bytesWritten">Outputs the Number of bytes written to the destination span.</param>
    /// <returns>True if serialization was successful; otherwise, false.</returns>
    public static bool TrySerialize(this in Packet packet, Span<byte> destination, [NotNullWhen(true)] out int bytesWritten)
        => PacketSerializer.TrySerialize(in packet, destination, out bytesWritten);

    /// <summary>
    /// Attempts to deserialize a packet from a span of bytes.
    /// </summary>
    /// <param name="source">The byte span containing packet data.</param>
    /// <param name="packet">Outputs the deserialized packet if successful.</param>
    /// <returns>True if deserialization was successful; otherwise, false.</returns>
    public static bool TryDeserialize(this ReadOnlySpan<byte> source, [NotNullWhen(true)] out Packet packet)
        => PacketSerializer.TryDeserialize(source, out packet);
}
