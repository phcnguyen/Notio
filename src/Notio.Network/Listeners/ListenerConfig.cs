using Notio.Shared.Configuration;

using System.ComponentModel.DataAnnotations;

namespace Notio.Network.Listeners;

/// <summary>
/// Represents network configuration settings for socket and TCP connections.
/// </summary>
public sealed class ListenerConfig : ConfiguredBinder
{
    /// <summary>
    /// Gets or sets the port number for the network connection.
    /// Must be within the range of 1 to 65535.
    /// Default is 5000.
    /// </summary>
    [Range(1, 65535)]
    public int Port { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the buffer size for receiving data.
    /// The value must be at least 64KB.
    /// Default is 64KB (65536 bytes).
    /// </summary>
    [Range(1024, int.MaxValue)]
    public int ReceiveBufferSize { get; set; } = 64 * 1024;

    /// <summary>
    /// Gets or sets the buffer size for sending data.
    /// The value must be at least 64KB.
    /// Default is 64KB (65536 bytes).
    /// </summary>
    [Range(1024, int.MaxValue)]
    public int SendBufferSize { get; set; } = 64 * 1024;

    /// <summary>
    /// Gets or sets the linger timeout in seconds.
    /// Default is 30 seconds.
    /// </summary>
    public int LingerTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the timeout for receiving data in milliseconds.
    /// Default is 5000 milliseconds.
    /// </summary>
    public int ReceiveTimeoutMilliseconds { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the timeout for sending data in milliseconds.
    /// Default is 5000 milliseconds.
    /// </summary>
    public int SendTimeoutMilliseconds { get; set; } = 5000;

    /// <summary>
    /// Gets or sets whether Nagle's algorithm is disabled (low-latency communication).
    /// Default is true.
    /// </summary>
    public bool NoDelay { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the socket can reuse an address already in the TIME_WAIT state.
    /// Default is false.
    /// </summary>
    public bool ReuseAddress { get; set; } = false;
}
