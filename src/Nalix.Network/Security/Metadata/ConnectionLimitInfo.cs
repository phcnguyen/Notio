using System;
using System.Diagnostics;

namespace Nalix.Network.Security.Metadata;

/// <summary>
/// Stores connection tracking data for an IP address.
/// Optimized as a readonly record struct for performance and memory usage.
/// </summary>
[DebuggerDisplay("Current: {CurrentConnections}, Total: {TotalConnectionsToday}, Last: {LastConnectionTime}")]
internal readonly record struct ConnectionLimitInfo
{
    /// <summary>
    /// The current Number of active connections.
    /// </summary>
    public readonly int CurrentConnections { get; init; }

    /// <summary>
    /// When the most recent connection was established.
    /// </summary>
    public readonly DateTime LastConnectionTime { get; init; }

    /// <summary>
    /// The total Number of connections made today.
    /// </summary>
    public readonly int TotalConnectionsToday { get; init; }

    /// <summary>
    /// When the last cleanup operation was performed.
    /// </summary>
    public readonly DateTime LastCleanupTime { get; init; }

    /// <summary>
    /// Creates a new connection info record.
    /// </summary>
    public ConnectionLimitInfo(
        int currentConnections,
        DateTime lastConnectionTime,
        int totalConnectionsToday,
        DateTime lastCleanupTime)
    {
        CurrentConnections = currentConnections;
        LastConnectionTime = lastConnectionTime;
        TotalConnectionsToday = totalConnectionsToday;
        LastCleanupTime = lastCleanupTime;
    }
}
