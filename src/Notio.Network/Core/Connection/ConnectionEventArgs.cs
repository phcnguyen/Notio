using Notio.Common.Connection;
using System;

namespace Notio.Network.Core.Connection;

/// <inheritdoc />
public sealed class ConnectionEventArgs(Connection connection) : EventArgs, IConnectEventArgs
{
    /// <inheritdoc />
    public IConnection Connection { get; } = connection;
}
