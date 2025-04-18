namespace Notio.Common.Security;

/// <summary>
/// Represents the available compression types.
/// </summary>
public enum CompressionMode : byte
{
    /// <summary>
    /// Represents GZip compression.
    /// </summary>
    GZip,

    /// <summary>Brotli compression.</summary>
    Brotli,

    /// <summary>Deflate compression.</summary>
    Deflate
}
