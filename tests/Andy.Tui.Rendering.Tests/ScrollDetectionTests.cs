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

        // When scroll is allowed, the plan carries a +1 scroll and repaints only
        // the newly exposed top row.
        var plan = comp.ComputeDamagePlan(prev, next, allowScroll: true);
        Assert.Equal(1, plan.ScrollDy);
        Assert.Single(plan.Dirty);
        Assert.Equal(new DirtyRect(0, 0, 4, 1), plan.Dirty[0]);
    }

    [Fact]
    public void Damage_Without_Scroll_Repaints_Every_Changed_Row()
    {
        var comp = new TtyCompositor();
        var prev = new CellGrid(4, 4);
        var next = new CellGrid(4, 4);
        for (int x = 0; x < 4; x++)
        {
            prev[x, 1] = new Cell("A", 1, new Rgb24(1, 1, 1), new Rgb24(0, 0, 0), CellAttrFlags.None);
            prev[x, 2] = new Cell("B", 1, new Rgb24(1, 1, 1), new Rgb24(0, 0, 0), CellAttrFlags.None);
            next[x, 2] = new Cell("A", 1, new Rgb24(1, 1, 1), new Rgb24(0, 0, 0), CellAttrFlags.None);
            next[x, 3] = new Cell("B", 1, new Rgb24(1, 1, 1), new Rgb24(0, 0, 0), CellAttrFlags.None);
        }

        // The plain Damage API never reduces to a scroll, so it must repaint
        // every row that actually changed (rows 1, 2 and 3 here).
        var dirty = comp.Damage(prev, next);
        Assert.Equal(3, dirty.Count);
        Assert.DoesNotContain(dirty, r => r.Y == 0);
    }
}
