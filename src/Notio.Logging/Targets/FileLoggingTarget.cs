using Notio.Common.Logging;
using Notio.Common.Models;
using Notio.Logging.Formatters;
using Notio.Logging.Internal.File;
using System;

namespace Notio.Logging.Targets;

/// <summary>
/// Standard file logger implementation that writes log messages to a file.
/// </summary>
/// <remarks>
/// This logger uses a specified formatter to format the log message before writing it to a file.
/// The default behavior can be customized by providing a custom <see cref="ILoggingFormatter"/>.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="FileLoggingTarget"/> class.
/// </remarks>
/// <param name="loggerFormatter">The log message formatter.</param>
/// <param name="fileLoggerOptions">The file logger options.</param>
public class FileLoggingTarget(ILoggingFormatter loggerFormatter, FileLoggerOptions fileLoggerOptions) : ILoggingTarget, IDisposable
{
    private readonly ILoggingFormatter _loggerFormatter = loggerFormatter ?? throw new ArgumentNullException(nameof(loggerFormatter));

    /// <summary>
    /// The provider responsible for writing logs to a file.
    /// </summary>
    public readonly FileLoggerProvider LoggerPrv = new(fileLoggerOptions);

    /// <summary>
    /// Initializes a new instance of the <see cref="FileLoggingTarget"/> with default log message formatting.
    /// </summary>
    public FileLoggingTarget()
        : this(new LoggingFormatter(false), new FileLoggerOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileLoggingTarget"/> with default log message formatting.
    /// </summary>
    /// <param name="options">A delegate to configure <see cref="FileLoggerOptions"/>.</param>
    public FileLoggingTarget(FileLoggerOptions options)
        : this(new LoggingFormatter(false), options)
    {
    }

    /// <summary>
    /// Publishes the formatted log entry to the log file.
    /// </summary>
    /// <param name="logMessage">The log entry to be published.</param>
    public void Publish(LoggingEntry logMessage)
        => LoggerPrv.WriteEntry(_loggerFormatter.FormatLog(logMessage));

    /// <summary>
    /// Disposes of the file logger and any resources it uses.
    /// </summary>
    public void Dispose()
    {
        LoggerPrv?.Dispose();
        GC.SuppressFinalize(this);
    }
}
