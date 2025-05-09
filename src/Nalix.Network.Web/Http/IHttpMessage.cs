using Nalix.Network.Web.Net;
using System;

namespace Nalix.Network.Web.Http;

/// <summary>
/// Represents a HTTP request or response.
/// </summary>
public interface IHttpMessage
{
    /// <summary>
    /// Gets the cookies.
    /// </summary>
    /// <value>
    /// The cookies.
    /// </value>
    ICookieCollection Cookies { get; }

    /// <summary>
    /// Gets or sets the protocol version.
    /// </summary>
    /// <value>
    /// The protocol version.
    /// </value>
    Version ProtocolVersion { get; }
}