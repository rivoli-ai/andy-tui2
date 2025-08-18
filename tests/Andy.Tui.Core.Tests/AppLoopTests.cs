using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using Andy.Tui.Core;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Core.Tests;

file sealed class CapturingPty : IPtyIo
{
    public int Writes;
    public string Last = string.Empty;
    public Task WriteAsync(ReadOnlyMemory<byte> frameBytes, CancellationToken cancellationToken)
    {
        Writes++;
        Last = Encoding.UTF8.GetString(frameBytes.Span);
        return Task.CompletedTask;
    }
}

public class AppLoopTests
{
    [Fact]
    public async Task RunOnce_Renders_Built_DisplayList()
    {
        var bus = new InvalidationBus();
        var sched = new FrameScheduler(new SimpleManualClock());
        var builder = new DisplayListBuilder();
        builder.PushClip(new ClipPush(0, 0, 5, 1));
        builder.DrawText(new TextRun(0, 0, "A", new Rgb24(255, 255, 255), null, CellAttrFlags.None));
        builder.Pop();
        var caps = new TerminalCapabilities { TrueColor = true, Palette256 = true };
        var pty = new CapturingPty();
        var loop = new AppLoop(bus, sched, () => builder.Build(), (5, 1), caps, pty);
        await loop.RunOnceAsync(CancellationToken.None);
        Assert.Contains("A", pty.Last);
    }

    [Fact]
    public async Task RunForEvents_Renders_Per_Requested_Recompose()
    {
        var bus = new InvalidationBus();
        var sched = new FrameScheduler(new SimpleManualClock());
        int builds = 0;
        DisplayList.DisplayList Build()
        {
            builds++;
            var b = new DisplayListBuilder();
            b.PushClip(new ClipPush(0, 0, 5, 1));
            b.DrawText(new TextRun(0, 0, builds.ToString(), new Rgb24(255, 255, 255), null, CellAttrFlags.None));
            b.Pop();
            return b.Build();
        }
        var caps = new TerminalCapabilities { TrueColor = true, Palette256 = true };
        var pty = new CapturingPty();
        var loop = new AppLoop(bus, sched, Build, (5, 1), caps, pty);
        using var cts = new CancellationTokenSource();
        var task = loop.RunForEventsAsync(3, cts.Token);
        // Trigger three recomposes
        bus.RequestRecompose();
        bus.RequestRecompose();
        bus.RequestRecompose();
        var rendered = await task;
        Assert.Equal(3, rendered);
    }
}
