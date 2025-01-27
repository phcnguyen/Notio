﻿using Notio.Common.Exceptions;
using Notio.Package.Models;
using Notio.Package.Serialization;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Notio.Package.Extensions;

/// <summary>
/// Cung cấp các phương thức mở rộng hiệu suất cao cho lớp Packet.
/// </summary>
[SkipLocalsInit]
public static partial class PackageExtensions
{
    private const int MaxStackAlloc = 512;
    private static readonly ArrayPool<byte> Pool = ArrayPool<byte>.Shared;

    /// <summary>
    /// Chuyển đổi Packet thành mảng byte.
    /// </summary>
    /// <exception cref="PackageException">Ném lỗi khi payload vượt quá giới hạn.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] ToByteArray(this in Packet @this)
    {
        if (@this.Payload.Length > ushort.MaxValue)
            throw new PackageException("Payload is too large.");

        int totalSize = PacketSize.Header + @this.Payload.Length;

        if (totalSize <= MaxStackAlloc)
        {
            Span<byte> stackBuffer = stackalloc byte[totalSize];
            PacketSerializer.WritePacketFast(stackBuffer, in @this);
            return stackBuffer.ToArray();
        }

        byte[] rentedArray = Pool.Rent(totalSize);
        try
        {
            PacketSerializer.WritePacketFast(rentedArray.AsSpan(0, totalSize), in @this);
            return rentedArray.AsSpan(0, totalSize).ToArray();
        }
        finally
        {
            Pool.Return(rentedArray, true);
        }
    }

    /// <summary>
    /// Tạo Packet từ mảng byte.
    /// </summary>
    /// <exception cref="PackageException">Ném lỗi khi dữ liệu không hợp lệ.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Packet FromByteArray(this ReadOnlySpan<byte> @this)
    {
        if (@this.Length < PacketSize.Header)
            throw new PackageException("Invalid length: @this is smaller than header size.");

        short length = MemoryMarshal.Read<short>(@this);
        if (length < PacketSize.Header || length > @this.Length)
            throw new PackageException($"Invalid length: {length}.");

        return PacketSerializer.ReadPacketFast(@this);
    }

    /// <summary>
    /// Tạo Packet từ mảng byte.
    /// </summary>
    /// <exception cref="PackageException">Ném lỗi khi dữ liệu không hợp lệ.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Packet FromByteArray(this byte[] @this)
    {
        if (@this.Length < PacketSize.Header)
            throw new PackageException("Invalid length: @this is smaller than header size.");

        short length = MemoryMarshal.Read<short>(@this);
        if (length < PacketSize.Header || length > @this.Length)
            throw new PackageException($"Invalid length: {length}.");

        return PacketSerializer.ReadPacketFast(@this);
    }

    /// <summary>
    /// Thử chuyển đổi Packet thành mảng byte.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryToByteArray(this in Packet @this, Span<byte> destination, out int bytesWritten)
    {
        if (@this.Payload.Length > ushort.MaxValue)
        {
            bytesWritten = 0;
            return false;
        }

        int totalSize = PacketSize.Header + @this.Payload.Length;
        if (destination.Length < totalSize)
        {
            bytesWritten = 0;
            return false;
        }

        try
        {
            PacketSerializer.WritePacketFast(destination[..totalSize], in @this);
            bytesWritten = totalSize;
            return true;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            bytesWritten = 0;
            return false;
        }
    }

    /// <summary>
    /// Thử tạo Packet từ mảng byte.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryFromByteArray(ReadOnlySpan<byte> source, out Packet packet)
    {
        if (source.Length < PacketSize.Header)
        {
            packet = default;
            return false;
        }

        try
        {
            // Validate @this length
            short length = MemoryMarshal.Read<short>(source);
            if (length < PacketSize.Header || length > source.Length)
            {
                packet = default;
                return false;
            }

            // Validate payload size
            int payloadSize = length - PacketSize.Header;
            if (payloadSize > ushort.MaxValue)
            {
                packet = default;
                return false;
            }

            packet = PacketSerializer.ReadPacketFast(source[..length]);
            return true;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            packet = default;
            return false;
        }
    }

    /// <summary>
    /// Trả về chuỗi dễ đọc của Packet.
    /// </summary>
    public static string ToString(this in Packet @this) =>
        $"Type: {@this.Type}, " +
        $"Flags: {@this.Flags}, " +
        $"Command: {@this.Command}, " +
        $"Payload: {BitConverter.ToString(@this.Payload.ToArray())}";
}