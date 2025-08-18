using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Rendering.Tests;

public class ScrollDetectionTests
{
    [Fact]
    public void Detects_Scroll_Down_And_Marks_Top_Rows()
    {
        var comp = new TtyCompositor();
        var prev = new CellGrid(4, 4);
        var next = new CellGrid(4, 4);
        // Fill prev rows 1..3 with letters, next rows 2..3 with same letters shifted down by 1
        for (int x = 0; x < 4; x++)
        {
            prev[x, 1] = new Cell("A", 1, new Rgb24(1, 1, 1), new Rgb24(0, 0, 0), CellAttrFlags.None);
            prev[x, 2] = new Cell("B", 1, new Rgb24(1, 1, 1), new Rgb24(0, 0, 0), CellAttrFlags.None);
            next[x, 2] = new Cell("A", 1, new Rgb24(1, 1, 1), new Rgb24(0, 0, 0), CellAttrFlags.None);
            next[x, 3] = new Cell("B", 1, new Rgb24(1, 1, 1), new Rgb24(0, 0, 0), CellAttrFlags.None);
        }
        var dirty = comp.Damage(prev, next);
        Assert.Single(dirty);
        Assert.Equal(new DirtyRect(0, 0, 4, 1), dirty[0]);
    }
}
