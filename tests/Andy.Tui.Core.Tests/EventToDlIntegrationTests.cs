using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using Andy.Tui.Core;
using Andy.Tui.DisplayList;
using Andy.Tui.Input;

namespace Andy.Tui.Core.Tests;

public class EventToDlIntegrationTests
{
    [Fact]
    public async Task MouseHover_Sets_PseudoState_And_Changes_Dl()
    {
        var bus = new InvalidationBus();
        var states = new PseudoStateRegistry();
        var focus = new FocusManager();
        var rects = new Dictionary<int, Andy.Tui.Layout.Rect> { { 1, new Andy.Tui.Layout.Rect(0, 0, 5, 1) } };
        var ei = new EventIntegration(bus, states, focus, () => rects);
        var caps = new TerminalCapabilities { TrueColor = true, Palette256 = true };
        var sched = new FrameScheduler(new SimpleManualClock());
        var pty = new TestPty();
        // Build DL based on pseudo-state
        DisplayList.DisplayList BuildDl()
        {
            var b = new DisplayListBuilder();
            b.PushClip(new ClipPush(0, 0, 5, 1));
            var fg = states.Get(1).HasFlag(PseudoState.Hover) ? new Rgb24(255, 0, 0) : new Rgb24(200, 200, 200);
            b.DrawText(new TextRun(0, 0, "A", fg, null, CellAttrFlags.None));
            b.Pop();
            return b.Build();
        }
        // Initial render
        await sched.RenderOnceAsync(BuildDl(), (5, 1), caps, pty, CancellationToken.None);
        var before = pty.Last;
        // Inject mouse over cell (0,0)
        var handled = ei.Handle(new MouseEvent(MouseKind.Move, 0, 0, MouseButton.None, KeyModifiers.None));
        Assert.True(handled);
        // Re-render
        await sched.RenderOnceAsync(BuildDl(), (5, 1), caps, pty, CancellationToken.None);
        var after = pty.Last;
        Assert.NotEqual(before, after);

        // Press activates
        var handledDown = ei.Handle(new MouseEvent(MouseKind.Down, 0, 0, MouseButton.Left, KeyModifiers.None));
        Assert.True(handledDown);
    }

    [Fact]
    public void ResizeEvent_Requests_Recompose()
    {
        var bus = new InvalidationBus();
        var states = new PseudoStateRegistry();
        var focus = new FocusManager();
        var rects = new Dictionary<int, Andy.Tui.Layout.Rect>();
        var ei = new EventIntegration(bus, states, focus, () => rects);
        bool requested = false; bus.RecomposeRequested += () => requested = true;
        var handled = ei.Handle(new ResizeEvent(80, 24));
        Assert.True(handled);
        Assert.True(requested);
    }
}

file sealed class TestPty : IPtyIo
{
    public string Last = string.Empty;
    public Task WriteAsync(ReadOnlyMemory<byte> frameBytes, CancellationToken cancellationToken)
    {
        Last = System.Text.Encoding.UTF8.GetString(frameBytes.Span);
        return Task.CompletedTask;
    }
}
