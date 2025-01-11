﻿using Notio.Packets.Metadata;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Notio.Packets.Extensions;

[SkipLocalsInit]
internal static unsafe class PacketSerializerUnsafe
{
    private const int MaxStackAllocSize = 1024; // Giới hạn kích thước stack alloc

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WritePacketUnsafe(byte* destination, in Packet packet)
    {
        if (destination == null)
            throw new PacketException("Destination pointer is null.");

        if ((uint)packet.Payload.Length > ushort.MaxValue)
            throw new PacketException("Payload size exceeds maximum allowed length.");

        int totalSize = PacketSize.Header + packet.Payload.Length;

        // Sử dụng Span để tối ưu hóa việc ghi header
        Span<byte> headerSpan = new(destination, PacketSize.Header);
        BinaryPrimitives.WriteInt16LittleEndian(headerSpan, (short)totalSize);
        headerSpan[PacketOffset.Type] = packet.Type;
        headerSpan[PacketOffset.Flags] = packet.Flags;
        BinaryPrimitives.WriteInt16LittleEndian(headerSpan[PacketOffset.Command..], packet.Command);

        // Copy payload với kiểm tra bounds
        if (!packet.Payload.IsEmpty)
        {
            fixed (byte* payloadPtr = packet.Payload.Span)
            {
                Buffer.MemoryCopy(
                    payloadPtr,
                    destination + PacketSize.Header,
                    packet.Payload.Length,
                    packet.Payload.Length
                );
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadPacketUnsafe(byte* source, out Packet packet, int length)
    {
        if (source == null)
            throw new PacketException("Source pointer is null.");

        if (length < PacketSize.Header)
            throw new PacketException("Packet size is smaller than the header size.");

        // Đọc độ dài gói tin an toàn hơn
        Span<byte> headerSpan = new(source, PacketSize.Header);
        short packetLength = BinaryPrimitives.ReadInt16LittleEndian(headerSpan);

        if ((uint)packetLength > length)
            throw new PacketException("Packet size exceeds the provided buffer length.");

        // Tính toán độ dài payload
        int payloadLength = packetLength - PacketSize.Header;

        // Tối ưu hóa việc cấp phát bộ nhớ cho payload
        byte[] payloadArray;
        if (payloadLength <= MaxStackAllocSize)
        {
            Span<byte> stackBuffer = stackalloc byte[payloadLength];
            fixed (byte* stackPtr = stackBuffer)
            {
                Buffer.MemoryCopy(
                    source + PacketSize.Header,
                    stackPtr,
                    payloadLength,
                    payloadLength
                );
            }
            payloadArray = stackBuffer.ToArray();
        }
        else
        {
            // Sử dụng ArrayPool cho payload lớn
            payloadArray = ArrayPool<byte>.Shared.Rent(payloadLength);
            try
            {
                fixed (byte* payloadPtr = payloadArray)
                {
                    Buffer.MemoryCopy(
                        source + PacketSize.Header,
                        payloadPtr,
                        payloadLength,
                        payloadLength
                    );
                }
                // Tạo bản sao để trả lại array pool
                var finalArray = new byte[payloadLength];
                Array.Copy(payloadArray, finalArray, payloadLength);
                ArrayPool<byte>.Shared.Return(payloadArray);
                payloadArray = finalArray;
            }
            catch
            {
                ArrayPool<byte>.Shared.Return(payloadArray);
                throw new PacketException("Failed to read the packet payload.");
            }
        }

        // Khởi tạo packet với tất cả thông tin
        packet = new Packet
        (
            type: headerSpan[PacketOffset.Type],
            flags: headerSpan[PacketOffset.Flags],
            command: BinaryPrimitives.ReadInt16LittleEndian(headerSpan[PacketOffset.Command..]),
            payload: new ReadOnlyMemory<byte>(payloadArray)
        );
    }
}