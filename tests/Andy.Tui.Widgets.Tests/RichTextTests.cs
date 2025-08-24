using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class RichTextTests
{
    [Fact]
    public void Parses_Bold_Underline_And_Color()
    {
        var rt = new Andy.Tui.Widgets.RichText();
        rt.SetText("[b][u][color=#00ff00]Hi[/color][/u][/b]");
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        rt.Render(new L.Rect(0,0,10,1), baseDl, b);
        var dl = b.Build();
        var tr = dl.Ops.OfType<DL.TextRun>().FirstOrDefault();
        Assert.NotNull(tr);
        Assert.Equal("Hi", tr!.Content);
        Assert.True((tr.Attrs & DL.CellAttrFlags.Bold) != 0);
        Assert.True((tr.Attrs & DL.CellAttrFlags.Underline) != 0);
    }
}
