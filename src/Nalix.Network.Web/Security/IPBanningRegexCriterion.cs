using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nalix.Network.Web.Security;

/// <summary>
/// Represents a log message regex matching criterion for <see cref="IPBanningModule"/>.
/// </summary>
/// <seealso cref="IIPBanningCriterion" />
public class IPBanningRegexCriterion : IIPBanningCriterion
{
    /// <summary>
    /// The default matching period.
    /// </summary>
    public const int DefaultSecondsMatchingPeriod = 60;

    /// <summary>
    /// The default maximum match count per period.
    /// </summary>
    public const int DefaultMaxMatchCount = 10;

    private readonly ConcurrentDictionary<IPAddress, ConcurrentBag<long>> _failRegexMatches = new();
    private readonly ConcurrentDictionary<string, Regex> _failRegex = new();
    private readonly IPBanningModule _parent;
    private readonly int _secondsMatchingPeriod;
    private readonly int _maxMatchCount;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="IPBanningRegexCriterion"/> class.
    /// </summary>
    /// <param name="parent">The parent.</param>
    /// <param name="rules">The rules.</param>
    /// <param name="maxMatchCount">The maximum match count.</param>
    /// <param name="secondsMatchingPeriod">The seconds matching period.</param>
    public IPBanningRegexCriterion(IPBanningModule parent, IEnumerable<string> rules, int maxMatchCount = DefaultMaxMatchCount, int secondsMatchingPeriod = DefaultSecondsMatchingPeriod)
    {
        _secondsMatchingPeriod = secondsMatchingPeriod;
        _maxMatchCount = maxMatchCount;
        _parent = parent;

        AddRules(rules);
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="IPBanningRegexCriterion"/> class.
    /// </summary>
    ~IPBanningRegexCriterion()
    {
        Dispose(false);
    }

    /// <inheritdoc />
    public Task<bool> ValidateIPAddress(IPAddress address)
    {
        long minTime = DateTime.Now.AddSeconds(-1 * _secondsMatchingPeriod).Ticks;
        bool shouldBan = _failRegexMatches.TryGetValue(address, out ConcurrentBag<long>? attempts) &&
                        attempts.Count(x => x >= minTime) >= _maxMatchCount;

        return Task.FromResult(shouldBan);
    }

    /// <inheritdoc />
    public void ClearIPAddress(IPAddress address)
    {
        _ = _failRegexMatches.TryRemove(address, out _);
    }

    /// <inheritdoc />
    public void PurgeData()
    {
        long minTime = DateTime.Now.AddSeconds(-1 * _secondsMatchingPeriod).Ticks;

        foreach (IPAddress k in _failRegexMatches.Keys)
        {
            if (!_failRegexMatches.TryGetValue(k, out ConcurrentBag<long>? failRegexMatches))
            {
                continue;
            }

            ConcurrentBag<long> recentMatches = [.. failRegexMatches.Where(x => x >= minTime)];
            if (recentMatches.IsEmpty)
            {
                _ = _failRegexMatches.TryRemove(k, out _);
            }
            else
            {
                _ = _failRegexMatches.AddOrUpdate(k, recentMatches, (x, y) => recentMatches);
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _failRegexMatches.Clear();
            _failRegex.Clear();
        }

        _disposed = true;
    }

    private void AddRules(IEnumerable<string> patterns)
    {
        foreach (string pattern in patterns)
        {
            AddRule(pattern);
        }
    }

    private void AddRule(string pattern)
    {
        try
        {
            _ = _failRegex.TryAdd(pattern, new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(500)));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Invalid regex - '{pattern}'. Exception: {ex}", nameof(IPBanningModule));
        }
    }
}
