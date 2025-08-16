using Andy.Tui.Observability;
using Xunit;

namespace Andy.Tui.Observability.Tests;

public class LoggerTests
{
    [Fact]
    public void NoopLogger_Discards_All_Messages()
    {
        var factory = LoggerFactory.CreateNoop();
        var log = factory.CreateLogger("test");
        log.Trace("t");
        log.Debug("d");
        log.Info("i");
        log.Warn("w");
        log.Error("e");
        // No observable behavior; just ensure no exceptions
    }

    [Fact]
    public void InMemoryLogger_Records_In_RingBuffer_Order()
    {
        var factory = LoggerFactory.CreateInMemory(capacity: 3);
        var log = factory.CreateLogger("cat");
        log.Info("1");
        log.Warn("2");
        log.Error("3");
        log.Debug("4"); // overwrites oldest

        var mem = Assert.IsType<InMemoryLogger>(log);
        var entries = mem.Entries;
        Assert.Equal(3, entries.Count);
        Assert.Equal((LogLevel.Warn, "2"), entries[0]);
        Assert.Equal((LogLevel.Error, "3"), entries[1]);
        Assert.Equal((LogLevel.Debug, "4"), entries[2]);
    }

    [Fact]
    public void LoggerFactory_Caches_By_Category()
    {
        var factory = LoggerFactory.CreateInMemory();
        var a = factory.CreateLogger("foo");
        var b = factory.CreateLogger("foo");
        var c = factory.CreateLogger("bar");
        Assert.Same(a, b);
        Assert.NotSame(a, c);
    }
}
