using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class ScatterHistogramBoxTests
{
    [Fact]
    public void Scatter_Renders_Points()
    {
        var s = new Andy.Tui.Widgets.ScatterPlot();
        s.SetPoints(new[]{(0.0,0.0),(1.0,1.0)});
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        s.Render(new L.Rect(0,0,10,10), baseDl, b);
        var dl = b.Build();
        Assert.True(dl.Ops.OfType<DL.Rect>().Any());
    }

    [Fact]
    public void Histogram_Renders_Bins()
    {
        var h = new Andy.Tui.Widgets.Histogram();
        h.SetBins(5);
        h.SetValues(new[]{0.0,0.1,0.2,0.8,0.9});
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        h.Render(new L.Rect(0,0,10,5), baseDl, b);
        var dl = b.Build();
        Assert.True(dl.Ops.OfType<DL.Rect>().Any());
    }

    [Fact]
    public void BoxPlot_Renders_Box_And_Median()
    {
        var bp = new Andy.Tui.Widgets.BoxPlot();
        bp.SetSeries(new[]{1.0,2.0,3.0,4.0,5.0});
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        bp.Render(new L.Rect(0,0,20,10), baseDl, b);
        var dl = b.Build();
        Assert.True(dl.Ops.OfType<DL.Rect>().Any());
    }
}
