using Andy.Tui.Core.Determinism;
using Xunit;

namespace Andy.Tui.Core.Tests;

public class StubbedIoTests
{
    [Fact]
    public void StubbedOutput_Records_Writes_With_Timestamp()
    {
        var clock = new ManualClock(100);
        var outp = new StubbedOutput(clock);
        outp.Write("hello");
        clock.AdvanceTicks(5);
        outp.Write("world");
        Assert.Equal(2, outp.Entries.Count);
        Assert.Equal((100, "hello"), outp.Entries[0]);
        Assert.Equal((105, "world"), outp.Entries[1]);
    }
}
