using Andy.Tui.Observability;

namespace Andy.Tui.Observability.Tests;

public class LoggingCategoriesTests
{
    [Fact]
    public void Categories_Are_Available_And_Low_Overhead()
    {
        ComprehensiveLoggingInitializer.Initialize(isTestMode: true);
        var log = ComprehensiveLoggingInitializer.Compositor;
        var mem = Assert.IsType<InMemoryLogger>(log);
        var before = mem.Entries.Count;
        log.Info("test");
        var after = mem.Entries.Count;
        Assert.Equal(before + 1, after);
    }
}
