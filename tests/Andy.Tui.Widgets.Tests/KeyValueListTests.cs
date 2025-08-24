using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class KeyValueListTests
{
    [Fact]
    public void Renders_Keys_And_Values()
    {
        var kv = new Andy.Tui.Widgets.KeyValueList();
        kv.SetItems(new[]{("A","1"),("B","2")});
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        kv.Render(new L.Rect(0,0,20,5), baseDl, b);
        var dl = b.Build();
        var text = string.Join("", dl.Ops.OfType<DL.TextRun>().Select(t => t.Content));
        Assert.Contains("A", text);
        Assert.Contains(":", text);
        Assert.Contains("1", text);
    }
}
