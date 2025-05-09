namespace Nalix.Network.Protocols;

public abstract partial class Protocol
{
    #region Fields

    private ulong _totalErrors;
    private ulong _totalMessages;

    #endregion Fields

    #region Properties

    /// <summary>
    /// Total number of errors encountered during message processing.
    /// </summary>
    public ulong TotalErrors => System.Threading.Interlocked.Read(ref _totalErrors);

    /// <summary>
    /// Total number of messages processed by this protocol.
    /// </summary>
    public ulong TotalMessages => System.Threading.Interlocked.Read(ref _totalMessages);

    #endregion Properties

    /// <summary>
    /// Captures a diagnostic snapshot of the current protocol state,
    /// including connection acceptance status and message statistics.
    /// </summary>
    /// <returns>
    /// A <see cref="Snapshot.ProtocolSnapshot"/> containing metrics like
    /// total messages processed and total errors encountered.
    /// </returns>
    public virtual Snapshot.ProtocolSnapshot Snapshot() => new()
    {
        IsListening = IsAccepting,
        TotalMessages = System.Threading.Interlocked.Read(ref _totalMessages),
        TotalErrors = System.Threading.Interlocked.Read(ref _totalErrors),
    };
}
