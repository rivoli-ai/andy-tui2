using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using Andy.Tui.Core;
using Andy.Tui.DisplayList;
using Andy.Tui.Observability;

namespace Andy.Tui.Core.Tests;

public class HudIntegrationTests
{
    [Fact]
    public async Task Scheduler_Updates_Hud_Metrics()
    {
        var hud = new HudOverlay { Enabled = false };
        var sched = new FrameScheduler(new SimpleManualClock());
        sched.SetMetricsSink(hud);
        var caps = new TerminalCapabilities { TrueColor = true, Palette256 = true };
        var pty = new BufferPty3();
        var dlb = new DisplayListBuilder();
        dlb.PushClip(new ClipPush(0, 0, 5, 1));
        dlb.DrawText(new TextRun(0, 0, "A", new Rgb24(255, 255, 255), null, CellAttrFlags.None));
        dlb.Pop();
        await sched.RenderOnceAsync(dlb.Build(), (5, 1), caps, pty, CancellationToken.None);
        Assert.True(hud.BytesPerFrame > 0);
        Assert.True(hud.Fps >= 0);
        Assert.InRange(hud.DirtyPercent, 0.0, 1.0);
    }
}

file sealed class BufferPty3 : IPtyIo
{
    public Task WriteAsync(ReadOnlyMemory<byte> frameBytes, CancellationToken cancellationToken) => Task.CompletedTask;
}
