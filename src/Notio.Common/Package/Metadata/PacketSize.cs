namespace Notio.Common.Package.Metadata;

/// <summary>
/// Defines the sizes of the components in the packet header.
/// </summary>
public static class PacketSize
{
    /// <summary>
    /// The size of the Length field in the packet header, in bytes.
    /// </summary>
    public const int Length = sizeof(ushort);

    /// <summary>
    /// The size of the Number field in the packet header, in bytes.
    /// </summary>
    public const int Id = sizeof(ushort);

    /// <summary>
    /// The size of the Type field in the packet header, in bytes.
    /// </summary>
    public const int Type = sizeof(byte);

    /// <summary>
    /// The size of the Flags field in the packet header, in bytes.
    /// </summary>
    public const int Flags = sizeof(byte);

    /// <summary>
    /// The size of the Priority field in the packet header, in bytes.
    /// </summary>
    public const int Priority = sizeof(byte);

    /// <summary>
    /// The size of the Id field in the packet header, in bytes.
    /// </summary>
    public const int Command = sizeof(ushort);

    /// <summary>
    /// The size of the Timestamp field in the packet header, in bytes.
    /// </summary>
    public const int Timestamp = sizeof(ulong);

    /// <summary>
    /// The size of the Checksum field in the packet header, in bytes.
    /// </summary>
    public const int Checksum = sizeof(uint);

    /// <summary>
    /// The total size of the packet header, which is the sum of the sizes of all header fields.
    /// </summary>
    public const int Header = Length + Id + Type + Flags + Priority + Command + Timestamp + Checksum;
}
