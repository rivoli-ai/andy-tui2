using Andy.Tui.DisplayList;
using Andy.Tui.Compositor;

namespace Andy.Tui.Rendering.Tests;

public class CompositorTests
{
    [Fact]
    public void Composite_Rect_Then_Text_Over_Background()
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0,0,10,3));
        b.DrawRect(new Rect(0,0,10,3,new Rgb24(10,10,10)));
        b.DrawText(new TextRun(1,1,"hi", new Rgb24(255,255,255), null, CellAttrFlags.None));
        b.Pop();
        var grid = new TtyCompositor().Composite(b.Build(), (10,3));

        Assert.Equal(" ", grid[0,0].Grapheme);
        Assert.Equal("h", grid[1,1].Grapheme);
        Assert.Equal(new Rgb24(255,255,255), grid[1,1].Fg);
        Assert.Equal(new Rgb24(10,10,10), grid[1,1].Bg);
    }

    [Fact]
    public void Damage_Finds_Changed_Cells()
    {
        var comp = new TtyCompositor();
        var g1 = new CellGrid(5,1);
        var g2 = new CellGrid(5,1);
        g2[0,0] = new Cell("A",1,new Rgb24(1,1,1), new Rgb24(0,0,0), CellAttrFlags.None);
        g2[1,0] = new Cell("B",1,new Rgb24(1,1,1), new Rgb24(0,0,0), CellAttrFlags.None);
        var dirty = comp.Damage(g1,g2);
        Assert.Single(dirty);
        Assert.Equal(new DirtyRect(0,0,2,1), dirty[0]);
    }

    [Fact]
    public void RowRuns_Groups_By_Attrs()
    {
        var comp = new TtyCompositor();
        var g = new CellGrid(5,1);
        g[0,0] = new Cell("A",1,new Rgb24(1,1,1), new Rgb24(0,0,0), CellAttrFlags.None);
        g[1,0] = new Cell("B",1,new Rgb24(1,1,1), new Rgb24(0,0,0), CellAttrFlags.None);
        g[2,0] = new Cell("C",1,new Rgb24(1,1,1), new Rgb24(10,10,10), CellAttrFlags.None);
        var runs = comp.RowRuns(g, new[]{ new DirtyRect(0,0,3,1) });
        Assert.Equal(2, runs.Count);
        Assert.Equal("AB", runs[0].Text);
        Assert.Equal("C", runs[1].Text);
        Assert.Equal(new Rgb24(1,1,1), runs[0].Fg);
        Assert.Equal(new Rgb24(0,0,0), runs[0].Bg);
    }
}
