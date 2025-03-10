using Notio.Cryptography.Utilities;
using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Notio.Cryptography.Symmetric;

/// <summary>
/// Provides encryption and decryption utilities using the Salsa20 stream cipher.
/// Salsa20 is a stream cipher designed by Daniel J. Bernstein that produces a keystream
/// to XOR with plaintext for encryption or with ciphertext for decryption.
/// </summary>
public static class Salsa20
{
    // ----------------------------
    // Public API: Encrypt and Decrypt
    // ----------------------------

    /// <summary>
    /// Encrypts plaintext using Salsa20 stream cipher.
    /// </summary>
    /// <param name="key">A 32-byte key (256 bits).</param>
    /// <param name="nonce">An 8-byte nonce (64 bits).</param>
    /// <param name="counter">Initial counter value, typically 0 for first use.</param>
    /// <param name="plaintext">The data to encrypt.</param>
    /// <returns>Encrypted bytes.</returns>
    public static byte[] Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ulong counter, ReadOnlySpan<byte> plaintext)
    {
        ValidateParameters(key, nonce);
        byte[] ciphertext = new byte[plaintext.Length];
        ProcessData(key, nonce, counter, plaintext, ciphertext);
        return ciphertext;
    }

    /// <summary>
    /// Encrypts plaintext using Salsa20 stream cipher, writing the output to the provided buffer.
    /// </summary>
    /// <param name="key">A 32-byte key (256 bits).</param>
    /// <param name="nonce">An 8-byte nonce (64 bits).</param>
    /// <param name="counter">Initial counter value, typically 0 for first use.</param>
    /// <param name="plaintext">The data to encrypt.</param>
    /// <param name="ciphertext">Buffer to receive the encrypted data.</param>
    /// <returns>Number of bytes written.</returns>
    public static int Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ulong counter,
                             ReadOnlySpan<byte> plaintext, Span<byte> ciphertext)
    {
        ValidateParameters(key, nonce);
        if (ciphertext.Length < plaintext.Length)
            throw new ArgumentException("Output buffer is too small", nameof(ciphertext));

        ProcessData(key, nonce, counter, plaintext, ciphertext);
        return plaintext.Length;
    }

    /// <summary>
    /// Decrypts ciphertext using Salsa20 stream cipher.
    /// </summary>
    /// <param name="key">A 32-byte key (256 bits).</param>
    /// <param name="nonce">An 8-byte nonce (64 bits).</param>
    /// <param name="counter">Initial counter value, must be same as used for encryption.</param>
    /// <param name="ciphertext">The data to decrypt.</param>
    /// <returns>Decrypted bytes.</returns>
    public static byte[] Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ulong counter, ReadOnlySpan<byte> ciphertext)
    {
        // Salsa20 decryption is identical to encryption since it's just XOR with the keystream
        return Encrypt(key, nonce, counter, ciphertext);
    }

    /// <summary>
    /// Decrypts ciphertext using Salsa20 stream cipher, writing the output to the provided buffer.
    /// </summary>
    /// <param name="key">A 32-byte key (256 bits).</param>
    /// <param name="nonce">An 8-byte nonce (64 bits).</param>
    /// <param name="counter">Initial counter value, must be same as used for encryption.</param>
    /// <param name="ciphertext">The data to decrypt.</param>
    /// <param name="plaintext">Buffer to receive the decrypted data.</param>
    /// <returns>Number of bytes written.</returns>
    public static int Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ulong counter,
                             ReadOnlySpan<byte> ciphertext, Span<byte> plaintext)
    {
        // Salsa20 decryption is identical to encryption
        return Encrypt(key, nonce, counter, ciphertext, plaintext);
    }

    // ----------------------------
    // Utility Methods
    // ----------------------------

    /// <summary>
    /// Converts a string passphrase into a 32-byte key using a simple hash function.
    /// Note: This is not a cryptographically secure KDF and should not be used for
    /// sensitive applications. Use a proper KDF like PBKDF2, Argon2, or Scrypt instead.
    /// </summary>
    /// <param name="passphrase">The passphrase to convert.</param>
    /// <returns>A 32-byte key derived from the passphrase.</returns>
    public static byte[] DeriveKeyFromPassphrase(string passphrase)
    {
        if (string.IsNullOrEmpty(passphrase))
            throw new ArgumentException("Passphrase cannot be null or empty", nameof(passphrase));

        // Simple hash function to derive a key (NOT secure for real use!)
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(passphrase);
        byte[] key = new byte[32];

        // Simple stretching algorithm (NOT secure for real applications!)
        for (int i = 0; i < 1000; i++)
        {
            for (int j = 0; j < bytes.Length; j++)
            {
                key[j % 32] ^= (byte)(bytes[j] + i);
                key[(j + 1) % 32] = (byte)((key[(j + 1) % 32] + bytes[j]) & 0xFF);
            }

            // Mix further
            for (int j = 0; j < 32; j++)
            {
                key[j] = (byte)((key[j] + key[(j + 1) % 32]) & 0xFF);
            }
        }

        return key;
    }

    /// <summary>
    /// Increments a counter value stored in a byte array.
    /// </summary>
    /// <param name="counter">The counter to increment.</param>
    public static void IncrementCounter(Span<byte> counter)
    {
        for (int i = 0; i < counter.Length; i++)
        {
            if (++counter[i] != 0)
                break;
        }
    }

    // ----------------------------
    // Core Implementation
    // ----------------------------

    private static void ValidateParameters(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce)
    {
        if (key.Length != 32)
            throw new ArgumentException("Key must be 32 bytes (256 bits)", nameof(key));
        if (nonce.Length != 8)
            throw new ArgumentException("Nonce must be 8 bytes (64 bits)", nameof(nonce));
    }

    /// <summary>
    /// Main function to process data (encrypt or decrypt)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ProcessData(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ulong counter,
                                   ReadOnlySpan<byte> input, Span<byte> output)
    {
        int blockCount = (input.Length + 63) / 64;
        Span<byte> keystream = stackalloc byte[64];

        for (int i = 0; i < blockCount; i++)
        {
            ulong blockCounter = counter + (ulong)i;
            GenerateSalsaBlock(key, nonce, blockCounter, keystream);

            int offset = i * 64;
            int bytesToProcess = Math.Min(64, input.Length - offset);

            for (int j = 0; j < bytesToProcess; j++)
            {
                output[offset + j] = (byte)(input[offset + j] ^ keystream[j]);
            }
        }
    }

    /// <summary>
    /// Generates a 64-byte Salsa20 keystream block
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GenerateSalsaBlock(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ulong counter, Span<byte> output)
    {
        // Initialize the state with constants, key, counter, and nonce
        Span<uint> state = stackalloc uint[16];

        // Constants "expand 32-byte k"
        state[0] = 0x61707865; // "expa"
        state[5] = 0x3320646e; // "nd 3"
        state[10] = 0x79622d32; // "2-by"
        state[15] = 0x6b206574; // "te k"

        // Key (first half)
        state[1] = BinaryPrimitives.ReadUInt32LittleEndian(key[..4]);
        state[2] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(4, 4));
        state[3] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(8, 4));
        state[4] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(12, 4));

        // Key (second half)
        state[11] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(16, 4));
        state[12] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(20, 4));
        state[13] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(24, 4));
        state[14] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(28, 4));

        // Counter (64 bits split into two 32-bit words)
        state[8] = (uint)(counter & 0xFFFFFFFF);
        state[9] = (uint)(counter >> 32);

        // Nonce (64 bits split into two 32-bit words)
        state[6] = BinaryPrimitives.ReadUInt32LittleEndian(nonce[..4]);
        state[7] = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(4, 4));

        // Create a working copy of the state
        Span<uint> workingState = stackalloc uint[16];
        state.CopyTo(workingState);

        // Apply the Salsa20 core function (20 rounds)
        for (int i = 0; i < 10; i++)
        {
            // Column rounds
            QuarterRound(ref workingState[0], ref workingState[4], ref workingState[8], ref workingState[12]);
            QuarterRound(ref workingState[5], ref workingState[9], ref workingState[13], ref workingState[1]);
            QuarterRound(ref workingState[10], ref workingState[14], ref workingState[2], ref workingState[6]);
            QuarterRound(ref workingState[15], ref workingState[3], ref workingState[7], ref workingState[11]);

            // Row rounds
            QuarterRound(ref workingState[0], ref workingState[1], ref workingState[2], ref workingState[3]);
            QuarterRound(ref workingState[5], ref workingState[6], ref workingState[7], ref workingState[4]);
            QuarterRound(ref workingState[10], ref workingState[11], ref workingState[8], ref workingState[9]);
            QuarterRound(ref workingState[15], ref workingState[12], ref workingState[13], ref workingState[14]);
        }

        // Add the original state to the worked state and serialize to output
        for (int i = 0; i < 16; i++)
        {
            uint result = workingState[i] + state[i];
            BinaryPrimitives.WriteUInt32LittleEndian(output.Slice(i * 4, 4), result);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void QuarterRound(ref uint a, ref uint b, ref uint c, ref uint d)
    {
        b ^= BitwiseUtils.RotateLeft(a + d, 7);
        c ^= BitwiseUtils.RotateLeft(b + a, 9);
        d ^= BitwiseUtils.RotateLeft(c + b, 13);
        a ^= BitwiseUtils.RotateLeft(d + c, 18);
    }
}
