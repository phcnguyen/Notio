using Notio.Cryptography.Utilities;
using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Notio.Cryptography.Symmetric;

/// <summary>
/// Provides encryption and decryption utilities using the Twofish block cipher.
/// Twofish is a symmetric key block cipher with a block size of 128 bits and key sizes up to 256 bits.
/// It was one of the AES finalists, and was designed by Bruce Schneier and others.
/// </summary>
public static class Twofish
{
    #region Constants

    // Block size in bytes (128 bits)
    private const int BlockSize = 16;

    #endregion

    #region Fields

    // MDS Matrix for diffusion
    private static readonly byte[][] MDS = [
        [0x01, 0xEF, 0x5B, 0x5B],
        [0x5B, 0xEF, 0xEF, 0x01],
        [0xEF, 0x5B, 0x01, 0xEF],
        [0xEF, 0x01, 0xEF, 0x5B]
    ];

    // RS Matrix for the key schedule
    private static readonly byte[][] RS = [
        [0x01, 0xA4, 0x55, 0x87, 0x5A, 0x58, 0xDB, 0x9E],
        [0xA4, 0x56, 0x82, 0xF3, 0x1E, 0xC6, 0x68, 0xE5],
        [0x02, 0xA1, 0xFC, 0xC1, 0x47, 0xAE, 0x3D, 0x19],
        [0xA4, 0x55, 0x87, 0x5A, 0x58, 0xDB, 0x9E, 0x03]
    ];

    // S-boxes
    private static readonly byte[] Q0 = [
        0xA9, 0x67, 0xB3, 0xE8, 0x04, 0xFD, 0xA3, 0x76,
        0x9A, 0x92, 0x80, 0x78, 0xE4, 0xDD, 0xD1, 0x38,
        0x0D, 0xC6, 0x35, 0x98, 0x18, 0xF7, 0xEC, 0x6C,
        0x43, 0x75, 0x37, 0x26, 0xFA, 0x13, 0x94, 0x48,
        0xF2, 0xD0, 0x8B, 0x30, 0x84, 0x54, 0xDF, 0x23,
        0x19, 0x5B, 0x3D, 0x59, 0xF3, 0xAE, 0xA2, 0x82,
        0x63, 0x01, 0x83, 0x2E, 0xD9, 0x51, 0x9B, 0x7C,
        0xA6, 0xEB, 0xA5, 0xBE, 0x16, 0x0C, 0xE3, 0x61,
        0xC0, 0x8C, 0x3A, 0xF5, 0x73, 0x2C, 0x25, 0x0B,
        0xBB, 0x4E, 0x89, 0x6B, 0x53, 0x6A, 0xB4, 0xF1,
        0xE1, 0xE6, 0xBD, 0x45, 0xE2, 0xF4, 0xB6, 0x66,
        0xCC, 0x95, 0x03, 0x56, 0xD4, 0x1C, 0x1E, 0xD7,
        0xFB, 0xC3, 0x8E, 0xB5, 0xE9, 0xCF, 0xBF, 0xBA,
        0xEA, 0x77, 0x39, 0xAF, 0x33, 0xC9, 0x62, 0x71,
        0x81, 0x79, 0x09, 0xAD, 0x24, 0xCD, 0xF9, 0xD8,
        0xE5, 0xC5, 0xB9, 0x4D, 0x44, 0x08, 0x86, 0xE7,
        0xA1, 0x1D, 0xAA, 0xED, 0x06, 0x70, 0xB2, 0xD2,
        0x41, 0x7B, 0xA0, 0x11, 0x31, 0xC2, 0x27, 0x90,
        0x20, 0xF6, 0x60, 0xFF, 0x96, 0x5C, 0xB1, 0xAB,
        0x9E, 0x9C, 0x52, 0x1B, 0x5F, 0x93, 0x0A, 0xEF,
        0x91, 0x85, 0x49, 0xEE, 0x2D, 0x4F, 0x8F, 0x3B,
        0x47, 0x87, 0x6D, 0x46, 0xD6, 0x3E, 0x69, 0x64,
        0x2A, 0xCE, 0xCB, 0x2F, 0xFC, 0x97, 0x05, 0x7A,
        0xAC, 0x7F, 0xD5, 0x1A, 0x4B, 0x0E, 0xA7, 0x5A,
        0x28, 0x14, 0x3F, 0x29, 0x88, 0x3C, 0x4C, 0x02,
        0xB8, 0xDA, 0xB0, 0x17, 0x55, 0x1F, 0x8A, 0x7D,
        0x57, 0xC7, 0x8D, 0x74, 0xB7, 0xC4, 0x9F, 0x72,
        0x7E, 0x15, 0x22, 0x12, 0x58, 0x07, 0x99, 0x34,
        0x6E, 0x50, 0xDE, 0x68, 0x65, 0xBC, 0xDB, 0xF8,
        0xC8, 0xA8, 0x2B, 0x40, 0xDC, 0xFE, 0x32, 0xA4,
        0xCA, 0x10, 0x21, 0xF0, 0xD3, 0x5D, 0x0F, 0x00,
        0x6F, 0x9D, 0x36, 0x42, 0x4A, 0x5E, 0xC1, 0xE0
    ];

