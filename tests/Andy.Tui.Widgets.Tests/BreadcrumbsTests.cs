using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class BreadcrumbsTests
{
    [Fact]
    public void Renders_All_Parts_With_Separators()
    {
        var bc = new Andy.Tui.Widgets.Breadcrumbs();
        bc.SetItems(new[]{"A","B","C"});
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        bc.Render(new L.Rect(0,0,20,1), baseDl, b);
        var dl = b.Build();
        var text = string.Join("", dl.Ops.OfType<DL.TextRun>().Select(t => t.Content));
        Assert.Contains("A", text);
        Assert.Contains("B", text);
        Assert.Contains("C", text);
        Assert.Contains("›", text);
    }
}
