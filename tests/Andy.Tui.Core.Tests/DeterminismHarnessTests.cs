using System;
using Andy.Tui.Core.Determinism;
using Xunit;

namespace Andy.Tui.Core.Tests;

public class DeterminismHarnessTests
{
    [Fact]
    public void Scheduler_Drain_Limits_Steps()
    {
        var sched = new DeterministicScheduler(new ManualClock());
        int calls = 0;
        for (int i = 0; i < 10; i++) sched.Enqueue(() => calls++);
        var steps = sched.Drain(maxSteps: 3);
        Assert.Equal(3, steps);
        Assert.Equal(3, calls);
    }
}
