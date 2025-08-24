using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class DockTests
{
    [Fact]
    public void Renders_Top_Left_Bottom_And_Center()
    {
        var d = new Andy.Tui.Widgets.DockLayout();
        bool top=false,left=false,bottom=false,center=false;
        d.SetRegions(
            (Andy.Tui.Widgets.DockRegion.Top, 1, (r,bd,b) => { top = true; b.DrawText(new DL.TextRun((int)r.X, (int)r.Y, "T", new DL.Rgb24(255,255,255), null, DL.CellAttrFlags.None)); }),
            (Andy.Tui.Widgets.DockRegion.Left, 2, (r,bd,b) => { left = true; b.DrawText(new DL.TextRun((int)r.X, (int)r.Y, "L", new DL.Rgb24(255,255,255), null, DL.CellAttrFlags.None)); }),
            (Andy.Tui.Widgets.DockRegion.Bottom, 1, (r,bd,b) => { bottom = true; b.DrawText(new DL.TextRun((int)r.X, (int)r.Y, "B", new DL.Rgb24(255,255,255), null, DL.CellAttrFlags.None)); })
        );
        d.SetCenter((r,bd,b) => { center = true; b.DrawText(new DL.TextRun((int)r.X, (int)r.Y, "C", new DL.Rgb24(255,255,255), null, DL.CellAttrFlags.None)); });
        var baseDl = new DL.DisplayListBuilder().Build();
        var bldr = new DL.DisplayListBuilder();
        d.Render(new L.Rect(0,0,10,5), baseDl, bldr);
        var dl = bldr.Build();
        Assert.True(top && left && bottom && center);
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), t => t.Content == "T");
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), t => t.Content == "L");
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), t => t.Content == "B");
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), t => t.Content == "C");
    }
}
