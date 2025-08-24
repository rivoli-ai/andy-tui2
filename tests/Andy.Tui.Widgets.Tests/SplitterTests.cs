using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class SplitterTests
{
    [Fact]
    public void Renders_Handle_And_Panes()
    {
        var s = new Andy.Tui.Widgets.Splitter();
        s.SetOrientation(Andy.Tui.Widgets.SplitterOrientation.Vertical);
        s.SetFirstPane((r, bd, b) => b.DrawText(new DL.TextRun((int)r.X, (int)r.Y, "A", new DL.Rgb24(255, 255, 255), null, DL.CellAttrFlags.None)));
        s.SetSecondPane((r, bd, b) => b.DrawText(new DL.TextRun((int)r.X, (int)r.Y, "B", new DL.Rgb24(255, 255, 255), null, DL.CellAttrFlags.None)));
        var baseDl = new DL.DisplayListBuilder().Build();
        var bld = new DL.DisplayListBuilder();
        s.Render(new L.Rect(0, 0, 40, 10), baseDl, bld);
        var dl = bld.Build();
        Assert.Contains(dl.Ops.OfType<DL.Rect>(), r => r.Width == 1 || r.Height == 1); // handle rect present
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), t => t.Content == "A");
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), t => t.Content == "B");
    }

    [Fact]
    public void Draws_Continuous_Vertical_Line()
    {
        var s = new Andy.Tui.Widgets.Splitter();
        s.SetOrientation(Andy.Tui.Widgets.SplitterOrientation.Vertical);
        s.SetFirstPane((r, bd, b) => { });
        s.SetSecondPane((r, bd, b) => { });
        var baseDl = new DL.DisplayListBuilder().Build();
        var bld = new DL.DisplayListBuilder();
        int width = 20, height = 6;
        s.Render(new L.Rect(0, 0, width, height), baseDl, bld);
        var dl = bld.Build();
        // Count vertical line glyphs │
        int bars = dl.Ops.OfType<DL.TextRun>().Count(t => t.Content == "│");
        Assert.True(bars >= height); // at least one per row
    }

    [Fact]
    public void Draws_Continuous_Horizontal_Line()
    {
        var s = new Andy.Tui.Widgets.Splitter();
        s.SetOrientation(Andy.Tui.Widgets.SplitterOrientation.Horizontal);
        s.SetFirstPane((r, bd, b) => { });
        s.SetSecondPane((r, bd, b) => { });
        var baseDl = new DL.DisplayListBuilder().Build();
        var bld = new DL.DisplayListBuilder();
        int width = 20, height = 6;
        s.Render(new L.Rect(0, 0, width, height), baseDl, bld);
        var dl = bld.Build();
        // Count horizontal line glyphs ─
        int dashes = dl.Ops.OfType<DL.TextRun>().Count(t => t.Content == "─");
        Assert.True(dashes >= width); // at least one per column
    }
}
