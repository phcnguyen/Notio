// Last updated: 2025-02-28 14:48:30 by phcnguyen
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Notio.Cryptography.Integrity;

/// <summary>
/// A high-performance CRC-8 implementation using polynomial x^8 + x^7 + x^6 + x^4 + x^2 + 1
/// </summary>
public static class Crc8
{
    // Pre-computed lookup table for faster CRC calculation
    private static readonly byte[] Table = [
        0x00, 0xD5, 0x7F, 0xAA, 0xFE, 0x2B, 0x81, 0x54, 0x29,
        0xFC, 0x56, 0x83, 0xD7, 0x02, 0xA8, 0x7D, 0x52, 0x87,
        0x2D, 0xF8, 0xAC, 0x79, 0xD3, 0x06, 0x7B, 0xAE, 0x04,
        0xD1, 0x85, 0x50, 0xFA, 0x2F, 0xA4, 0x71, 0xDB, 0x0E,
        0x5A, 0x8F, 0x25, 0xF0, 0x8D, 0x58, 0xF2, 0x27, 0x73,
        0xA6, 0x0C, 0xD9, 0xF6, 0x23, 0x89, 0x5C, 0x08, 0xDD,
        0x77, 0xA2, 0xDF, 0x0A, 0xA0, 0x75, 0x21, 0xF4, 0x5E,
        0x8B, 0x9D, 0x48, 0xE2, 0x37, 0x63, 0xB6, 0x1C, 0xC9,
        0xB4, 0x61, 0xCB, 0x1E, 0x4A, 0x9F, 0x35, 0xE0, 0xCF,
        0x1A, 0xB0, 0x65, 0x31, 0xE4, 0x4E, 0x9B, 0xE6, 0x33,
        0x99, 0x4C, 0x18, 0xCD, 0x67, 0xB2, 0x39, 0xEC, 0x46,
        0x93, 0xC7, 0x12, 0xB8, 0x6D, 0x10, 0xC5, 0x6F, 0xBA,
        0xEE, 0x3B, 0x91, 0x44, 0x6B, 0xBE, 0x14, 0xC1, 0x95,
        0x40, 0xEA, 0x3F, 0x42, 0x97, 0x3D, 0xE8, 0xBC, 0x69,
        0xC3, 0x16, 0xEF, 0x3A, 0x90, 0x45, 0x11, 0xC4, 0x6E,
        0xBB, 0xC6, 0x13, 0xB9, 0x6C, 0x38, 0xED, 0x47, 0x92,
        0xBD, 0x68, 0xC2, 0x17, 0x43, 0x96, 0x3C, 0xE9, 0x94,
        0x41, 0xEB, 0x3E, 0x6A, 0xBF, 0x15, 0xC0, 0x4B, 0x9E,
        0x34, 0xE1, 0xB5, 0x60, 0xCA, 0x1F, 0x62, 0xB7, 0x1D,
        0xC8, 0x9C, 0x49, 0xE3, 0x36, 0x19, 0xCC, 0x66, 0xB3,
        0xE7, 0x32, 0x98, 0x4D, 0x30, 0xE5, 0x4F, 0x9A, 0xCE,
        0x1B, 0xB1, 0x64, 0x72, 0xA7, 0x0D, 0xD8, 0x8C, 0x59,
        0xF3, 0x26, 0x5B, 0x8E, 0x24, 0xF1, 0xA5, 0x70, 0xDA,
        0x0F, 0x20, 0xF5, 0x5F, 0x8A, 0xDE, 0x0B, 0xA1, 0x74,
        0x09, 0xDC, 0x76, 0xA3, 0xF7, 0x22, 0x88, 0x5D, 0xD6,
        0x03, 0xA9, 0x7C, 0x28, 0xFD, 0x57, 0x82, 0xFF, 0x2A,
        0x80, 0x55, 0x01, 0xD4, 0x7E, 0xAB, 0x84, 0x51, 0xFB,
        0x2E, 0x7A, 0xAF, 0x05, 0xD0, 0xAD, 0x78, 0xD2, 0x07,
        0x53, 0x86, 0x2C, 0xF9
    ];

