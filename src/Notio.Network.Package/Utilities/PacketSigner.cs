using Notio.Common.Package;
using Notio.Network.Package.Enums;
using Notio.Network.Package.Extensions;
using Notio.Network.Package.Metadata;
using System;
using System.Buffers;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Notio.Network.Package.Utilities;

[SkipLocalsInit]
internal class PacketSigner
{
    private const int MaxStackAlloc = 512;
    private static readonly ArrayPool<byte> Pool = ArrayPool<byte>.Shared;

    private const ushort SignatureSize = 32;

    /// <summary>
    /// Signs a data packet and appends the signature to the payload.
    /// </summary>
    /// <param name="packet">The data packet to sign.</param>
    /// <returns>The signed data packet, including the signature in the payload.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IPacket SignPacket(in IPacket packet)
    {
        int dataSize = PacketSize.Header + packet.Payload.Length;
        Span<byte> dataToSign = dataSize <= MaxStackAlloc
            ? stackalloc byte[dataSize]
            : Pool.Rent(dataSize);

        try
        {
            // Write the header with the original length
            WriteHeader(dataToSign, packet, originalLength: packet.Length);
            packet.Payload.Span.CopyTo(dataToSign[PacketSize.Header..]);

            // Compute the signature
            byte[] signature = SHA256.HashData(dataToSign);
            byte[] newPayload = new byte[packet.Payload.Length + SignatureSize];

            packet.Payload.Span.CopyTo(newPayload);
            signature.CopyTo(newPayload, packet.Payload.Length);

            return new Packet(
                packet.Type,
                packet.Flags.AddFlag(PacketFlags.IsSigned),
                packet.Priority,
                packet.Command,
                newPayload
            );
        }
        finally
        {
            if (dataSize > MaxStackAlloc)
                Pool.Return(dataToSign.ToArray());
        }
    }

    /// <summary>
    /// Verifies the validity of a data packet, including signature verification.
    /// </summary>
    /// <param name="packet">The data packet to verify.</param>
    /// <returns>Returns true if the packet is valid and the signature is correct; false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool VerifyPacket(in IPacket packet)
    {
        if (!packet.Flags.HasFlag(PacketFlags.IsSigned))
            return false;

        int payloadLengthWithoutSignature = Math.Max(0, packet.Payload.Length - SignatureSize);

        if (payloadLengthWithoutSignature <= 0)
            return false; // Invalid payload

        ReadOnlySpan<byte> payload = packet.Payload.Span[..payloadLengthWithoutSignature];
        ReadOnlySpan<byte> storedSignature = packet.Payload.Span[payloadLengthWithoutSignature..];

        int dataSize = PacketSize.Header + payloadLengthWithoutSignature;
        Span<byte> dataToVerify = dataSize <= MaxStackAlloc
            ? stackalloc byte[dataSize]
            : Pool.Rent(dataSize);

        try
        {
            // Write header with original length minus signature
            WriteHeader(dataToVerify, packet, packet.Length - SignatureSize);
            payload.CopyTo(dataToVerify[PacketSize.Header..]);

            // Compute the signature
            byte[] computedSignature = SHA256.HashData(dataToVerify);

            return storedSignature.SequenceEqual(computedSignature);
        }
        finally
        {
            if (dataSize > MaxStackAlloc)
                Pool.Return(dataToVerify.ToArray());
        }
    }

    /// <summary>
    /// Removes the signature from the payload of a data packet.
    /// </summary>
    /// <param name="packet">The data packet to remove the signature from.</param>
    /// <returns>The data packet without a signature.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IPacket StripSignature(in IPacket packet)
    {
        if (packet.Payload.Length <= SignatureSize)
            return packet; // If no signature, return the original packet

        // Remove the signature and return the packet without a signature
        return new Packet(
            packet.Type,
            packet.Flags.RemoveFlag(PacketFlags.IsSigned),
            packet.Priority,
            packet.Command,
            packet.Payload[..^SignatureSize]
        );
    }

    /// <summary>
    /// Writes the header information of a data packet into the buffer.
    /// </summary>
    /// <param name="buffer">The buffer to write the header data to.</param>
    /// <param name="packet">The data packet to write the header from.</param>
    /// <param name="originalLength">The original length of the data packet (excluding the signature).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteHeader(Span<byte> buffer, in IPacket packet, int originalLength)
    {
        // Using Unsafe to write quickly to memory
        Unsafe.WriteUnaligned(ref buffer[0], originalLength);
        buffer[2] = packet.Type;
        buffer[3] = packet.Flags.AddFlag(PacketFlags.IsSigned);
        Unsafe.WriteUnaligned(ref buffer[4], packet.Command);
    }
}
