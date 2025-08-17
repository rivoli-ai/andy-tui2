using Andy.Tui.Backend.Terminal;
using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Rendering.Tests;

public class E2eCursorMultiRunTests
{
    [Fact]
    public void Multiple_Runs_On_Same_Row_Produce_Multiple_Cursor_Positions()
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0,0,20,1));
        b.DrawText(new TextRun(0,0,"hi", new Rgb24(255,255,255), null, CellAttrFlags.None));
        b.DrawText(new TextRun(5,0,"there", new Rgb24(255,255,255), null, CellAttrFlags.None));
        b.Pop();
        var dl = b.Build();
        var comp = new TtyCompositor();
        var g = comp.Composite(dl, (20,1));
        var dirty = comp.Damage(new CellGrid(20,1), g);
        var runs = comp.RowRuns(g, dirty);
        var s = System.Text.Encoding.UTF8.GetString(new AnsiEncoder().Encode(runs, new TerminalCapabilities{ TrueColor=true, Palette256=true }).Span);
        Assert.Contains("\x1b[1;1H", s);
        Assert.Contains("\x1b[1;6H", s);
    }
}