    private static readonly byte[] Q1 = [
        0x75, 0xF3, 0xC6, 0xF4, 0xDB, 0x7B, 0xFB, 0xC8,
        0x4A, 0xD3, 0xE6, 0x6B, 0x45, 0x7D, 0xE8, 0x4B,
        0xD6, 0x32, 0xD8, 0xFD, 0x37, 0x71, 0xF1, 0xE1,
        0x30, 0x0F, 0xF8, 0x1B, 0x87, 0xFA, 0x06, 0x3F,
        0x5E, 0xBA, 0xAE, 0x5B, 0x8A, 0x00, 0xBC, 0x9D,
        0x6D, 0xC1, 0xB1, 0x0E, 0x80, 0x5D, 0xD2, 0xD5,
        0xA0, 0x84, 0x07, 0x14, 0xB5, 0x90, 0x2C, 0xA3,
        0xB2, 0x73, 0x4C, 0x54, 0x92, 0x74, 0x36, 0x51,
        0x38, 0xB0, 0xBD, 0x5A, 0xFC, 0x60, 0x62, 0x96,
        0x6C, 0x42, 0xF7, 0x10, 0x7C, 0x28, 0x27, 0x8C,
        0x13, 0x95, 0x9C, 0xC7, 0x24, 0x46, 0x3B, 0x70,
        0xCA, 0xE3, 0x85, 0xCB, 0x11, 0xD0, 0x93, 0xB8,
        0xA6, 0x83, 0x20, 0xFF, 0x9F, 0x77, 0xC3, 0xCC,
        0x03, 0x6F, 0x08, 0xBF, 0x40, 0xE7, 0x2B, 0xE2,
        0x79, 0x0C, 0xAA, 0x82, 0x41, 0x3A, 0xEA, 0xB9,
        0xE4, 0x9A, 0xA4, 0x97, 0x7E, 0xDA, 0x7A, 0x17,
        0x66, 0x94, 0xA1, 0x1D, 0x3D, 0xF0, 0xDE, 0xB3,
        0x0B, 0x72, 0xA7, 0x1C, 0xEF, 0xD1, 0x53, 0x3E,
        0x8F, 0x33, 0x26, 0x5F, 0xEC, 0x76, 0x2A, 0x49,
        0x81, 0x88, 0xEE, 0x21, 0xC4, 0x1A, 0xEB, 0xD9,
        0xC5, 0x39, 0x99, 0xCD, 0xAD, 0x31, 0x8B, 0x01,
        0x18, 0x23, 0xDD, 0x1F, 0x4E, 0x2D, 0xF9, 0x48,
        0x4F, 0xF2, 0x65, 0x8E, 0x78, 0x5C, 0x58, 0x19,
        0x8D, 0xE5, 0x98, 0x57, 0x67, 0x7F, 0x05, 0x64,
        0xAF, 0x63, 0xB6, 0xFE, 0xF5, 0xB7, 0x3C, 0xA5,
        0xCE, 0xE9, 0x68, 0x44, 0xE0, 0x4D, 0x43, 0x69,
        0x29, 0x2E, 0xAC, 0x15, 0x59, 0xA8, 0x0A, 0x9E,
        0x6E, 0x47, 0xDF, 0x34, 0x35, 0x6A, 0xCF, 0xDC,
        0x22, 0xC9, 0xC0, 0x9B, 0x89, 0xD4, 0xED, 0xAB,
        0x12, 0xA2, 0x0D, 0x52, 0xBB, 0x02, 0x2F, 0xA9,
        0xD7, 0x61, 0x1E, 0xB4, 0x50, 0x04, 0xF6, 0xC2,
        0x16, 0x25, 0x86, 0x56, 0x55, 0x09, 0xBE, 0x91
    ];

