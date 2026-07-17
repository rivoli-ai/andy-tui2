using System;
using System.Collections.Generic;
using Andy.Tui.Compose;
using Xunit;

namespace Andy.Tui.Compose.Tests;

public class EffectsOrderingTests
{
    [Fact]
    public void Layout_Effects_Run_Before_Paint_Effects_On_Mount()
    {
        var log = new List<string>();

        var root = new VComponent(ctx =>
        {
            // Register paint first, layout second, to prove ordering is by phase,
            // not by registration order.
            ctx.UseEffect(() => { log.Add("paint"); return null; }, EffectPhase.Paint, Array.Empty<object?>());
            ctx.UseEffect(() => { log.Add("layout"); return null; }, EffectPhase.Layout, Array.Empty<object?>());
            return new VText("x");
        });

        var composer = new Composer(root);
        composer.Recompose();

        Assert.Equal(new[] { "layout", "paint" }, log);
    }

    [Fact]
    public void Cleanups_Run_In_Reverse_Registration_Order_On_Unmount()
    {
        var log = new List<string>();

        var root = new VComponent(ctx =>
        {
            ctx.UseEffect(() => { log.Add("setup-layout"); return () => log.Add("cleanup-layout"); }, EffectPhase.Layout, Array.Empty<object?>());
            ctx.UseEffect(() => { log.Add("setup-paint"); return () => log.Add("cleanup-paint"); }, EffectPhase.Paint, Array.Empty<object?>());
            return new VText("x");
        });

        var composer = new Composer(root);
        composer.Recompose();
        Assert.Equal(new[] { "setup-layout", "setup-paint" }, log);

        log.Clear();
        composer.Unmount();

        // Cleanups run in reverse of registration order.
        Assert.Equal(new[] { "cleanup-paint", "cleanup-layout" }, log);
    }

    [Fact]
    public void Effect_With_Empty_Deps_Runs_Once_Across_Frames()
    {
        var runs = 0;
        var scheduler = new ManualFrameScheduler();
        StateRef<int> state = default;

        var root = new VComponent(ctx =>
        {
            state = ctx.UseState(0);
            ctx.UseEffect(() => { runs++; return null; }, EffectPhase.Paint, Array.Empty<object?>());
            return new VText($"{state.Value}");
        });

        var composer = new Composer(root, scheduler);
        composer.Recompose();
        Assert.Equal(1, runs);

        // A state change causes a re-render, but the empty-deps effect must not re-run.
        state.Set(1);
        scheduler.Flush();
        Assert.Equal(1, runs);
    }

    [Fact]
    public void Effect_Reruns_When_Deps_Change_Running_Cleanup_First()
    {
        var log = new List<string>();
        var scheduler = new ManualFrameScheduler();
        StateRef<int> state = default;

        var root = new VComponent(ctx =>
        {
            state = ctx.UseState(0);
            int dep = state.Value;
            ctx.UseEffect(() =>
            {
                log.Add($"setup:{dep}");
                return () => log.Add($"cleanup:{dep}");
            }, EffectPhase.Paint, new object?[] { dep });
            return new VText($"{dep}");
        });

        var composer = new Composer(root, scheduler);
        composer.Recompose();
        Assert.Equal(new[] { "setup:0" }, log);

        state.Set(1);
        scheduler.Flush();

        // The previous effect's cleanup runs before the new effect.
        Assert.Equal(new[] { "setup:0", "cleanup:0", "setup:1" }, log);
    }

    [Fact]
    public void Unmounting_A_Removed_Child_Releases_Its_Effects()
    {
        var log = new List<string>();
        var show = new List<string> { "a" };

        var root = new VComponent(_ =>
        {
            var stack = new VElement("stack");
            foreach (var key in show)
            {
                var capturedKey = key;
                var child = new VComponent(ctx =>
                {
                    ctx.UseEffect(() =>
                    {
                        log.Add($"subscribe:{capturedKey}");
                        return () => log.Add($"dispose:{capturedKey}");
                    }, EffectPhase.Paint, Array.Empty<object?>());
                    return new VText(capturedKey);
                });
                child.WithKey(capturedKey);
                stack.AddChild(child);
            }
            return stack;
        });

        var composer = new Composer(root);
        composer.Recompose();
        Assert.Equal(new[] { "subscribe:a" }, log);

        // Remove the child; its subscription must be disposed.
        show.Clear();
        composer.Recompose();
        Assert.Equal(new[] { "subscribe:a", "dispose:a" }, log);
    }
}
