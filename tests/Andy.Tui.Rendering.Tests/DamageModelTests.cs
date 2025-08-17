using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Rendering.Tests;

public class DamageModelTests
{
    [Fact]
    public void Two_NonContiguous_Changes_Yield_Two_Rects()
    {
        var comp = new TtyCompositor();
        var prev = new CellGrid(6,1);
        var next = new CellGrid(6,1);
        next[1,0] = new Cell("A",1,new Rgb24(1,1,1), new Rgb24(0,0,0), CellAttrFlags.None);
        next[4,0] = new Cell("B",1,new Rgb24(1,1,1), new Rgb24(0,0,0), CellAttrFlags.None);
        var dirty = comp.Damage(prev, next);
        Assert.Equal(2, dirty.Count);
        Assert.Equal(new DirtyRect(1,0,1,1), dirty[0]);
        Assert.Equal(new DirtyRect(4,0,1,1), dirty[1]);
    }

    [Fact]
    public void No_Dirty_When_Equal()
    {
        var comp = new TtyCompositor();
        var prev = new CellGrid(3,1);
        var next = new CellGrid(3,1);
        var dirty = comp.Damage(prev, next);
        Assert.Empty(dirty);
    }
}
