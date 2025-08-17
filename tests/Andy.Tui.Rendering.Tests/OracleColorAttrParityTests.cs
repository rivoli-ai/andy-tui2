using Andy.Tui.Backend.Terminal;
using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Rendering.Tests;

public class OracleColorAttrParityTests
{
    [Fact]
    public void Colors_And_Attrs_Roundtrip_Via_Oracle()
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0,0,20,1));
        b.DrawText(new TextRun(0,0,"AB", new Rgb24(10,20,30), new Rgb24(1,2,3), CellAttrFlags.Bold | CellAttrFlags.Underline));
        b.Pop();
        var comp = new TtyCompositor();
        var grid = comp.Composite(b.Build(), (20,1));
        var dirty = comp.Damage(new CellGrid(20,1), grid);
        var runs = comp.RowRuns(grid, dirty);
        var bytes = new AnsiEncoder().Encode(runs, new TerminalCapabilities{ TrueColor=true, Palette256=true });
        var oracle = VirtualScreenOracle.Decode(bytes.Span, (20,1));
        Assert.Equal(grid[0,0].Fg, oracle[0,0].Fg);
        Assert.Equal(grid[0,0].Bg, oracle[0,0].Bg);
        Assert.Equal(grid[0,0].Attrs & (CellAttrFlags.Bold | CellAttrFlags.Underline | CellAttrFlags.Dim | CellAttrFlags.Blink | CellAttrFlags.Reverse | CellAttrFlags.Strikethrough),
                     oracle[0,0].Attrs & (CellAttrFlags.Bold | CellAttrFlags.Underline | CellAttrFlags.Dim | CellAttrFlags.Blink | CellAttrFlags.Reverse | CellAttrFlags.Strikethrough));
    }
}
