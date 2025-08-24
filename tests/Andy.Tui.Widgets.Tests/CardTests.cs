using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class CardTests
{
    [Fact]
    public void Renders_Title_Body_And_Footer()
    {
        var c = new Andy.Tui.Widgets.Card();
        c.SetTitle("T"); c.SetBody("B"); c.SetFooter("F");
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        c.Render(new L.Rect(0,0,20,6), baseDl, b);
        var dl = b.Build();
        var text = string.Join("", dl.Ops.OfType<DL.TextRun>().Select(t => t.Content));
        Assert.Contains("T", text);
        Assert.Contains("B", text);
        Assert.Contains("F", text);
    }
}
