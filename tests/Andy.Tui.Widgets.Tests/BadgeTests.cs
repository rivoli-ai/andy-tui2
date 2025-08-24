using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class BadgeTests
{
    [Fact]
    public void Renders_Text_And_Border()
    {
        var bd = new Andy.Tui.Widgets.Badge();
        bd.SetText("OK");
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        bd.RenderAt(0,0, baseDl, b);
        var dl = b.Build();
        Assert.Contains(dl.Ops.OfType<DL.Border>(), _ => true);
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), t => t.Content.Contains("OK"));
    }
}
