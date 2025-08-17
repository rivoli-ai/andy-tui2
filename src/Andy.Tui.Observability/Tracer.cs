using System.Diagnostics;

namespace Andy.Tui.Observability;

public static class Tracer
{
    public static IDisposable BeginSpan(string category, string name)
    {
        var logger = ComprehensiveLoggingInitializer.GetLogger(category);
        logger.Debug($"span {name} start");
        var sw = Stopwatch.StartNew();
        return new Span(logger, name, sw);
    }

    private sealed class Span : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _name;
        private readonly Stopwatch _sw;
        private bool _disposed;

        public Span(ILogger logger, string name, Stopwatch sw)
        {
            _logger = logger;
            _name = name;
            _sw = sw;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _sw.Stop();
            _logger.Debug($"span {_name} end {_sw.ElapsedMilliseconds}ms");
            _disposed = true;
        }
    }
}
