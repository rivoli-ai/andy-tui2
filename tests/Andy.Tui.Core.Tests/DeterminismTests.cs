using System;
using Andy.Tui.Core.Determinism;
using Xunit;

namespace Andy.Tui.Core.Tests;

public class DeterminismTests
{
    [Fact]
    public void ManualClock_Advances_Ticks()
    {
        var clock = new ManualClock();
        Assert.Equal(0, clock.NowTicks);
        clock.AdvanceTicks(5);
        Assert.Equal(5, clock.NowTicks);
    }

    [Fact]
    public void Scheduler_Enqueue_And_Drain_Runs_Actions()
    {
        var scheduler = new DeterministicScheduler(new ManualClock());
        int calls = 0;
        scheduler.Enqueue(() => calls++);
        scheduler.Enqueue(() => calls += 2);
        var steps = scheduler.Drain();
        Assert.Equal(2, steps);
        Assert.Equal(3, calls);
    }
}
