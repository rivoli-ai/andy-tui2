using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class LineChartTests
{
    [Fact]
    public void Renders_Line_Points()
    {
        var lc = new Andy.Tui.Widgets.LineChart();
        lc.SetValues(new double[]{1,2,3,2,1});
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        lc.Render(new L.Rect(0,0,5,5), baseDl, b);
        var dl = b.Build();
        Assert.True(dl.Ops.OfType<DL.Rect>().Any());
    }
}
