// Last updated: 2025-02-28 14:58:04 by phcnguyen
using Notio.Randomization;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Notio.Cryptography.Asymmetric;

/// <summary>
/// High-performance implementation of the X25519 elliptic curve Diffie-Hellman (ECDH) key exchange protocol.
/// </summary>
/// <remarks>
/// X25519 is a specific elliptic curve designed for use in cryptographic protocols like TLS.
/// It allows two parties to securely exchange keys without needing to share a secret in advance.
/// This implementation follows RFC 7748 specifications.
/// </remarks>
public static class X25519
{
    // Prime p = 2^255 - 19
    private static readonly BigInteger P = BigInteger.Parse("57896044618658097711785492504343953926634992332820282019728792003956564819949");

    // Constant A24 = (486662 - 2)/4 = 121665
    private static readonly BigInteger A24 = 121665;

    // Field Element Size in bytes
    private const int FieldElementSize = 32;

    // Base point u = 9 (encoded as 32-byte little-endian)
    private static readonly byte[] BasePoint = CreateBasePoint();

    /// <summary>
    /// Generates an X25519 key pair.
    /// </summary>
    /// <returns>A tuple with (privateKey, publicKey) each 32 bytes.</returns>
    public static (byte[] PrivateKey, byte[] PublicKey) GenerateKeyPair()
    {
        // Generate a random 32-byte scalar and clamp it
        byte[] privateKey = new byte[FieldElementSize];
        RandomizedGenerator.Fill(privateKey);
        ClampScalar(privateKey);

        // Compute public key using the base point
        byte[] publicKey = ScalarMult(privateKey, BasePoint);
        return (privateKey, publicKey);
    }

    /// <summary>
    /// Computes the shared secret between your private key and a peer's public key.
    /// </summary>
    /// <param name="privateKey">Your 32-byte private key.</param>
    /// <param name="peerPublicKey">The peer's 32-byte public key.</param>
    /// <returns>The shared secret as a 32-byte array.</returns>
    /// <exception cref="ArgumentException">If either key is not exactly 32 bytes.</exception>
    public static byte[] ComputeSharedSecret(ReadOnlySpan<byte> privateKey, ReadOnlySpan<byte> peerPublicKey)
    {
        if (privateKey.Length != FieldElementSize)
            throw new ArgumentException($"Private key must be {FieldElementSize} bytes.", nameof(privateKey));

        if (peerPublicKey.Length != FieldElementSize)
            throw new ArgumentException($"Public key must be {FieldElementSize} bytes.", nameof(peerPublicKey));

        // Create a copy of the private key to clamp it without modifying the original
        Span<byte> clampedPrivateKey = stackalloc byte[FieldElementSize];
        privateKey.CopyTo(clampedPrivateKey);
        ClampScalar(clampedPrivateKey);

        // Compute the shared secret
        return ScalarMult(clampedPrivateKey, peerPublicKey);
    }

    /// <summary>
    /// Clamps a 32-byte scalar for X25519 as specified in RFC 7748.
    /// The clamping is done in-place.
    /// </summary>
    /// <param name="scalar">The scalar to clamp (must be 32 bytes).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ClampScalar(Span<byte> scalar)
    {
        Debug.Assert(scalar.Length == FieldElementSize);

        scalar[0] &= 248;     // Clear lower 3 bits
        scalar[31] &= 127;    // Clear high bit
        scalar[31] |= 64;     // Set second-highest bit
    }

    /// <summary>
    /// Creates the base point for X25519.
    /// </summary>
    /// <returns>A 32-byte array representing u=9 in little-endian.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte[] CreateBasePoint()
    {
        var basePoint = new byte[FieldElementSize];
        basePoint[0] = 9;  // u=9 in little-endian
        return basePoint;
    }

