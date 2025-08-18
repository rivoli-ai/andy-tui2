using Andy.Tui.Backend.Terminal;
using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Rendering.Tests;

public class DirtyVsFullParityTests
{
    [Fact]
    public void Dirty_And_Full_Repaint_Produce_Identical_Bytes()
    {
        var dlb = new DisplayListBuilder();
        dlb.PushClip(new ClipPush(0, 0, 10, 2));
        dlb.DrawRect(new Rect(0, 0, 10, 2, new Rgb24(0, 0, 0)));
        dlb.DrawText(new TextRun(1, 0, "abc", new Rgb24(255, 255, 255), null, CellAttrFlags.None));
        dlb.DrawText(new TextRun(6, 1, "xyz", new Rgb24(255, 255, 255), null, CellAttrFlags.None));
        dlb.Pop();
        var dl = dlb.Build();

        var comp = new TtyCompositor();
        var empty = new CellGrid(10, 2);
        var next = comp.Composite(dl, (10, 2));
        var dirty = comp.Damage(empty, next);
        var runsDirty = comp.RowRuns(next, dirty);
        var bytesDirty = new AnsiEncoder().Encode(runsDirty, new TerminalCapabilities { TrueColor = true, Palette256 = true });

        // Full repaint: dirty == full when previous is empty; for now equivalent
        var runsFull = comp.RowRuns(next, new[] { new DirtyRect(0, 0, 10, 1), new DirtyRect(0, 1, 10, 1) });
        var bytesFull = new AnsiEncoder().Encode(runsFull, new TerminalCapabilities { TrueColor = true, Palette256 = true });

        Assert.Equal(bytesFull.ToArray(), bytesDirty.ToArray());
    }
}
