using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class TabsTests
{
    [Fact]
    public void Renders_Header_And_Active_Tab()
    {
        var t = new Andy.Tui.Widgets.Tabs();
        t.SetTabs(new[] { "One", "Two" });
        t.SetActive(1);
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        t.Render(new L.Rect(0, 0, 40, 5), baseDl, b);
        var dl = b.Build();
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), tr => tr.Content.Contains("Two"));
        Assert.Contains(dl.Ops.OfType<DL.Rect>(), r => r.Y == 1); // separator under tabs
    }
}
