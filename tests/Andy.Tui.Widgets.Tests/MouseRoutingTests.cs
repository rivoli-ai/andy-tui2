using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class MouseRoutingTests
{
    [Fact]
    public void ListBox_OnMouseDown_Selects_Item_By_Y()
    {
        var lb = new Andy.Tui.Widgets.ListBox();
        lb.SetItems(new[] { "A", "B", "C" });
        var rect = new L.Rect(0, 0, 10, 3);
        // Click on second row (y=1)
        lb.OnMouseDown(1, 1, rect);
        // Selected should be index 1
        // There's no getter; we re-render and inspect bold attr on second row
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        lb.Render(rect, baseDl, b);
        var dl = b.Build();
        var runs = dl.Ops.OfType<DL.TextRun>().ToList();
        Assert.True((runs[1].Attrs & DL.CellAttrFlags.Bold) == DL.CellAttrFlags.Bold);
    }

    [Fact]
    public void ScrollView_OnMouseWheel_Adjusts_Scroll()
    {
        var sv = new Andy.Tui.Widgets.ScrollView();
        sv.SetContent(string.Join('\n', Enumerable.Range(1, 100).Select(i => $"Line {i}")));
        var rect = new L.Rect(0, 0, 20, 5);
        // Scroll down one notch (wheelDelta = -1)
        sv.OnMouseWheel(-1, rect);
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        sv.Render(rect, baseDl, b);
        var dl = b.Build();
        var first = dl.Ops.OfType<DL.TextRun>().FirstOrDefault();
        Assert.Contains("Line 2", first.Content);
    }
}
