namespace Notio.Common.Security;

/// <summary>
/// Specifies the encryption modes available for symmetric encryption.
/// </summary>
public enum EncryptionMode : byte
{
    /// <summary>
    /// ChaCha20 encryption with Poly1305 for authenticated encryption.
    /// </summary>
    ChaCha20Poly1305 = 1,

    /// <summary>
    /// Salsa20 stream cipher.
    /// </summary>
    Salsa20 = 2,

    /// <summary>
    /// Twofish block cipher.
    /// </summary>
    TwofishEcb = 3,

    /// <summary>
    /// Twofish block cipher.
    /// </summary>
    TwofishCbc = 4,

    /// <summary>
    /// XTEA encryption algorithm.
    /// </summary>
    Xtea = 5,
}
