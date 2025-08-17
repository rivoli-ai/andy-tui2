using Andy.Tui.Backend.Terminal;
using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Rendering.Tests;

public class CuratedParityFixturesTests
{
    private static (CellGrid grid, ReadOnlyMemory<byte> bytes) Render(DisplayList.DisplayList dl, (int W,int H) size)
    {
        var comp = new TtyCompositor();
        var cells = comp.Composite(dl, (size.W,size.H));
        var dirty = comp.Damage(new CellGrid(size.W,size.H), cells);
        var runs = comp.RowRuns(cells, dirty);
        var caps = new TerminalCapabilities{ TrueColor = true, Palette256 = true };
        var bytes = new AnsiEncoder().Encode(runs, caps);
        return (cells, bytes);
    }

    [Fact]
    public void Fixture_Text_Over_Background_Color_Parity()
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0,0,20,3));
        b.DrawRect(new Rect(0,0,20,3,new Rgb24(3,4,5)));
        b.DrawText(new TextRun(2,1,"Hello", new Rgb24(200,100,50), null, CellAttrFlags.Bold | CellAttrFlags.Underline));
        b.Pop();
        var (grid, bytes) = Render(b.Build(), (20,3));
        var oracle = VirtualScreenOracle.Decode(bytes.Span, (20,3));
        for (int y = 0; y < 3; y++)
        for (int x = 0; x < 20; x++)
        {
            Assert.Equal(grid[x,y].Grapheme ?? "", oracle[x,y].Grapheme ?? "");
            Assert.Equal(grid[x,y].Fg, oracle[x,y].Fg);
            Assert.Equal(grid[x,y].Bg, oracle[x,y].Bg);
        }
    }

    [Fact]
    public void Fixture_Multicolor_Runs_Same_Row_Parity()
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0,0,20,1));
        b.DrawText(new TextRun(0,0,"Hi", new Rgb24(255,0,0), new Rgb24(0,0,0), CellAttrFlags.None));
        b.DrawText(new TextRun(3,0,"There", new Rgb24(0,255,0), new Rgb24(0,0,0), CellAttrFlags.None));
        b.Pop();
        var (grid, bytes) = Render(b.Build(), (20,1));
        var oracle = VirtualScreenOracle.Decode(bytes.Span, (20,1));
        for (int x = 0; x < 20; x++)
        {
            Assert.Equal(grid[x,0].Grapheme ?? "", oracle[x,0].Grapheme ?? "");
            Assert.Equal(grid[x,0].Fg, oracle[x,0].Fg);
            Assert.Equal(grid[x,0].Bg, oracle[x,0].Bg);
        }
    }
}
