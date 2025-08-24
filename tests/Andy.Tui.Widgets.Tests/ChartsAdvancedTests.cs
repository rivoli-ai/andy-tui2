using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class ChartsAdvancedTests
{
    [Fact]
    public void Heatmap_Renders_Cells()
    {
        var hm = new Andy.Tui.Widgets.Heatmap();
        hm.SetGrid(4);
        hm.SetValues(new[]{0.0,0.5,1.0,0.25,0.75,0.1});
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        hm.Render(new L.Rect(0,0,8,4), baseDl, b);
        var dl = b.Build();
        Assert.True(dl.Ops.OfType<DL.Rect>().Any());
    }

    [Fact]
    public void Bullet_Renders_Bar_And_Target()
    {
        var bl = new Andy.Tui.Widgets.BulletChart();
        bl.SetRange(0, 100); bl.SetValue(40); bl.SetTarget(80);
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        bl.Render(new L.Rect(0,0,20,3), baseDl, b);
        var dl = b.Build();
        Assert.True(dl.Ops.OfType<DL.Rect>().Any());
    }

    [Fact]
    public void Gauge_Renders_Track_And_Fill()
    {
        var g = new Andy.Tui.Widgets.Gauge();
        g.SetRange(0, 100); g.SetValue(50);
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        g.Render(new L.Rect(0,0,20,5), baseDl, b);
        var dl = b.Build();
        Assert.True(dl.Ops.OfType<DL.Rect>().Any());
    }

    [Fact]
    public void Candlestick_Renders_Wicks_And_Bodies()
    {
        var cs = new Andy.Tui.Widgets.Candlestick();
        cs.SetSeries(new[]{ new Andy.Tui.Widgets.Candlestick.Candle(1, 5, 0.5, 4), new Andy.Tui.Widgets.Candlestick.Candle(4, 6, 3, 3.5) });
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        cs.Render(new L.Rect(0,0,10,10), baseDl, b);
        var dl = b.Build();
        Assert.True(dl.Ops.OfType<DL.Rect>().Any());
    }
}