    /// <summary>
    /// Computes X25519 scalar multiplication using optimized Montgomery ladder.
    /// </summary>
    /// <param name="scalar">A 32-byte scalar (will be clamped)</param>
    /// <param name="uCoordinate">A 32-byte u-coordinate</param>
    /// <returns>The resulting 32-byte u-coordinate.</returns>
    private static byte[] ScalarMult(ReadOnlySpan<byte> scalar, ReadOnlySpan<byte> uCoordinate)
    {
        Debug.Assert(scalar.Length == FieldElementSize && uCoordinate.Length == FieldElementSize);

        // Convert little-endian byte arrays to BigInteger
        BigInteger uValue = ToBigInteger(uCoordinate);

        // Create intermediate values for Montgomery ladder
        BigInteger x1 = uValue;
        BigInteger z1 = BigInteger.One;
        BigInteger x2 = BigInteger.One;
        BigInteger z2 = BigInteger.Zero;

        int swapBit = 0;

        // Process bits from most significant to least significant (except the top bit which is always 0)
        // We process from bits 254 down to 0
        for (int i = 254; i >= 0; i--)
        {
            // Extract the current bit
            int bit = (scalar[i >> 3] >> (i & 7)) & 1;

            // Conditional swap based on current bit
            int swap = bit ^ swapBit;
            if (swap == 1)
            {
                SwapFieldElements(ref x1, ref x2);
                SwapFieldElements(ref z1, ref z2);
            }
            swapBit = bit;

            // Montgomery ladder step
            BigInteger a = (x2 + z2) % P;
            BigInteger b = (x2 - z2 + P) % P;
            BigInteger c = (x1 + z1) % P;
            BigInteger d = (x1 - z1 + P) % P;

            BigInteger da = (d * a) % P;
            BigInteger cb = (c * b) % P;

            x1 = ModMul(da + cb, da + cb);
            z1 = ModMul(uValue, ModMul(da - cb + P, da - cb + P));

            x2 = ModMul(a * b, a * b);
            BigInteger e = (a * a - b * b + P) % P;
            z2 = ModMul(e, (a * a + ModMul(A24, e)));
        }

        // Final conditional swap
        if (swapBit == 1)
        {
            SwapFieldElements(ref x1, ref x2);
            SwapFieldElements(ref z1, ref z2);
        }

        // Compute x2/z2 using modular inverse
        BigInteger result = ModMul(x2, ModInverse(z2));

        // Convert result to little-endian bytes
        return ToLittleEndianBytes(result);
    }

    /// <summary>
    /// Modular multiplication with reduction modulo P
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BigInteger ModMul(BigInteger a, BigInteger b)
        => (a * b) % P;

    /// <summary>
    /// Computes the modular inverse of a modulo p using Fermat's little theorem.
    /// Since p is prime, a^(p-2) mod p is the inverse.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BigInteger ModInverse(BigInteger a)
        => BigInteger.ModPow(a, P - 2, P);

    /// <summary>
    /// Swaps two field elements if the swap bit is 1
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SwapFieldElements(ref BigInteger a, ref BigInteger b)
        => (b, a) = (a, b);

    /// <summary>
    /// Converts a little-endian byte span to a BigInteger
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BigInteger ToBigInteger(ReadOnlySpan<byte> bytes)
        => new(bytes, isUnsigned: true, isBigEndian: false);

    /// <summary>
    /// Converts a BigInteger to a little-endian byte array of fixed length.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte[] ToLittleEndianBytes(BigInteger value)
    {
        // Get the bytes in little-endian order
        Span<byte> bytes = stackalloc byte[FieldElementSize];
        if (!value.TryWriteBytes(bytes, out int bytesWritten, isUnsigned: true, isBigEndian: false))
        {
            // This should never happen with our field size
            throw new InvalidOperationException("Failed to convert BigInteger to byte array");
        }

        // Ensure fixed size output
        byte[] result = new byte[FieldElementSize];
        bytes[..Math.Min(bytesWritten, FieldElementSize)].CopyTo(result);
        return result;
    }

    /// <summary>
    /// Debug assertion class
    /// </summary>
    private static class Debug
    {
        [Conditional("DEBUG")]
        public static void Assert(bool condition)
        {
            if (!condition)
                throw new InvalidOperationException("Debug assertion failed");
        }
    }
}
