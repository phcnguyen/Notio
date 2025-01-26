﻿using System;

namespace Notio.FileStorage.Extensions;

/// <summary>
/// Represents a URL expiration mechanism that can either be defined in seconds or as an exact UTC DateTime.
/// </summary>
public class UrlExpiration
{
    /// <summary>
    /// Gets the expiration time in seconds.
    /// </summary>
    public uint InSeconds { get; private set; }

    /// <summary>
    /// Determines whether the expiration is enabled.
    /// </summary>
    /// <value>True if expiration time is greater than zero; otherwise, false.</value>
    public bool IsEnabled => InSeconds > 0;

    /// <summary>
    /// Gets the UTC DateTime when the URL will expire.
    /// </summary>
    /// <value>The expiration DateTime in UTC.</value>
    public DateTime InDateTime => DateTime.UtcNow.AddSeconds(InSeconds);

    /// <summary>
    /// Initializes a new instance of the <see cref="UrlExpiration"/> class with a specified expiration time in seconds.
    /// </summary>
    /// <param name="seconds">The expiration time in seconds. Default is 0 (no expiration).</param>
    public UrlExpiration(uint seconds = 0) => InSeconds = seconds;

    /// <summary>
    /// Initializes a new instance of the <see cref="UrlExpiration"/> class with a specified expiration DateTime in UTC.
    /// </summary>
    /// <param name="utcDate">The exact UTC expiration DateTime.</param>
    /// <exception cref="OverflowException">Thrown when the time difference exceeds the maximum value of a <see cref="uint"/>.</exception>
    public UrlExpiration(DateTime utcDate)
    {
        var diff = Convert.ToUInt32((utcDate - DateTime.UtcNow).TotalSeconds);

        if (diff > uint.MaxValue || diff < 0)
            throw new OverflowException();

        InSeconds = Convert.ToUInt32(diff);
    }
}