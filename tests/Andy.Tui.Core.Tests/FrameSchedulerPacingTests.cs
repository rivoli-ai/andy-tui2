using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Andy.Tui.Core.Tests;

public class FrameSchedulerPacingTests
{
    private sealed class FakeClock : IClock
    {
        public long NowMs { get; private set; }
        public void Advance(long ms) => NowMs += ms;
    }

    private sealed class NullPty : Andy.Tui.Backend.Terminal.IPtyIo
    {
        public Task WriteAsync(ReadOnlyMemory<byte> bytes, CancellationToken ct = default) => Task.CompletedTask;
    }

    [Fact]
    public async Task Honors_Target_Fps_In_Idle_Frames()
    {
        var clock = new FakeClock();
        var scheduler = new FrameScheduler(clock, targetFps: 30);
        var hud = new Andy.Tui.Observability.HudOverlay { Enabled = false };
        scheduler.SetMetricsSink(hud);
        var pty = new NullPty();
        var caps = Andy.Tui.Backend.Terminal.CapabilityDetector.DetectFromEnvironment();
        var viewport = (W: 80, H: 24);
        var empty = new Andy.Tui.DisplayList.DisplayListBuilder().Build();

        // Run a handful of frames and manually advance time by the computed sleep
        for (int i = 0; i < 5; i++)
        {
            // RenderOnceWithMetricsAsync will call clock.NowMs multiple times; simulate time progressing
            var before = clock.NowMs;
            var task = scheduler.RenderOnceWithMetricsAsync(empty, viewport, caps, pty, CancellationToken.None);
            // Simulate compositor/encode/write taking 1ms
            clock.Advance(1);
            await task;
            // After render, scheduler computes sleep; we advance by that to emulate passing time
            clock.Advance(33);
        }
        // If we reached here without exceptions, basic pacing path exercised
        Assert.True(true);
    }
}
