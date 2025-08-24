using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using Andy.Tui.Core;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Core.Tests;

file sealed class BufferPty : IPtyIo
{
    public int Writes;
    public string Last;
    public Task WriteAsync(ReadOnlyMemory<byte> frameBytes, CancellationToken cancellationToken)
    {
        Writes++;
        Last = Encoding.UTF8.GetString(frameBytes.Span);
        return Task.CompletedTask;
    }
}

public class FrameSchedulerTests
{
    [Fact]
    public async Task RenderOnce_Writes_Frame_And_Respects_Target()
    {
        var dlb = new DisplayListBuilder();
        dlb.PushClip(new ClipPush(0, 0, 10, 1));
        dlb.DrawText(new TextRun(0, 0, "x", new Rgb24(255, 255, 255), null, CellAttrFlags.None));
        dlb.Pop();
        var dl = dlb.Build();
        var pty = new BufferPty();
        var clock = new SimpleManualClock();
        var sched = new FrameScheduler(clock, targetFps: 60);
        var caps = new TerminalCapabilities { TrueColor = true, Palette256 = true };

        var task = sched.RenderOnceAsync(dl, (10, 1), caps, pty, CancellationToken.None);
        // Advance clock so elapsed < target
        clock.Advance(5);
        await task;
        Assert.Equal(1, pty.Writes);
        Assert.Contains("x", pty.Last);
    }

    [Fact]
    public async Task FpsEma_And_DirtyPercent_Smoke()
    {
        var clock = new SimpleManualClock();
        var sched = new FrameScheduler(clock, targetFps: 60);
        var hud = new Andy.Tui.Observability.HudOverlay { Enabled = false };
        sched.SetMetricsSink(hud);
        var caps = new TerminalCapabilities { TrueColor = true, Palette256 = true };
        var pty = new BufferPty();

        // Frame 1: background only
        var b1 = new DisplayListBuilder();
        b1.PushClip(new ClipPush(0, 0, 10, 3));
        b1.DrawRect(new Rect(0, 0, 10, 3, new Rgb24(0, 0, 0)));
        b1.Pop();
        await sched.RenderOnceAsync(b1.Build(), (10, 3), caps, pty, CancellationToken.None);
        var fps1 = hud.Fps;

        // Advance clock by 100ms between frames to force low FPS on next sample
        clock.Advance(100);

        // Frame 2: same background + one changed cell
        var b2 = new DisplayListBuilder();
        b2.PushClip(new ClipPush(0, 0, 10, 3));
        b2.DrawRect(new Rect(0, 0, 10, 3, new Rgb24(0, 0, 0)));
        b2.DrawText(new TextRun(0, 0, "X", new Rgb24(255, 255, 255), null, CellAttrFlags.None));
        b2.Pop();
        await sched.RenderOnceAsync(b2.Build(), (10, 3), caps, pty, CancellationToken.None);

        Assert.True(hud.Fps > 0);
        Assert.True(hud.Fps < fps1); // EMA should decrease after a long interval
        Assert.InRange(hud.DirtyPercent, 0.02, 0.05); // 1 of 30 cells ≈ 3.3%
    }

    [Fact]
    public async Task Resize_Triggers_Full_Repaint_And_Preserves_Content()
    {
        var clock = new SimpleManualClock();
        var sched = new FrameScheduler(clock, targetFps: 1000);
        var caps = new TerminalCapabilities { TrueColor = true, Palette256 = true };
        var pty = new BufferPty();

        DisplayList.DisplayList BuildDl()
        {
            var b = new DisplayListBuilder();
            b.PushClip(new ClipPush(0, 0, 40, 6));
            b.DrawRect(new Rect(0, 0, 40, 6, new Rgb24(0, 0, 0)));
            b.DrawText(new TextRun(1, 1, "HEADER", new Rgb24(255, 255, 255), null, CellAttrFlags.Bold));
            b.DrawText(new TextRun(1, 2, "ROW1", new Rgb24(200, 200, 200), null, CellAttrFlags.None));
            b.DrawText(new TextRun(1, 3, "ROW2", new Rgb24(200, 200, 200), null, CellAttrFlags.None));
            b.Pop();
            return b.Build();
        }

        await sched.RenderOnceAsync(BuildDl(), (80, 24), caps, pty, CancellationToken.None);
        clock.Advance(16);
        await sched.RenderOnceAsync(BuildDl(), (60, 20), caps, pty, CancellationToken.None);
        clock.Advance(16);
        await sched.RenderOnceAsync(BuildDl(), (80, 24), caps, pty, CancellationToken.None);

        Assert.Contains("\u001b[2J", pty.Last); // clear screen on resize
        Assert.Contains("HEADER", pty.Last);
        Assert.Contains("ROW1", pty.Last);
        Assert.Contains("ROW2", pty.Last);
    }
}
