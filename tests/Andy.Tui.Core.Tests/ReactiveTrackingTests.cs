using System;
using Andy.Tui.Core.Reactive;
using Xunit;

namespace Andy.Tui.Core.Tests;

/// <summary>
/// Tests for automatic reactive dependency tracking, deterministic disposal,
/// cycle detection, and re-entrancy control (issue #32).
/// </summary>
public class ReactiveTrackingTests
{
    [Fact]
    public void Computed_AutoTracks_Signal_Without_Manual_Subscription()
    {
        var a = new Signal<int>(1);
        int computeCalls = 0;
        var c = new Computed<int>(() =>
        {
            computeCalls++;
            return a.Value * 2;
        });

        Assert.Equal(2, c.Value);
        Assert.Equal(1, computeCalls);

        // Cached until dependency changes.
        Assert.Equal(2, c.Value);
        Assert.Equal(1, computeCalls);

        // No manual wiring: changing the signal must invalidate automatically.
        a.Value = 5;
        Assert.Equal(10, c.Value);
        Assert.Equal(2, computeCalls);
    }

    [Fact]
    public void Computed_AutoTracks_Multiple_Signals()
    {
        var a = new Signal<int>(2);
        var b = new Signal<int>(3);
        var c = new Computed<int>(() => a.Value + b.Value);

        Assert.Equal(5, c.Value);

        a.Value = 10;
        Assert.Equal(13, c.Value);

        b.Value = 20;
        Assert.Equal(30, c.Value);
    }

    [Fact]
    public void Computed_Tracks_Dynamic_Dependencies()
    {
        var toggle = new Signal<bool>(true);
        var a = new Signal<int>(1);
        var b = new Signal<int>(100);

        int computeCalls = 0;
        var c = new Computed<int>(() =>
        {
            computeCalls++;
            return toggle.Value ? a.Value : b.Value;
        });

        // Initially depends on toggle and a (not b).
        Assert.Equal(1, c.Value);
        Assert.Equal(1, computeCalls);

        // Changing b must NOT invalidate while the active branch reads a.
        b.Value = 200;
        Assert.Equal(1, c.Value);
        Assert.Equal(1, computeCalls);

        // Changing a DOES invalidate.
        a.Value = 2;
        Assert.Equal(2, c.Value);
        Assert.Equal(2, computeCalls);

        // Flip the branch: now depends on b, not a.
        toggle.Value = false;
        Assert.Equal(200, c.Value);
        int callsAfterFlip = computeCalls;

        // a is no longer a dependency -> no recompute.
        a.Value = 999;
        Assert.Equal(200, c.Value);
        Assert.Equal(callsAfterFlip, computeCalls);

        // b is now a dependency -> recompute.
        b.Value = 300;
        Assert.Equal(300, c.Value);
        Assert.Equal(callsAfterFlip + 1, computeCalls);
    }

    [Fact]
    public void Nested_Computed_AutoTracks_Through_Chain()
    {
        var a = new Signal<int>(1);
        var b = new Signal<int>(2);
        var sum = new Computed<int>(() => a.Value + b.Value);
        var doubled = new Computed<int>(() => sum.Value * 2);

        Assert.Equal(6, doubled.Value); // (1+2)*2

        a.Value = 10;
        Assert.Equal(24, doubled.Value); // (10+2)*2

        b.Value = 8;
        Assert.Equal(36, doubled.Value); // (10+8)*2
    }

    [Fact]
    public void Computed_ValueChanged_Delivers_New_Value()
    {
        var a = new Signal<int>(1);
        var c = new Computed<int>(() => a.Value * 2);

        int raised = 0;
        int? delivered = null;
        c.ValueChanged += (_, v) => { raised++; delivered = v; };

        // Prime.
        Assert.Equal(2, c.Value);

        a.Value = 5;
        Assert.Equal(1, raised);
        Assert.Equal(10, delivered); // new value, not the old cached 2
    }

    [Fact]
    public void Computed_ValueChanged_Does_Not_Fire_When_Value_Unchanged()
    {
        var a = new Signal<int>(4);
        // Computed value depends only on parity, so many signal changes leave it stable.
        var isEven = new Computed<bool>(() => a.Value % 2 == 0);

        int raised = 0;
        isEven.ValueChanged += (_, _) => raised++;
        Assert.True(isEven.Value);

        a.Value = 6; // still even -> value unchanged -> no event
        Assert.Equal(0, raised);

        a.Value = 7; // now odd -> value changed -> event
        Assert.Equal(1, raised);
    }

    [Fact]
    public void Cycle_Is_Detected()
    {
        Computed<int> a = null!;
        Computed<int> b = null!;
        a = new Computed<int>(() => b.Value + 1);
        b = new Computed<int>(() => a.Value + 1);

        Assert.Throws<InvalidOperationException>(() => _ = a.Value);
    }

