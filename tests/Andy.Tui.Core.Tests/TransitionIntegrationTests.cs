using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Animations;
using Andy.Tui.Backend.Terminal;
using Andy.Tui.Core;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Core.Tests;

public class TransitionIntegrationTests
{
    [Fact]
    public async Task Color_Transition_Changes_Text_Color_Over_Time()
    {
        var clock = new SimpleManualClock();
        var sched = new FrameScheduler(clock);
        var caps = new TerminalCapabilities { TrueColor = true, Palette256 = true };
        var pty = new BufferPty2();
        long start = clock.NowMs;
        TextRun BuildRun(long now)
        {
            var run = new TextRun(0, 0, "A", new Rgb24(0, 0, 0), null, CellAttrFlags.None);
            var tr = new TransitionColor(new Rgb24(0, 0, 0), new Rgb24(100, 0, 0), 1000);
            return ColorTransitionApplier.Apply(run, start, now, tr);
        }
        DisplayList.DisplayList BuildDl(long now)
        {
            var b = new DisplayListBuilder();
            b.PushClip(new ClipPush(0, 0, 5, 1));
            b.DrawText(BuildRun(now));
            b.Pop();
            return b.Build();
        }
        // Frame 1 at t=0
        var (bytes1, _) = await sched.RenderOnceWithMetricsAsync(BuildDl(clock.NowMs), (5, 1), caps, pty, CancellationToken.None);
        // Advance to mid
        clock.Advance(500);
        var (bytes2, _) = await sched.RenderOnceWithMetricsAsync(BuildDl(clock.NowMs), (5, 1), caps, pty, CancellationToken.None);
        Assert.NotEqual(bytes1, bytes2);
    }
}

file sealed class BufferPty2 : IPtyIo
{
    public Task WriteAsync(ReadOnlyMemory<byte> frameBytes, CancellationToken cancellationToken) => Task.CompletedTask;
}
