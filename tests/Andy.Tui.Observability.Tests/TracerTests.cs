using Andy.Tui.Observability;

namespace Andy.Tui.Observability.Tests;

public class TracerTests
{
    [Fact]
    public void Span_Logs_Start_And_End()
    {
        ComprehensiveLoggingInitializer.Initialize(isTestMode: true);
        var log = ComprehensiveLoggingInitializer.GetLogger("TestSpan");
        var mem = Assert.IsType<InMemoryLogger>(log);
        var before = mem.Entries.Count;
        using (Tracer.BeginSpan("TestSpan", "work"))
        {
        }
        var after = mem.Entries.Count;
        Assert.True(after >= before + 2);
    }

    [Fact]
    public void ChromeTraceSink_Collects_Begin_End()
    {
        ComprehensiveLoggingInitializer.Initialize(isTestMode: true);
        var sink = new ChromeTraceSink();
        Tracer.SetSink(sink);
        using (Tracer.BeginSpan("route", "event")) { }
        var json = sink.ToChromeTraceJson();
        Assert.Contains("\"traceEvents\":", json);
        Assert.Contains("\"ph\":\"B\"", json);
        Assert.Contains("\"ph\":\"E\"", json);
    }
}
