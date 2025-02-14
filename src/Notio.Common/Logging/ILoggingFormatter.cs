using Notio.Common.Models;

namespace Notio.Common.Logging;

/// <summary>
/// Interface defining a formatter for log messages.
/// </summary>
public interface ILoggingFormatter
{
    /// <summary>
    /// Formats a log message based on the provided information.
    /// </summary>
    /// <param name="message">The log message to format.</param>
    /// <returns>A formatted string representing the log message.</returns>
    string FormatLog(LoggingEntry message);
}
