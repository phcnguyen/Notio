using Notio.Common.Logging;
using Notio.Common.Models;
using Notio.Logging.Formatters;
using System;

namespace Notio.Logging.Targets;

/// <summary>
/// The ConsoleLoggingTarget class provides the ability to output log messages to the console,
/// with colors corresponding to the log severity levels.
/// </summary>
/// <remarks>
/// This class is initialized with a specific log formatting object.
/// </remarks>
/// <param name="loggerFormatter">The object responsible for formatting the log message.</param>
public sealed class ConsoleLoggingTarget(ILoggingFormatter loggerFormatter) : ILoggingTarget
{
    private readonly ILoggingFormatter _loggerFormatter = loggerFormatter;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleLoggingTarget"/> class with a default log formatter.
    /// </summary>
    public ConsoleLoggingTarget() : this(new LoggingFormatter(true))
    {
    }

    /// <summary>
    /// Outputs the log message to the console.
    /// </summary>
    /// <param name="logMessage">The log message to be outputted.</param>
    public void Publish(LoggingEntry logMessage)
        => Console.WriteLine(_loggerFormatter.FormatLog(logMessage));
}