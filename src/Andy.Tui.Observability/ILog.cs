namespace Andy.Tui.Observability;

/// <summary>
/// Log severity levels in ascending order of importance.
/// </summary>
public enum LogLevel
{
    /// <summary>Highly verbose diagnostic information.</summary>
    Trace,
    /// <summary>Verbose debugging information.</summary>
    Debug,
    /// <summary>Informational messages about normal operation.</summary>
    Info,
    /// <summary>Non-fatal issues that may require attention.</summary>
    Warn,
    /// <summary>Errors indicating failures that need investigation.</summary>
    Error
}

/// <summary>
/// Minimal logger interface with convenience methods per level.
/// </summary>
public interface ILogger
{
    /// <summary>Logs a message with the specified level.</summary>
    void Log(LogLevel level, string message);
    /// <summary>Logs a trace-level message.</summary>
    void Trace(string message);
    /// <summary>Logs a debug-level message.</summary>
    void Debug(string message);
    /// <summary>Logs an info-level message.</summary>
    void Info(string message);
    /// <summary>Logs a warning-level message.</summary>
    void Warn(string message);
    /// <summary>Logs an error-level message.</summary>
    void Error(string message);
}

/// <summary>
/// Factory for creating and caching loggers by category.
/// </summary>
public interface ILoggerFactory
{
    /// <summary>Creates or returns a cached logger for a category.</summary>
    ILogger CreateLogger(string category);
}
