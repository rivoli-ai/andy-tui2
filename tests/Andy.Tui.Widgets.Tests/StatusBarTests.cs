using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class StatusBarTests
{
    [Fact]
    public void Renders_Left_Center_Right()
    {
        var bar = new Andy.Tui.Widgets.StatusBar();
        bar.SetText("L", "C", "R");
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        bar.Render(new L.Rect(0,0,9,1), baseDl, b);
        var dl = b.Build();
        var runs = dl.Ops.OfType<DL.TextRun>().ToList();
        Assert.Contains(runs, t => t.Content == "L" && t.X == 0);
        Assert.Contains(runs, t => t.Content == "R");
        Assert.Contains(runs, t => t.Content == "C");
    }
}
