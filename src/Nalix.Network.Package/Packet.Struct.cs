using Nalix.Common.Package;
using Nalix.Common.Package.Enums;
using Nalix.Serialization;
using Nalix.Shared.Time;

namespace Nalix.Network.Package;

/// <summary>
/// Represents an immutable network packet with metadata and payload.
/// This high-performance struct is optimized for efficient serialization and transmission.
/// </summary>
[System.Runtime.InteropServices.StructLayout(
    System.Runtime.InteropServices.LayoutKind.Sequential)]
public readonly partial struct Packet : IPacket, System.IDisposable
{
    #region Constructors

    /// <summary>
    /// Creates an empty packet with the specified id and code.
    /// </summary>
    /// <param name="id">The packet Number.</param>
    /// <param name="code">The packet code.</param>
    public static Packet Empty(ushort id, PacketCode code)
        => new(id, code, PacketType.None, PacketFlags.None, PacketPriority.Low, System.Memory<byte>.Empty);

    /// <summary>
    /// Initializes a new instance of the <see cref="Packet"/> struct with a specific Number and payload.
    /// </summary>
    /// <param name="id">The packet Number.</param>
    /// <param name="code">The packet code.</param>
    /// <param name="payload">The packet payload (data).</param>
    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public Packet(ushort id, PacketCode code, byte[] payload)
        : this(id, code, new System.Memory<byte>(payload))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Packet"/> struct with a specific Number and payload.
    /// </summary>
    /// <param name="id">The packet Number.</param>
    /// <param name="code">The packet code.</param>
    /// <param name="payload">The packet payload (data).</param>
    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public Packet(ushort id, PacketCode code, System.ReadOnlySpan<byte> payload)
        : this(id, code, new System.Memory<byte>(payload.ToArray()))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Packet"/> struct with a specific Number and payload.
    /// </summary>
    /// <param name="id">The packet Number.</param>
    /// <param name="code">The packet code.</param>
    /// <param name="payload">The packet payload (data).</param>
    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public Packet(ushort id, PacketCode code, System.Memory<byte> payload)
        : this(id, code, PacketType.Binary, PacketFlags.None, PacketPriority.Low, payload)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Packet"/> struct with specified enum values for flags and priority.
    /// </summary>
    /// <param name="id">The packet id.</param>
    /// <param name="code">The packet code.</param>
    /// <param name="s">The packet payload as a UTF-8 encoded string.</param>
    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public Packet(ushort id, PacketCode code, string s)
        : this(id, code, PacketType.String, PacketFlags.None, PacketPriority.Low, JsonOptions.Encoding.GetBytes(s))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Packet"/> struct with type, flags, priority, id, and payload.
    /// </summary>
    /// <param name="id">The packet id.</param>
    /// <param name="code">The packet code.</param>
    /// <param name="type">The packet type.</param>
    /// <param name="flags">The packet flags.</param>
    /// <param name="priority">The packet priority.</param>
    /// <param name="payload">The packet payload (data).</param>
    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public Packet(
        ushort id,
        ushort code,
        byte type,
        byte flags,
        byte priority,
        System.Memory<byte> payload)
        : this(id, (PacketCode)code, (PacketType)type, (PacketFlags)flags, (PacketPriority)priority, payload)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Packet"/> struct with specified enum values for type, flags, and priority.
    /// </summary>
    /// <param name="id">The packet id.</param>
    /// <param name="code">The packet code.</param>
    /// <param name="type">The packet type.</param>
    /// <param name="flags">The packet flags.</param>
    /// <param name="priority">The packet priority.</param>
    /// <param name="payload">The packet payload (data).</param>
    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public Packet(
        ushort id,
        PacketCode code,
        PacketType type,
        PacketFlags flags,
        PacketPriority priority,
        System.Memory<byte> payload)
        : this(id, 0, Clock.UnixTicksNow(), code, type, flags, priority, 0, payload, true)
    {
    }

    #endregion Constructors

    #region IDisposable

    /// <summary>
    /// Releases any resources used by this packet, returning rented arrays to the pool.
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        // Only return large arrays to the pool
        if (Payload.Length > MaxHeapAllocSize &&
            System.Runtime.InteropServices.MemoryMarshal.TryGetArray<byte>
            (Payload, out System.ArraySegment<byte> segment) &&
            segment.Array is { } array)
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(array, clearArray: true);
        }
    }

    #endregion IDisposable
}
