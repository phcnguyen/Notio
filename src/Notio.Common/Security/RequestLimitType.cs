namespace Notio.Network.Security.Enums;

/// <summary>
/// Represents different levels of request limits that can be applied to a firewall configuration.
/// These levels define thresholds for the number of requests allowed.
/// </summary>
public enum RequestLimitType
{
    /// <summary>
    /// Represents a low request limit, typically allowing only a small number of requests.
    /// </summary>
    Low,

    /// <summary>
    /// Represents a medium request limit, allowing a moderate number of requests.
    /// </summary>
    Medium,

    /// <summary>
    /// Represents a high request limit, allowing a large number of requests.
    /// </summary>
    High,

    /// <summary>
    /// Represents an unlimited request limit, with no restrictions on the number of requests.
    /// </summary>
    Unlimited
}
