using System;
using System.Collections.Concurrent;

namespace Andy.Tui.Observability;

/// <summary>
/// Minimal comprehensive logging initializer for Phase 2 baseline.
/// </summary>
public static class ComprehensiveLoggingInitializer
{
    private static bool _initialized;
    private static ILoggerFactory _factory = LoggerFactory.CreateNoop();
    public static ILogger DisplayList => GetLogger("DisplayList");
    public static ILogger Compositor => GetLogger("Compositor");
    public static ILogger Damage => GetLogger("Damage");
    public static ILogger Encoder => GetLogger("Encoder");
    public static ILogger Backend => GetLogger("Backend");

    public static void Initialize(bool isTestMode = false)
    {
        if (_initialized) return;
        _factory = isTestMode ? LoggerFactory.CreateInMemory() : LoggerFactory.CreateNoop();
        _initialized = true;
        DisplayList.Info("Logging initialized");
    }

    public static ILogger GetLogger(string category)
    {
        return _factory.CreateLogger(category);
    }
}
