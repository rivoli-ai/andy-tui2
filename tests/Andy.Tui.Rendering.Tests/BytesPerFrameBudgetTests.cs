using Andy.Tui.Backend.Terminal;
using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Rendering.Tests;

public class BytesPerFrameBudgetTests
{
    [Fact]
    public void Multirun_Frame_Bytes_Under_Budget()
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, 80, 5));
        b.DrawText(new TextRun(0, 0, "Hello", new Rgb24(255, 255, 255), null, CellAttrFlags.None));
        b.DrawText(new TextRun(10, 0, "World", new Rgb24(255, 255, 255), null, CellAttrFlags.None));
        b.DrawText(new TextRun(0, 2, "Colors", new Rgb24(10, 20, 30), new Rgb24(1, 2, 3), CellAttrFlags.Bold));
        b.Pop();
        var comp = new TtyCompositor();
        var grid = comp.Composite(b.Build(), (80, 5));
        var dirty = comp.Damage(new CellGrid(80, 5), grid);
        var runs = comp.RowRuns(grid, dirty);
        var bytes = new AnsiEncoder().Encode(runs, new TerminalCapabilities { TrueColor = true, Palette256 = true });
        // Budget: cursor moves + minimal SGR + text; keep under conservative 2KB
        Assert.True(bytes.Length < 2048, $"Frame bytes too high: {bytes.Length}");
    }
}
