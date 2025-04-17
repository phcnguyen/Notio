using Notio.Common.Package.Enums;
using Notio.Network.Dispatcher.Statistics;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Notio.Network.Dispatcher.Queue;

public sealed partial class PacketQueue<TPacket> where TPacket : Common.Package.IPacket
{
    #region Public Methods

    /// <summary>
    /// Get queue statistics
    /// </summary>
    public PacketStatistics GetStatistics()
    {
        if (!_collectStatistics || _queueTimer == null)
            return new PacketStatistics();

        Dictionary<PacketPriority, PriorityStatistics> stats = [];

        this.CollectStatisticsInternal(stats);

        float avgProcessingMs = 0;
        if (_packetsProcessed > 0)
            avgProcessingMs = (float)(_totalProcessingTicks * 1000.0 / Stopwatch.Frequency) / _packetsProcessed;

        return new PacketStatistics
        {
            TotalCount = Count,
            PerPriorityStats = stats,
            ArgProcessingTimeMs = avgProcessingMs,
            UptimeSeconds = (int)_queueTimer.Elapsed.TotalSeconds // _queueTimer is guaranteed to be non-null here
        };
    }

    #endregion

    #region Private Methods

    private void CollectStatisticsInternal(Dictionary<PacketPriority, PriorityStatistics> stats)
    {
        for (int i = 0; i < _priorityCount; i++)
        {
            stats[(PacketPriority)i] = new PriorityStatistics
            {
                CurrentQueueSize = Volatile.Read(ref _priorityCounts[i]),
                EnqueuedCount = _enqueuedCounts[i],
                DequeuedCount = _dequeuedCounts[i],
                ExpiredCount = _expiredCounts[i],
                InvalidCount = _invalidCounts[i]
            };
        }
    }

    /// <summary>
    /// Update performance statistics
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdatePerformanceStats(long startTicks)
    {
        long endTicks = Stopwatch.GetTimestamp();
        long elapsed = endTicks - startTicks;

        _totalProcessingTicks += elapsed;
        _packetsProcessed++;
    }

    #endregion
}