    #endregion

    #region Public Class

    /// <summary>
    /// Twofish in ECB mode.
    /// </summary>
    public static class ECB
    {
        /// <summary>
        /// Encrypts data using Twofish in ECB mode.
        /// Note: ECB mode is not recommended for most applications.
        /// </summary>
        /// <param name="key">Key of 16, 24, or 32 bytes (128, 192, or 256 bits)</param>
        /// <param name="plaintext">Data to encrypt (must be a multiple of 16 bytes)</param>
        /// <returns>Encrypted data</returns>
        public static byte[] Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> plaintext)
        {
            ValidateParameters(key, plaintext);

            byte[] ciphertext = new byte[plaintext.Length];
            Encrypt(key, plaintext, ciphertext);
            return ciphertext;
        }

        /// <summary>
        /// Encrypts data using Twofish in ECB mode, writing to the provided buffer.
        /// </summary>
        /// <param name="key">Key of 16, 24, or 32 bytes (128, 192, or 256 bits)</param>
        /// <param name="plaintext">Data to encrypt (must be a multiple of 16 bytes)</param>
        /// <param name="ciphertext">Buffer to receive encrypted data</param>
        /// <returns>Number of bytes written</returns>
        public static int Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> plaintext, Span<byte> ciphertext)
        {
            ValidateParameters(key, plaintext);

            if (ciphertext.Length < plaintext.Length)
                throw new ArgumentException("Output buffer is too small", nameof(ciphertext));

            // Key setup
            var keySchedule = GenerateKeySchedule(key);

            // Process each block
            for (int i = 0; i < plaintext.Length; i += BlockSize)
            {
                EncryptBlock(plaintext.Slice(i, BlockSize), ciphertext.Slice(i, BlockSize), keySchedule);
            }

            return plaintext.Length;
        }

        /// <summary>
        /// Decrypts data using Twofish in ECB mode.
        /// </summary>
        /// <param name="key">Key of 16, 24, or 32 bytes (128, 192, or 256 bits)</param>
        /// <param name="ciphertext">Data to decrypt (must be a multiple of 16 bytes)</param>
        /// <returns>Decrypted data</returns>
        public static byte[] Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> ciphertext)
        {
            ValidateParameters(key, ciphertext);

            byte[] plaintext = new byte[ciphertext.Length];
            Decrypt(key, ciphertext, plaintext);
            return plaintext;
        }

        /// <summary>
        /// Decrypts data using Twofish in ECB mode, writing to the provided buffer.
        /// </summary>
        /// <param name="key">Key of 16, 24, or 32 bytes (128, 192, or 256 bits)</param>
        /// <param name="ciphertext">Data to decrypt (must be a multiple of 16 bytes)</param>
        /// <param name="plaintext">Buffer to receive decrypted data</param>
        /// <returns>Number of bytes written</returns>
        public static int Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> ciphertext, Span<byte> plaintext)
        {
            ValidateParameters(key, ciphertext);

            if (plaintext.Length < ciphertext.Length)
                throw new ArgumentException("Output buffer is too small", nameof(plaintext));

            // Key setup
            var keySchedule = GenerateKeySchedule(key);

            // Process each block
            for (int i = 0; i < ciphertext.Length; i += BlockSize)
            {
                DecryptBlock(ciphertext.Slice(i, BlockSize), plaintext.Slice(i, BlockSize), keySchedule);
            }

            return ciphertext.Length;
        }
    }

    /// <summary>
    /// Twofish in CBC mode.
    /// </summary>
    public static class CBC
    {
        /// <summary>
        /// Encrypts data using Twofish in CBC mode.
        /// </summary>
        /// <param name="key">Key of 16, 24, or 32 bytes (128, 192, or 256 bits)</param>
        /// <param name="iv">Initialization vector (16 bytes)</param>
        /// <param name="plaintext">Data to encrypt (must be a multiple of 16 bytes)</param>
        /// <returns>Encrypted data</returns>
        public static byte[] Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> plaintext)
        {
            ValidateParameters(key, plaintext);
            if (iv.Length != BlockSize)
                throw new ArgumentException($"IV must be {BlockSize} bytes", nameof(iv));

            byte[] ciphertext = new byte[plaintext.Length];
            Encrypt(key, iv, plaintext, ciphertext);
            return ciphertext;
        }

        /// <summary>
        /// Encrypts data using Twofish in CBC mode, writing to the provided buffer.
        /// </summary>
        /// <param name="key">Key of 16, 24, or 32 bytes (128, 192, or 256 bits)</param>
        /// <param name="iv">Initialization vector (16 bytes)</param>
        /// <param name="plaintext">Data to encrypt (must be a multiple of 16 bytes)</param>
        /// <param name="ciphertext">Buffer to receive encrypted data</param>
        /// <returns>Number of bytes written</returns>
        public static int Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv,
                                    ReadOnlySpan<byte> plaintext, Span<byte> ciphertext)
        {
            ValidateParameters(key, plaintext);
            if (iv.Length != BlockSize)
                throw new ArgumentException($"IV must be {BlockSize} bytes", nameof(iv));

            if (ciphertext.Length < plaintext.Length)
                throw new ArgumentException("Output buffer is too small", nameof(ciphertext));

            // Key setup
            var keySchedule = GenerateKeySchedule(key);

            // Working buffers
            Span<byte> previousBlock = stackalloc byte[BlockSize];
            iv.CopyTo(previousBlock);

            Span<byte> tempBlock = stackalloc byte[BlockSize];

            // Process each block
            for (int i = 0; i < plaintext.Length; i += BlockSize)
            {
                // XOR plaintext with previous ciphertext block (or IV for first block)
                ReadOnlySpan<byte> currentPlaintext = plaintext.Slice(i, BlockSize);
                for (int j = 0; j < BlockSize; j++)
                {
                    tempBlock[j] = (byte)(currentPlaintext[j] ^ previousBlock[j]);
                }

                // Encrypt the block
                EncryptBlock(tempBlock, ciphertext.Slice(i, BlockSize), keySchedule);

                // Update previous block for next iteration
                ciphertext.Slice(i, BlockSize).CopyTo(previousBlock);
            }

            return plaintext.Length;
        }

        /// <summary>
        /// Decrypts data using Twofish in CBC mode.
        /// </summary>
        /// <param name="key">Key of 16, 24, or 32 bytes (128, 192, or 256 bits)</param>
        /// <param name="iv">Initialization vector (16 bytes, same as used for encryption)</param>
        /// <param name="ciphertext">Data to decrypt (must be a multiple of 16 bytes)</param>
        /// <returns>Decrypted data</returns>
        public static byte[] Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> ciphertext)
        {
            ValidateParameters(key, ciphertext);
            if (iv.Length != BlockSize)
                throw new ArgumentException($"IV must be {BlockSize} bytes", nameof(iv));

            byte[] plaintext = new byte[ciphertext.Length];
            Decrypt(key, iv, ciphertext, plaintext);
            return plaintext;
        }

        /// <summary>
        /// Decrypts data using Twofish in CBC mode, writing to the provided buffer.
        /// </summary>
        /// <param name="key">Key of 16, 24, or 32 bytes (128, 192, or 256 bits)</param>
        /// <param name="iv">Initialization vector (16 bytes, same as used for encryption)</param>
        /// <param name="ciphertext">Data to decrypt (must be a multiple of 16 bytes)</param>
        /// <param name="plaintext">Buffer to receive decrypted data</param>
        /// <returns>Number of bytes written</returns>
        public static int Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv,
                                    ReadOnlySpan<byte> ciphertext, Span<byte> plaintext)
        {
            ValidateParameters(key, ciphertext);
            if (iv.Length != BlockSize)
                throw new ArgumentException($"IV must be {BlockSize} bytes", nameof(iv));

            if (plaintext.Length < ciphertext.Length)
                throw new ArgumentException("Output buffer is too small", nameof(plaintext));

            // Key setup
            var keySchedule = GenerateKeySchedule(key);

            // Working buffers
            Span<byte> previousBlock = stackalloc byte[BlockSize];
            iv.CopyTo(previousBlock);

            Span<byte> tempBlock = stackalloc byte[BlockSize];

            // Process each block
            for (int i = 0; i < ciphertext.Length; i += BlockSize)
            {
                // Store current ciphertext block to use as next previous block
                ReadOnlySpan<byte> currentCiphertext = ciphertext.Slice(i, BlockSize);
                Span<byte> currentPlaintext = plaintext.Slice(i, BlockSize);

                // Decrypt the block
                DecryptBlock(currentCiphertext, tempBlock, keySchedule);

                // XOR with previous ciphertext block (or IV for first block)
                for (int j = 0; j < BlockSize; j++)
                {
                    currentPlaintext[j] = (byte)(tempBlock[j] ^ previousBlock[j]);
                }

                // Update previous block for next iteration
                currentCiphertext.CopyTo(previousBlock);
            }

            return ciphertext.Length;
        }
    }

    #endregion

    #region Core Implementation

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidateParameters(ReadOnlySpan<byte> key, ReadOnlySpan<byte> data)
    {
        if (key.Length != 16 && key.Length != 24 && key.Length != 32)
            throw new ArgumentException("Key must be 16, 24, or 32 bytes (128, 192, or 256 bits)", nameof(key));

        if (data.Length % BlockSize != 0)
            throw new ArgumentException($"Data length must be a multiple of {BlockSize} bytes", nameof(data));
    }

    /// <summary>
    /// Key schedule structure for Twofish
    /// </summary>
    private struct TwofishKey
    {
        public uint[] ExpandedKey;   // Expanded key (40 32-bit words for rounds)
        public uint[] SBoxKeys;      // S-box keys (up to 4 32-bit words)
    }

    /// <summary>
    /// Generates the key schedule for Twofish encryption/decryption
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TwofishKey GenerateKeySchedule(ReadOnlySpan<byte> key)
    {
        var result = new TwofishKey
        {
            ExpandedKey = new uint[40],
            SBoxKeys = new uint[key.Length / 8]
        };

        int k = key.Length / 8;  // Key length in 64-bit words (2, 3, or 4)
        uint[] Me = new uint[k];
        uint[] Mo = new uint[k];

        // Calculate Mo and Me from the key
        for (int i = 0; i < k; i++)
        {
            Me[i] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(i * 8, 4));
            Mo[i] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(i * 8 + 4, 4));
        }

        // Calculate the S-box keys
        for (int i = 0; i < k; i++)
        {
            result.SBoxKeys[k - 1 - i] = RS_MDS_Encode(Me[i], Mo[i]);
        }

        // Calculate the round subkeys
        uint rho = 0x01010101;
        uint[] A = new uint[20];
        uint[] B = new uint[20];

        for (int i = 0; i < 20; i++)
        {
            A[i] = H((uint)(2 * i * rho), Me, k);
            B[i] = BitwiseUtils.RotateLeft(H((uint)((2 * i + 1) * rho), Mo, k), 8);
            result.ExpandedKey[2 * i] = A[i] + B[i];
            result.ExpandedKey[2 * i + 1] = BitwiseUtils.RotateLeft(A[i] + 2 * B[i], 9);
        }

        return result;
    }

    /// <summary>
    /// Encrypts a single 16-byte block
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EncryptBlock(ReadOnlySpan<byte> plaintext, Span<byte> ciphertext, TwofishKey key)
    {
        // Extract four 32-bit words from plaintext
        uint[] input = new uint[4];
        for (int i = 0; i < 4; i++)
        {
            input[i] = BinaryPrimitives.ReadUInt32LittleEndian(plaintext.Slice(i * 4, 4));
        }

        // Input whitening
        input[0] ^= key.ExpandedKey[0];
        input[1] ^= key.ExpandedKey[1];
        input[2] ^= key.ExpandedKey[2];
        input[3] ^= key.ExpandedKey[3];

        // 16 rounds of encryption
        uint t0, t1;
        for (int round = 0; round < 16; round++)
        {
            t0 = G(input[0], key.SBoxKeys);
            t1 = G(BitwiseUtils.RotateLeft(input[1], 8), key.SBoxKeys);
            input[2] = BitwiseUtils.RotateRight(input[2] ^ (t0 + t1 + key.ExpandedKey[2 * round + 8]), 1);
            input[3] = BitwiseUtils.RotateLeft(input[3], 1) ^ (t0 + 2 * t1 + key.ExpandedKey[2 * round + 9]);

            // Rotate for next round
            if (round < 15)
            {
                (input[2], input[0]) = (input[0], input[2]);
                (input[1], input[3]) = (input[3], input[1]);
            }
        }

        // Output whitening
        input[0] ^= key.ExpandedKey[4];
        input[1] ^= key.ExpandedKey[5];
        input[2] ^= key.ExpandedKey[6];
        input[3] ^= key.ExpandedKey[7];

        // Write output
        for (int i = 0; i < 4; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(ciphertext.Slice(i * 4, 4), input[i]);
        }
    }

    /// <summary>
    /// Decrypts a single 16-byte block
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DecryptBlock(ReadOnlySpan<byte> ciphertext, Span<byte> plaintext, TwofishKey key)
    {
        // Extract four 32-bit words from ciphertext
        uint[] input = new uint[4];
        for (int i = 0; i < 4; i++)
        {
            input[i] = BinaryPrimitives.ReadUInt32LittleEndian(ciphertext.Slice(i * 4, 4));
        }

        // Output whitening (reverse)
        input[0] ^= key.ExpandedKey[4];
        input[1] ^= key.ExpandedKey[5];
        input[2] ^= key.ExpandedKey[6];
        input[3] ^= key.ExpandedKey[7];

        // 16 rounds of decryption (reverse of encryption)
        uint t0, t1;
        for (int round = 15; round >= 0; round--)
        {
            // Rotate for this round (reverse of encryption)
            if (round < 15)
            {
                (input[2], input[0]) = (input[0], input[2]);
                (input[1], input[3]) = (input[3], input[1]);
            }

            t0 = G(input[0], key.SBoxKeys);
            t1 = G(BitwiseUtils.RotateLeft(input[1], 8), key.SBoxKeys);
            input[2] = BitwiseUtils.RotateLeft(input[2], 1) ^ (t0 + t1 + key.ExpandedKey[2 * round + 8]);
            input[3] = BitwiseUtils.RotateRight(input[3] ^ (t0 + 2 * t1 + key.ExpandedKey[2 * round + 9]), 1);
        }

        // Input whitening (reverse)
        input[0] ^= key.ExpandedKey[0];
        input[1] ^= key.ExpandedKey[1];
        input[2] ^= key.ExpandedKey[2];
        input[3] ^= key.ExpandedKey[3];

        // Write output
        for (int i = 0; i < 4; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(plaintext.Slice(i * 4, 4), input[i]);
        }
    }

    /// <summary>
    /// The G function - a key-dependent permutation on a 32-bit word.
    /// This is the heart of the Twofish cipher.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
    private static uint G(uint x, uint[] sBoxKeys)
    {
        // Break input into four bytes and pass through S-boxes
        byte[] result =
        [
            Q01((byte)(x & 0xFF)),
            Q00((byte)((x >> 8) & 0xFF)),
            Q00((byte)((x >> 16) & 0xFF)),
            Q01((byte)((x >> 24) & 0xFF)),
        ];

        // MDS matrix multiplication
        uint a = (uint)(result[0] ^ result[1] ^ result[2] ^ result[3]);
        uint b = (uint)(result[0] ^ result[1]);
        uint c = (uint)(result[2] ^ result[3]);

        return (a << 24) | (b << 16) | (c << 8) | (uint)(result[0] ^ result[3]);
    }

    /// <summary>
    /// The H function for the key schedule
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint H(uint X, uint[] L, int k)
    {
        // Input: X, a 32-bit input
        // L, key material
        // k, Number of 64-bit words in key

        byte y0 = Q00((byte)(X & 0xFF));
        byte y1 = Q01((byte)((X >> 8) & 0xFF));
        byte y2 = Q00((byte)((X >> 16) & 0xFF));
        byte y3 = Q01((byte)((X >> 24) & 0xFF));

        // Apply the MDS matrix
        return MDS_Apply(y0, y1, y2, y3, L, k);
    }

    /// <summary>
    /// MDS matrix application for the H function
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint MDS_Apply(byte y0, byte y1, byte y2, byte y3, uint[] L, int k)
    {
        // Apply the MDS matrix multiplication
        uint z0 = 0, z1 = 0, z2 = 0, z3 = 0;

        // Mix based on key size
        for (int i = 0; i < k; i++)
        {
            z0 ^= L[i] & 0xFF;
            z1 ^= (L[i] >> 8) & 0xFF;
            z2 ^= (L[i] >> 16) & 0xFF;
            z3 ^= (L[i] >> 24) & 0xFF;
        }

        // Final S-box applications
        y0 ^= (byte)z0;
        y1 ^= (byte)z1;
        y2 ^= (byte)z2;
        y3 ^= (byte)z3;

        // Final MDS matrix multiply
        return (uint)MDS[0][y0] ^
              ((uint)MDS[1][y1] << 8) ^
              ((uint)MDS[2][y2] << 16) ^
              ((uint)MDS[3][y3] << 24);
    }

    /// <summary>
    /// Reed-Solomon encoding combined with MDS matrix for the key schedule
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint RS_MDS_Encode(uint k0, uint k1)
    {
        uint result = 0;

        // Apply the Reed-Solomon encoding
        for (int i = 0; i < 4; i++)
        {
            byte b0 = (byte)(k0 >> (8 * i));
            byte b1 = (byte)(k1 >> (8 * i));

            // Reed-Solomon multiplication
            byte r = 0;
            for (int j = 0; j < 8; j++)
            {
                r ^= (byte)(RS[i][j] * ((j < 4 ? b0 : b1) & 0xFF));
            }

            result |= ((uint)r) << (8 * i);
        }

        return result;
    }

    /// <summary>
    /// Q0 S-box lookup function
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte Q00(byte x) => Q0[x];

    /// <summary>
    /// Q1 S-box lookup function
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte Q01(byte x) => Q1[x];

    #endregion
}
