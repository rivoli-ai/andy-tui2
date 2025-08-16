using System;
using Andy.Tui.Core.Determinism;

namespace Andy.Tui.Core.Tests.TestHarness;

public abstract class DeterministicTestBase : IDisposable
{
    protected ManualClock Clock { get; }
    protected DeterministicScheduler Scheduler { get; }

    protected DeterministicTestBase()
    {
        Clock = new ManualClock();
        Scheduler = new DeterministicScheduler(Clock);
    }

    public void Dispose()
    {
        // Drain any pending actions to keep tests isolated
        Scheduler.Drain();
    }
}
