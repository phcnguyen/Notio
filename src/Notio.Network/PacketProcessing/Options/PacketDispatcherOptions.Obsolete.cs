using Notio.Common.Connection;
using Notio.Common.Package;
using System;

namespace Notio.Network.PacketProcessing.Options;

public sealed partial class PacketDispatcherOptions
{
    /// <summary>
    /// Configures packet compression and decompression for the packet dispatcher.
    /// </summary>
    /// <param name="compressionMethod">
    /// A function that compresses a packet before sending. The function receives an <see cref="IPacket"/>
    /// and returns the compressed <see cref="IPacket"/>. If this is null, compression will not be applied.
    /// </param>
    /// <param name="decompressionMethod">
    /// A function that decompresses a packet before processing. The function receives an <see cref="IPacket"/>
    /// and returns the decompressed <see cref="IPacket"/>. If this is null, decompression will not be applied.
    /// </param>
    /// <remarks>
    /// This method allows you to specify compression and decompression functions that will be applied to packets
    /// before they are sent or received. The compression and decompression methods are applied based on packet flags,
    /// which help determine if a packet should be compressed or decompressed. If either method is null, the corresponding
    /// compression or decompression step will be skipped.
    /// </remarks>
    /// <returns>
    /// The current <see cref="PacketDispatcherOptions"/> instance for method chaining.
    /// </returns>
    [Obsolete("Use WithTypedCompression and WithTypedDecompression for type-specific compression.")]
    public PacketDispatcherOptions WithPacketCompression
    (
        Func<IPacket, IConnection, IPacket>? compressionMethod,
        Func<IPacket, IConnection, IPacket>? decompressionMethod
    )
    {
        if (compressionMethod is not null) _compressionMethod = compressionMethod;
        if (decompressionMethod is not null) _decompressionMethod = decompressionMethod;

        _logger?.Debug("Packet compression configured.");
        return this;
    }

    /// <summary>
    /// Configures packet encryption and decryption for the packet dispatcher.
    /// </summary>
    /// <param name="encryptionMethod">
    /// A function that encrypts a packet before sending. The function receives an <see cref="IPacket"/> and a byte array (encryption key),
    /// and returns the encrypted <see cref="IPacket"/>.
    /// </param>
    /// <param name="decryptionMethod">
    /// A function that decrypts a packet before processing. The function receives an <see cref="IPacket"/> and a byte array (decryption key),
    /// and returns the decrypted <see cref="IPacket"/>.
    /// </param>
    /// <remarks>
    /// This method allows you to specify encryption and decryption functions that will be applied to packets
    /// before they are sent or received. The encryption and decryption methods will be invoked based on certain conditions,
    /// which are determined by the packet's flags (as checked by <see cref="IPacket.Flags"/>).
    /// Ensure that the encryption and decryption functions are compatible with the packet's structure.
    /// </remarks>
    /// <returns>
    /// The current <see cref="PacketDispatcherOptions"/> instance for method chaining.
    /// </returns>
    [Obsolete("Use WithTypedEncryption and WithTypedDecryption for type-specific encryption.")]
    public PacketDispatcherOptions WithPacketCrypto
    (
        Func<IPacket, IConnection, IPacket>? encryptionMethod,
        Func<IPacket, IConnection, IPacket>? decryptionMethod
    )
    {
        if (encryptionMethod is not null) _encryptionMethod = encryptionMethod;
        if (decryptionMethod is not null) _decryptionMethod = decryptionMethod;

        _logger?.Debug("Packet encryption configured.");
        return this;
    }

    /// <summary>
    /// Configures the packet serialization and deserialization methods.
    /// </summary>
    /// <param name="serializationMethod">
    /// A function that serializes a packet into a <see cref="Memory{Byte}"/>.
    /// </param>
    /// <param name="deserializationMethod">
    /// A function that deserializes a <see cref="Memory{Byte}"/> back into an <see cref="IPacket"/>.
    /// </param>
    /// <returns>
    /// The current <see cref="PacketDispatcherOptions"/> instance for method chaining.
    /// </returns>
    /// <remarks>
    /// This method allows customizing how packets are serialized before sending and deserialized upon receiving.
    /// </remarks>
    [Obsolete("Use WithTypedSerializer and WithTypedDeserializer for type-specific serialization.")]
    public PacketDispatcherOptions WithPacketSerialization
    (
        Func<IPacket, Memory<byte>>? serializationMethod,
        Func<ReadOnlyMemory<byte>, IPacket>? deserializationMethod
    )
    {
        if (serializationMethod is not null) SerializationMethod = serializationMethod;
        if (deserializationMethod is not null) DeserializationMethod = deserializationMethod;

        _logger?.Debug("Packet serialization configured.");
        return this;
    }
}
