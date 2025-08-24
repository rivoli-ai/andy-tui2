using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class SparklineBarLinkLayersTests
{
    [Fact]
    public void Sparkline_Renders_Ramp()
    {
        var sp = new Andy.Tui.Widgets.Sparkline();
        sp.SetValues(new double[] { 0, 1, 2, 3, 4, 5, 6, 7 });
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        sp.Render(new L.Rect(0, 0, 8, 1), baseDl, b);
        var dl = b.Build();
        Assert.True(dl.Ops.OfType<DL.TextRun>().Count() >= 8);
    }

    [Fact]
    public void BarChart_Measure_And_Render()
    {
        var chart = new Andy.Tui.Widgets.BarChart();
        chart.SetItems(new (string, double)[] { ("A", 0.1), ("B", 0.5), ("C", 0.9) });
        var (w, h) = chart.Measure();
        Assert.True(w >= 11);
        Assert.Equal(3, h);
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        chart.Render(new L.Rect(0, 0, 20, 3), baseDl, b);
        var dl = b.Build();
        Assert.True(dl.Ops.OfType<DL.TextRun>().Any());
    }

    [Fact]
    public void Link_Underlines_Text()
    {
        var link = new Andy.Tui.Widgets.Link();
        link.SetText("Hello");
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        link.Render(new L.Rect(0, 0, 10, 1), baseDl, b);
        var dl = b.Build();
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), t => t.Content.Contains("Hello") && (t.Attrs & DL.CellAttrFlags.Underline) != 0);
    }

    [Fact]
    public void Layers_Draws_Multiple_Layers()
    {
        var layers = new Andy.Tui.Widgets.StackLayers();
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        layers.AddLayer((bd, lb) => lb.DrawText(new DL.TextRun(0, 0, "Layer1", new DL.Rgb24(255, 255, 255), null, DL.CellAttrFlags.None)));
        layers.AddLayer((bd, lb) => lb.DrawText(new DL.TextRun(0, 1, "Layer2", new DL.Rgb24(255, 255, 255), null, DL.CellAttrFlags.None)));
        layers.Render(new L.Rect(0, 0, 20, 3), baseDl, b);
        var dl = b.Build();
        var texts = dl.Ops.OfType<DL.TextRun>().Select(t => t.Content).ToArray();
        Assert.Contains("Layer1", texts);
        Assert.Contains("Layer2", texts);
    }
}
