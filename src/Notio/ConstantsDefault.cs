using System.Text;

namespace Notio;

/// <summary>
/// Represents default values and constants for configurations.
/// </summary>
public static class ConstantsDefault
{
    /// <summary>
    /// The number of microseconds in one second (1,000,000).
    /// This value is used for time conversions and time-based calculations.
    /// </summary>
    public const long MicrosecondsPerSecond = 1_000_000L;

    /// <summary>
    /// The threshold size (in bytes) for using stack-based memory allocation.
    /// This value represents the maximum size for which memory can be allocated on the stack.
    /// </summary>
    public const int MaxStackAllocSize = 0x100;

    /// <summary>
    /// The threshold size (in bytes) for using heap-based memory allocation.
    /// This value represents the maximum size for which memory should be allocated from the heap instead of the stack.
    /// </summary>
    public const int MaxHeapAllocSize = 0x400;

    /// <summary>
    /// The default encoding used for JSON serialization and deserialization.
    /// </summary>
    public static Encoding Encoding { get; set; } = Encoding.UTF8;
}
