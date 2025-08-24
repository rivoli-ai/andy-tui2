using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class ListViewTests
{
    [Fact]
    public void Selection_Toggle_And_Range()
    {
        var lv = new Andy.Tui.Widgets.ListView();
        lv.SetItems(new[]{"A","B","C","D","E"});
        lv.MoveCursor(1); // cursor at 1
        lv.ToggleSelect();
        lv.SelectRange(1,3);
        var sel = lv.GetSelectedIndices();
        Assert.Contains(1, sel);
        Assert.Contains(2, sel);
        Assert.Contains(3, sel);
    }

    [Fact]
    public void Render_Draws_Border_And_Items()
    {
        var lv = new Andy.Tui.Widgets.ListView();
        lv.SetItems(new[]{"A","B"});
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        lv.Render(new L.Rect(0,0,10,5), baseDl, b);
        var dl = b.Build();
        Assert.Contains(dl.Ops.OfType<DL.Border>(), _ => true);
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), t => t.Content == "A");
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), t => t.Content == "B");
    }
}
