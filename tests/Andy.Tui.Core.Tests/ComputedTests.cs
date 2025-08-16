using System;
using Andy.Tui.Core.Reactive;
using Xunit;

namespace Andy.Tui.Core.Tests;

public class ComputedTests
{
    [Fact]
    public void Computed_Caches_Until_Invalidated()
    {
        int computeCalls = 0;
        var a = new Signal<int>(1);
        var c = new Computed<int>(() => { computeCalls++; return a.Value * 2; }, subscribeDependencies: invalidate =>
        {
            a.ValueChanged += (_, _) => invalidate();
        });

        // First read computes
        Assert.Equal(2, c.Value);
        Assert.Equal(1, computeCalls);
        // Second read uses cache
        Assert.Equal(2, c.Value);
        Assert.Equal(1, computeCalls);

        // Change dependency -> invalidate -> next read recomputes
        a.Value = 3;
        Assert.Equal(6, c.Value);
        Assert.Equal(2, computeCalls);
    }

    [Fact]
    public void Computed_Raises_ValueChanged_On_Invalidate()
    {
        var a = new Signal<int>(1);
        var c = new Computed<int>(() => a.Value * 2, invalidate => a.ValueChanged += (_, _) => invalidate());
        int raised = 0;
        c.ValueChanged += (_, _) => raised++;

        // Prime
        _ = c.Value;
        a.Value = 2; // invalidates
        Assert.Equal(1, raised);
    }

    [Fact]
    public void Dispose_Is_Safe_To_Call_Multiple_Times()
    {
        var s = new Signal<int>(1);
        var c = new Computed<int>(() => s.Value, invalidate => s.ValueChanged += (_, _) => invalidate());

        // Access value to ensure initialization path runs
        _ = c.Value;

        c.Dispose();
        // Should not throw if disposed again
        c.Dispose();
    }
}