    /// <summary>
    /// Computes the CRC-8 checksum of the specified bytes
    /// </summary>
    /// <param name="bytes">The buffer to compute the CRC upon</param>
    /// <returns>The specified CRC</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte HashToByte(params byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            throw new ArgumentException("Bytes array cannot be null or empty", nameof(bytes));

        return HashToByte(bytes.AsSpan());
    }

    /// <summary>
    /// Computes the CRC-8 checksum of the specified bytes
    /// </summary>
    /// <param name="bytes">The buffer to compute the CRC upon</param>
    /// <returns>The specified CRC</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte HashToByte(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
            throw new ArgumentException("Bytes span cannot be empty", nameof(bytes));

        byte crc = 0xFF;

        // Process bytes in chunks when possible
        if (bytes.Length >= 8)
        {
            int unalignedBytes = bytes.Length % 8;
            int alignedLength = bytes.Length - unalignedBytes;

            for (int i = 0; i < alignedLength; i += 8)
            {
                crc = ProcessOctet(crc, bytes.Slice(i, 8));
            }

            // Process remaining bytes
            for (int i = alignedLength; i < bytes.Length; i++)
            {
                crc = Table[crc ^ bytes[i]];
            }
        }
        else
        {
            // Process small arrays with simple loop
            for (int i = 0; i < bytes.Length; i++)
            {
                crc = Table[crc ^ bytes[i]];
            }
        }

        return crc;
    }

    /// <summary>
    /// Computes the CRC-8 of the specified byte range
    /// </summary>
    /// <param name="bytes">The buffer to compute the CRC upon</param>
    /// <param name="start">The start index upon which to compute the CRC</param>
    /// <param name="length">The length of the buffer upon which to compute the CRC</param>
    /// <returns>The specified CRC</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte HashToByte(byte[] bytes, int start, int length)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        if (bytes.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(bytes), "Bytes array cannot be empty");

        if (start >= bytes.Length && length > 1)
            throw new ArgumentOutOfRangeException(nameof(start), "Start index is out of range");

        int end = start + length;
        if (end > bytes.Length)
            throw new ArgumentOutOfRangeException(nameof(length), "Specified length exceeds buffer bounds");

        return HashToByte(bytes.AsSpan(start, length));
    }

    /// <summary>
    /// Process 8 bytes at a time for better performance on larger inputs
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte ProcessOctet(byte crc, ReadOnlySpan<byte> octet)
    {
        // Process 8 bytes at once for better CPU cache utilization
        crc = Table[crc ^ octet[0]];
        crc = Table[crc ^ octet[1]];
        crc = Table[crc ^ octet[2]];
        crc = Table[crc ^ octet[3]];
        crc = Table[crc ^ octet[4]];
        crc = Table[crc ^ octet[5]];
        crc = Table[crc ^ octet[6]];
        crc = Table[crc ^ octet[7]];

        return crc;
    }

    /// <summary>
    /// Computes the CRC-8 of the specified memory
    /// </summary>
    /// <param name="data">The memory to compute the CRC upon</param>
    /// <returns>The specified CRC</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe byte HashToByte<T>(Span<T> data) where T : unmanaged
    {
        if (data.IsEmpty)
            throw new ArgumentException("Data span cannot be empty", nameof(data));

        ReadOnlySpan<byte> bytes;
        if (typeof(T) == typeof(byte))
        {
            bytes = MemoryMarshal.Cast<T, byte>(data);
        }
        else
        {
            // Handle non-byte spans by reinterpreting as bytes
            bytes = MemoryMarshal.AsBytes(data);
        }

        return HashToByte(bytes);
    }
}
