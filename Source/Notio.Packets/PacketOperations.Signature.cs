﻿using Notio.Packets.Enums;
using Notio.Packets.Extensions;
using Notio.Packets.Metadata;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Notio.Packets;

/// <summary>
/// Lớp tĩnh <c>PacketSignature</c> cung cấp các phương thức để ký và xác minh gói dữ liệu.
/// </summary>
public static partial class PacketOperations
{
    // Kích thước của chữ ký, sử dụng SHA256 (32 byte)
    private const short SignatureSize = 32;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Packet SignPacket(this in Packet packet)
    {
        int dataSize = PacketSize.Header + packet.Payload.Length;
        byte[] signature;

        try
        {
            if (dataSize <= MaxStackAlloc)
            {
                Span<byte> stackBuffer = stackalloc byte[dataSize];
                WriteDataToBuffer(stackBuffer, packet, packet.Payload);
                signature = SHA256.HashData(stackBuffer.ToArray());
                return CreateSignedPacket(packet, signature);
            }

            byte[] dataToSign = Pool.Rent(dataSize);
            try
            {
                WriteDataToBuffer(dataToSign.AsSpan(0, dataSize), packet, packet.Payload);
                signature = SHA256.HashData(dataToSign.AsSpan(0, dataSize));
                return CreateSignedPacket(packet, signature);
            }
            finally
            {
                Pool.Return(dataToSign);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error occurred while signing the packet.", ex);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool VerifyPacket(this in Packet packet)
    {
        if (packet.Payload.Length < SignatureSize)
            return false;

        ReadOnlyMemory<byte> payloadWithoutSignature = packet.Payload[..^SignatureSize];
        ReadOnlyMemory<byte> receivedSignature = packet.Payload[^SignatureSize..];

        int dataSize = PacketSize.Header + payloadWithoutSignature.Length;
        byte[] computedSignature;

        try
        {
            if (dataSize <= MaxStackAlloc)
            {
                Span<byte> stackBuffer = stackalloc byte[dataSize];
                WriteDataToBuffer(stackBuffer, packet, payloadWithoutSignature);
                computedSignature = SHA256.HashData(stackBuffer.ToArray());
                return receivedSignature.Span.SequenceEqual(computedSignature);
            }

            byte[] dataToVerify = Pool.Rent(dataSize);
            try
            {
                WriteDataToBuffer(dataToVerify.AsSpan(0, dataSize), packet, payloadWithoutSignature);
                computedSignature = SHA256.HashData(dataToVerify.AsSpan(0, dataSize));
                return receivedSignature.Span.SequenceEqual(computedSignature);
            }
            finally
            {
                Pool.Return(dataToVerify);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error occurred while verifying the packet signature.", ex);
        }
    }

    /// <summary>
    /// Ghi dữ liệu vào buffer, bao gồm thông tin gói và payload.
    /// </summary>
    /// <param name="buffer">Buffer để ghi dữ liệu vào.</param>
    /// <param name="packet">Gói dữ liệu.</param>
    /// <param name="payload">Payload của gói dữ liệu.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteDataToBuffer(Span<byte> buffer, in Packet packet, ReadOnlyMemory<byte> payload)
    {
        BitConverter.TryWriteBytes(buffer[0..2], packet.Length + SignatureSize);
        buffer[2] = packet.Type;
        buffer[3] = packet.Flags.AddFlag(PacketFlags.IsSigned);
        BitConverter.TryWriteBytes(buffer[4..6], packet.Command);

        // Ghi payload vào buffer
        if (MemoryMarshal.TryGetArray(payload, out ArraySegment<byte> segment))
            Buffer.BlockCopy(segment.Array!, segment.Offset, buffer.ToArray(), PacketSize.Header, payload.Length);
        else
            payload.Span.CopyTo(buffer[(PacketSize.Header)..]);
    }

    /// <summary>
    /// Tạo gói dữ liệu đã được ký với chữ ký thêm vào cuối payload.
    /// </summary>
    /// <param name="packet">Gói dữ liệu gốc.</param>
    /// <param name="signature">Chữ ký HMACSHA256.</param>
    /// <returns>Gói dữ liệu đã được thêm chữ ký.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Packet CreateSignedPacket(in Packet packet, byte[] signature)
    {
        byte[] newPayload = new byte[packet.Payload.Length + SignatureSize];

        // Sao chép payload gốc vào newPayload
        if (MemoryMarshal.TryGetArray(packet.Payload, out ArraySegment<byte> segment))
            Buffer.BlockCopy(segment.Array!, segment.Offset, newPayload, 0, packet.Payload.Length);
        else
            packet.Payload.CopyTo(newPayload);

        // Thêm chữ ký vào cuối payload
        signature.CopyTo(newPayload, packet.Payload.Length);
        return packet.WithPayload(newPayload);
    }

    /// <summary>
    /// Loại bỏ chữ ký khỏi payload của gói dữ liệu.
    /// </summary>
    /// <param name="packet">Gói dữ liệu cần loại bỏ chữ ký.</param>
    /// <returns>Gói dữ liệu không có chữ ký.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Packet StripSignature(in Packet packet)
    {
        if (packet.Payload.Length < SignatureSize)
            return packet;  // Nếu không có chữ ký, trả về gói dữ liệu gốc

        return packet.WithPayload(packet.Payload[..^SignatureSize]);
    }
}