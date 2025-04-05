using Notio.Common.Exceptions;
using Notio.Common.Package.Metadata;
using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Notio.Network.Package.Serialization;

public static partial class PacketSerializer
{
    /// <summary>
    /// Writes a packet to a given buffer in a fast and efficient way.
    /// </summary>
    /// <param name="buffer">The buffer to write the packet data to.</param>
    /// <param name="packet">The packet to be written.</param>
    /// <returns>The Number of bytes written to the buffer.</returns>
    /// <exception cref="PackageException">Thrown if the buffer size is too small for the packet.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WritePacketFast(Span<byte> buffer, in Packet packet)
    {
        int totalSize = PacketSize.Header + packet.Payload.Length;

        if (buffer.Length < totalSize)
            throw new PackageException($"Buffer size ({buffer.Length}) is too small for packet size ({totalSize}).");

        ushort id = packet.Id;
        ulong timestamp = packet.Timestamp;
        uint checksum = packet.Checksum;

        try
        {
            // Write header first
            EmitHeader(buffer, totalSize, id, timestamp, checksum, packet);

            // Write the payload if it's not empty
            if (packet.Payload.Length > 0)
            {
                packet.Payload.Span.CopyTo(buffer[PacketSize.Header..]);
            }

            return totalSize;
        }
        catch (Exception ex) when (ex is not PackageException)
        {
            throw new PackageException("Failed to serialize packet", ex);
        }
    }

    /// <summary>
    /// Asynchronously writes a packet to a given memory buffer.
    /// </summary>
    /// <param name="buffer">The buffer to write the packet data to.</param>
    /// <param name="packet">The packet to be written.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A value task representing the asynchronous write operation.</returns>
    public static ValueTask<int> WritePacketFastAsync(
        Memory<byte> buffer, Packet packet, CancellationToken cancellationToken = default)
    {
        // For small payloads, perform synchronously to avoid task overhead
        if (packet.Payload.Length < 4096)
        {
            try
            {
                return new ValueTask<int>(WritePacketFast(buffer.Span, packet));
            }
            catch (Exception ex) when (ex is not OperationCanceledException && cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<int>(cancellationToken);
            }
            catch (Exception ex)
            {
                return ValueTask.FromException<int>(ex);
            }
        }

        // For larger payloads, use Task to prevent blocking
        return new ValueTask<int>(Task.Run(() => WritePacketFast(buffer.Span, packet), cancellationToken));
    }

    /// <summary>
    /// Asynchronously writes a packet to a stream.
    /// </summary>
    /// <param name="stream">The stream to write the packet to.</param>
    /// <param name="packet">The packet to be written.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    public static async ValueTask WriteToStreamAsync(
        Stream stream, Packet packet, CancellationToken cancellationToken = default)
    {
        int totalSize = PacketSize.Header + packet.Payload.Length;

        // For very large payloads, rent from the pool
        byte[] buffer = totalSize <= 81920
            ? ArrayPool<byte>.Shared.Rent(totalSize)
            : new byte[totalSize]; // For extremely large packets, avoid exhausting the pool

        try
        {
            int bytesWritten = WritePacketFast(buffer.AsSpan(0, totalSize), packet);

            await stream.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesWritten), cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            // Handle cancellation gracefully
            throw new PackageException("Packet serialization was canceled.", ex);
        }
        catch (Exception ex)
        {
            // Re-throw any unexpected errors with extra context
            throw new PackageException("Failed to write packet to stream.", ex);
        }
        finally
        {
            // Only return to pool if we rented from it
            if (totalSize <= 81920)
            {
                ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
            }
        }
    }
}
