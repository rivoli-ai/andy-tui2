using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class TitleBadgeBellResizeFigletTests
{
    [Fact]
    public void TitleBadge_Renders_Title_And_Badge()
    {
        var tb = new Andy.Tui.Widgets.TitleBadge();
        tb.SetTitle("Inbox"); tb.SetBadge("7");
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        tb.Render(new L.Rect(0,0,20,1), baseDl, b);
        var dl = b.Build();
        var runs = dl.Ops.OfType<DL.TextRun>().ToArray();
        Assert.Contains(runs, r => r.Content.Contains("Inbox"));
        Assert.Contains(runs, r => r.Content.Contains("7"));
    }

    [Fact]
    public void Bell_Shows_Then_Expires()
    {
        var bell = new Andy.Tui.Widgets.Bell();
        bell.Show("Hi", ttlFrames: 2);
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        bell.RenderAt(0,0, baseDl, b);
        Assert.True(bell.IsVisible);
        bell.Tick(); bell.Tick();
        Assert.False(bell.IsVisible);
    }

    [Fact]
    public void ResizeHandle_Renders_Glyphs()
    {
        var rh = new Andy.Tui.Widgets.ResizeHandle();
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        rh.SetOrientation(false);
        rh.Render(new L.Rect(0,0,3,5), baseDl, b);
        var dl = b.Build();
        Assert.NotEmpty(dl.Ops.OfType<DL.TextRun>());
    }

    [Fact]
    public void FigletViewer_Renders_Lines()
    {
        var fv = new Andy.Tui.Widgets.FigletViewer();
        fv.SetText("A");
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        fv.Render(new L.Rect(0,0,40,6), baseDl, b);
        var dl = b.Build();
        Assert.True(dl.Ops.OfType<DL.TextRun>().Count() >= 3);
    }
}
