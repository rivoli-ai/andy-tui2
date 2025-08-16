using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Andy.Tui.Observability;

/// <summary>
/// A minimal logger implementation suitable for Phase 0. Provides
/// a no-op logger and an in-memory logger for tests and diagnostics.
/// </summary>
public sealed class LoggerFactory : ILoggerFactory
{
    private readonly ConcurrentDictionary<string, ILogger> _cache = new();
    private readonly Func<string, ILogger> _creator;

    private LoggerFactory(Func<string, ILogger> creator)
    {
        _creator = creator;
    }

    /// <summary>
    /// Creates a factory that returns a no-op logger for every category.
    /// </summary>
    public static LoggerFactory CreateNoop() => new(category => new NoopLogger());

    /// <summary>
    /// Creates a factory that returns an in-memory logger for each category.
    /// </summary>
    public static LoggerFactory CreateInMemory(int capacity = 1024) => new(category => new InMemoryLogger(capacity));

    /// <summary>
    /// Creates or returns a cached logger for the given category.
    /// </summary>
    public ILogger CreateLogger(string category) => _cache.GetOrAdd(category, _creator);
}

/// <summary>
/// A logger that discards all messages.
/// </summary>
public sealed class NoopLogger : ILogger
{
    /// <inheritdoc />
    public void Log(LogLevel level, string message) { }
    /// <inheritdoc />
    public void Trace(string message) { }
    /// <inheritdoc />
    public void Debug(string message) { }
    /// <inheritdoc />
    public void Info(string message) { }
    /// <inheritdoc />
    public void Warn(string message) { }
    /// <inheritdoc />
    public void Error(string message) { }
}

/// <summary>
/// A simple in-memory logger that stores the most recent log entries
/// in a ring buffer, useful for unit tests and diagnostics.
/// </summary>
public sealed class InMemoryLogger : ILogger
{
    private readonly (LogLevel Level, string Message)[] _buffer;
    private int _index;
    private int _count;

    /// <summary>
    /// Creates a new in-memory logger with the specified capacity.
    /// </summary>
    public InMemoryLogger(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _buffer = new (LogLevel, string)[capacity];
        _index = 0;
        _count = 0;
    }

    /// <summary>
    /// Gets a snapshot of the current entries in chronological order.
    /// </summary>
    public IReadOnlyList<(LogLevel Level, string Message)> Entries
    {
        get
        {
            var result = new List<(LogLevel, string)>(_count);
            int start = (_index - _count + _buffer.Length) % _buffer.Length;
            for (int i = 0; i < _count; i++)
            {
                var idx = (start + i) % _buffer.Length;
                result.Add(_buffer[idx]);
            }
            return result;
        }
    }

    /// <inheritdoc />
    public void Log(LogLevel level, string message)
    {
        _buffer[_index] = (level, message);
        _index = (_index + 1) % _buffer.Length;
        if (_count < _buffer.Length) _count++;
    }

    /// <inheritdoc />
    public void Trace(string message) => Log(LogLevel.Trace, message);
    /// <inheritdoc />
    public void Debug(string message) => Log(LogLevel.Debug, message);
    /// <inheritdoc />
    public void Info(string message) => Log(LogLevel.Info, message);
    /// <inheritdoc />
    public void Warn(string message) => Log(LogLevel.Warn, message);
    /// <inheritdoc />
    public void Error(string message) => Log(LogLevel.Error, message);
}
