using System.Diagnostics;

namespace Andy.Tui.Observability;

public static class Tracer
{
    private static ITraceSink? _sink;
    public static void SetSink(ITraceSink sink) => _sink = sink;

    public static IDisposable BeginSpan(string category, string name)
    {
        var logger = ComprehensiveLoggingInitializer.GetLogger(category);
        logger.Debug($"span {name} start");
        var sw = Stopwatch.StartNew();
        var span = new Span(logger, category, name, sw, _sink);
        _sink?.OnBegin(category, name, 0);
        return span;
    }

    private sealed class Span : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _category;
        private readonly string _name;
        private readonly Stopwatch _sw;
        private readonly ITraceSink? _sink;
        private bool _disposed;

        public Span(ILogger logger, string category, string name, Stopwatch sw, ITraceSink? sink)
        {
            _logger = logger;
            _category = category;
            _name = name;
            _sw = sw;
            _sink = sink;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _sw.Stop();
            _logger.Debug($"span {_name} end {_sw.ElapsedMilliseconds}ms");
            _sink?.OnEnd(_category, _name, _sw.ElapsedMilliseconds);
            _disposed = true;
        }
    }
}

public interface ITraceSink
{
    void OnBegin(string category, string name, long tsMs);
    void OnEnd(string category, string name, long durMs);
}

public sealed class ChromeTraceSink : ITraceSink
{
    private readonly List<string> _events = new();
    private readonly object _gate = new();
    private long _timeOriginMs = Environment.TickCount64;

    public void OnBegin(string category, string name, long tsMs)
    {
        var ts = Environment.TickCount64 - _timeOriginMs;
        lock (_gate)
        {
            _events.Add($"{{\"cat\":\"{category}\",\"name\":\"{name}\",\"ph\":\"B\",\"ts\":{ts}}}");
        }
    }
    public void OnEnd(string category, string name, long durMs)
    {
        var ts = Environment.TickCount64 - _timeOriginMs;
        lock (_gate)
        {
            _events.Add($"{{\"cat\":\"{category}\",\"name\":\"{name}\",\"ph\":\"E\",\"ts\":{ts}}}");
        }
    }

    public string ToChromeTraceJson()
    {
        lock (_gate)
        {
            return $"{{\"traceEvents\":[{string.Join(",", _events)}]}}";
        }
    }
}