    [Fact]
    public void Effect_AutoTracks_And_Reruns_On_Change()
    {
        var a = new Signal<int>(1);
        int runs = 0;
        int lastSeen = 0;
        using var e = new Effect(() =>
        {
            runs++;
            lastSeen = a.Value;
        });

        Assert.Equal(1, runs);
        Assert.Equal(1, lastSeen);

        a.Value = 42;
        Assert.Equal(2, runs);
        Assert.Equal(42, lastSeen);
    }

    [Fact]
    public void Effect_Tracks_Dynamic_Dependencies()
    {
        var toggle = new Signal<bool>(true);
        var a = new Signal<int>(1);
        var b = new Signal<int>(100);
        int runs = 0;

        using var e = new Effect(() =>
        {
            runs++;
            _ = toggle.Value ? a.Value : b.Value;
        });

        Assert.Equal(1, runs);

        // b not tracked yet.
        b.Value = 200;
        Assert.Equal(1, runs);

        // a tracked.
        a.Value = 2;
        Assert.Equal(2, runs);

        // Switch branch to depend on b.
        toggle.Value = false;
        int afterFlip = runs;

        a.Value = 3; // no longer tracked
        Assert.Equal(afterFlip, runs);

        b.Value = 300; // now tracked
        Assert.Equal(afterFlip + 1, runs);
    }

    [Fact]
    public void Disposed_Effect_Never_Runs_Again()
    {
        var a = new Signal<int>(1);
        int runs = 0;
        var e = new Effect(() => { runs++; _ = a.Value; });
        Assert.Equal(1, runs);

        e.Dispose();

        a.Value = 2;
        Assert.Equal(1, runs); // no rerun after disposal

        // Explicit Run is a no-op after disposal.
        e.Run();
        Assert.Equal(1, runs);
    }

    [Fact]
    public void Effect_Dispose_Is_Idempotent()
    {
        var a = new Signal<int>(1);
        var e = new Effect(() => { _ = a.Value; });
        e.Dispose();
        e.Dispose(); // must not throw
    }

    [Fact]
    public void Disposed_Computed_Releases_Dependency_References()
    {
        var a = new Signal<int>(1);
        int computeCalls = 0;
        var c = new Computed<int>(() => { computeCalls++; return a.Value * 2; });

        Assert.Equal(2, c.Value);
        Assert.Equal(1, computeCalls);

        c.Dispose();

        // After disposal the computed is no longer a dependent of the signal:
        // changing the signal must not trigger any recompute.
        a.Value = 100;
        Assert.Equal(1, computeCalls);

        // Idempotent.
        c.Dispose();
    }

    [Fact]
    public void Effect_Reentrant_Trigger_Does_Not_Recurse_Infinitely()
    {
        var a = new Signal<int>(0);
        int runs = 0;

        using var e = new Effect(() =>
        {
            runs++;
            // Writing a dependency from within the effect re-triggers the effect.
            // Without re-entrancy control this would recurse; the guard folds the
            // re-run iteratively and terminates at the fixpoint.
            if (a.Value < 5)
            {
                a.Value = a.Value + 1;
            }
        });

        // Initial run wrote a=1 before the subscription to 'a' was established.
        Assert.Equal(1, a.Value);
        int runsAfterInit = runs;

        // External nudge triggers the (now subscribed) effect, which drives 'a'
        // to its fixpoint via re-entrant writes that must not overflow the stack.
        a.Value = 2;
        Assert.Equal(5, a.Value);
        Assert.True(runs > runsAfterInit, "effect should have re-run after the external change");
    }

    [Fact]
    public void Diamond_Dependency_Recomputes_Correctly()
    {
        var a = new Signal<int>(1);
        var left = new Computed<int>(() => a.Value + 1);
        var right = new Computed<int>(() => a.Value * 2);
        var bottom = new Computed<int>(() => left.Value + right.Value);

        Assert.Equal(4, bottom.Value); // (1+1) + (1*2)

        a.Value = 5;
        Assert.Equal(16, bottom.Value); // (5+1) + (5*2)
    }

    [Fact]
    public void Signal_Peek_Does_Not_Register_Dependency()
    {
        var a = new Signal<int>(1);
        int computeCalls = 0;
        var c = new Computed<int>(() => { computeCalls++; return a.Peek() * 2; });

        Assert.Equal(2, c.Value);
        Assert.Equal(1, computeCalls);

        // Peek did not subscribe, so changing a does not invalidate.
        a.Value = 50;
        Assert.Equal(2, c.Value);
        Assert.Equal(1, computeCalls);
    }
}
