namespace Notio.Common.Memory;

/// <summary>
/// Manages buffers of various sizes.
/// </summary>
public interface IBufferPool
{
    /// <summary>
    /// Gets the maximum buffer size from the configuration list.
    /// </summary>
    int MaxBufferSize { get; }

    /// <summary>
    /// Rents a buffer with a specific size.
    /// </summary>
    /// <param name="size">The size of the buffer to rent. Default value is 256.</param>
    /// <returns>A byte array representing the rented buffer.</returns>
    byte[] Rent(int size = 256);

    /// <summary>
    /// Returns a buffer for reuse.
    /// </summary>
    /// <param name="buffer">The buffer to return.</param>
    void Return(byte[] buffer);

    /// <summary>
    /// Gets memory allocation information for a specific size.
    /// </summary>
    /// <param name="size">The size of the memory to check.</param>
    /// <returns>The allocation value for the given size.</returns>
    double GetAllocationForSize(int size);
}
