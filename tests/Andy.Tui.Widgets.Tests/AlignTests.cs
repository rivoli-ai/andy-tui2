using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class AlignTests
{
    [Theory]
    [InlineData(0,0,20,10, 10,5, Andy.Tui.Widgets.HorizontalAlign.Left, Andy.Tui.Widgets.VerticalAlign.Top, 0,0)]
    [InlineData(0,0,20,10, 10,5, Andy.Tui.Widgets.HorizontalAlign.Center, Andy.Tui.Widgets.VerticalAlign.Middle, 5,2)]
    [InlineData(0,0,20,10, 10,5, Andy.Tui.Widgets.HorizontalAlign.Right, Andy.Tui.Widgets.VerticalAlign.Bottom, 10,5)]
    public void ComputeChildRect_PositionsCorrectly(int x,int y,int w,int h,int cw,int ch, Andy.Tui.Widgets.HorizontalAlign ha, Andy.Tui.Widgets.VerticalAlign va, int expX,int expY)
    {
        var a = new Andy.Tui.Widgets.Align();
        a.SetChildFixedSize(cw, ch);
        var got = a.ComputeChildRect(x,y,w,h);
        a.SetAlignment(ha, va);
        got = a.ComputeChildRect(x,y,w,h);
        Assert.Equal(expX, got.x);
        Assert.Equal(expY, got.y);
    }

    [Fact]
    public void Render_Child_In_Stretched_Area()
    {
        var a = new Andy.Tui.Widgets.Align();
        a.SetAlignment(Andy.Tui.Widgets.HorizontalAlign.Stretch, Andy.Tui.Widgets.VerticalAlign.Stretch);
        a.SetChild((r, bd, b) => b.DrawText(new DL.TextRun((int)r.X, (int)r.Y, "X", new DL.Rgb24(255,255,255), null, DL.CellAttrFlags.None)));
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        a.Render(new L.Rect(0,0,10,3), baseDl, b);
        var dl = b.Build();
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), t => t.X == 0 && t.Y == 0 && t.Content == "X");
    }
}
