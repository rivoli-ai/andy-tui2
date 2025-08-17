using Andy.Tui.Compositor;

namespace Andy.Tui.Rendering.Tests;

public class ScrollDirtyBudgetTests
{
    [Fact]
    public void Scroll_Dirty_Coverage_Is_Under_12_Percent()
    {
        int width = 100;
        int height = 50;
        var prev = new CellGrid(width, height);
        var next = new CellGrid(width, height);
        // Fill prev rows 0..48 with letters, next rows 1..49 with same letters (simulate scroll down by 1)
        for (int y = 0; y < height - 1; y++)
        {
            for (int x = 0; x < width; x++)
            {
                prev[x,y] = new Cell("A",1,new Andy.Tui.DisplayList.Rgb24(200,200,200), new Andy.Tui.DisplayList.Rgb24(0,0,0), Andy.Tui.DisplayList.CellAttrFlags.None);
                next[x,y+1] = new Cell("A",1,new Andy.Tui.DisplayList.Rgb24(200,200,200), new Andy.Tui.DisplayList.Rgb24(0,0,0), Andy.Tui.DisplayList.CellAttrFlags.None);
            }
        }
        var comp = new TtyCompositor();
        var dirty = comp.Damage(prev, next);
        int dirtyArea = dirty.Sum(r => r.Width * r.Height);
        double percent = (double)dirtyArea / (width * height);
        Assert.True(percent <= 0.12, $"Dirty percent too high: {percent:P2}");
    }
}
