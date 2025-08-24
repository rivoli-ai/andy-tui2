using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class MenuBarTests
{
    [Fact]
    public void Renders_Titles_With_First_Letter_Underlined_And_Bold()
    {
        var mb = new Andy.Tui.Widgets.MenuBar()
            .Add("File", new Andy.Tui.Widgets.Menu())
            .Add("Edit", new Andy.Tui.Widgets.Menu())
            .Add("View", new Andy.Tui.Widgets.Menu());
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        mb.Render(new L.Rect(0, 0, 80, 1), baseDl, b);
        var dl = b.Build();
        var runs = dl.Ops.OfType<DL.TextRun>().Where(r => r.Y == 0).ToList();
        // Each title should have its first character underlined and bold, and the rest bold
        Assert.Contains(runs, r => r.Content == "F" && (r.Attrs & (DL.CellAttrFlags.Bold | DL.CellAttrFlags.Underline)) == (DL.CellAttrFlags.Bold | DL.CellAttrFlags.Underline));
        Assert.Contains(runs, r => r.Content == "ile" && (r.Attrs & DL.CellAttrFlags.Bold) == DL.CellAttrFlags.Bold);
        Assert.Contains(runs, r => r.Content == "E" && (r.Attrs & (DL.CellAttrFlags.Bold | DL.CellAttrFlags.Underline)) == (DL.CellAttrFlags.Bold | DL.CellAttrFlags.Underline));
        Assert.Contains(runs, r => r.Content == "dit" && (r.Attrs & DL.CellAttrFlags.Bold) == DL.CellAttrFlags.Bold);
        Assert.Contains(runs, r => r.Content == "V" && (r.Attrs & (DL.CellAttrFlags.Bold | DL.CellAttrFlags.Underline)) == (DL.CellAttrFlags.Bold | DL.CellAttrFlags.Underline));
        Assert.Contains(runs, r => r.Content == "iew" && (r.Attrs & DL.CellAttrFlags.Bold) == DL.CellAttrFlags.Bold);
    }
}
