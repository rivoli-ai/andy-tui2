using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class LinkOsc8Tests
{
    [Fact]
    public void Emits_OSC8_Sequences_When_Enabled()
    {
        var link = new Andy.Tui.Widgets.Link();
        link.SetText("Homepage");
        link.SetUrl("https://example.com");
        link.EnableOsc8(true);
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        link.Render(new L.Rect(0,0,40,1), baseDl, b);
        var dl = b.Build();
        var tr = dl.Ops.OfType<DL.TextRun>().FirstOrDefault();
        Assert.NotNull(tr.Content);
        Assert.Contains("\u001b]8;;https://example.com\u001b\\", tr.Content); // start
        Assert.EndsWith("\u001b]8;;\u001b\\", tr.Content); // end
    }

    [Fact]
    public void No_OSC8_When_Disabled()
    {
        var link = new Andy.Tui.Widgets.Link();
        link.SetText("Homepage");
        link.SetUrl("https://example.com");
        link.EnableOsc8(false);
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        link.Render(new L.Rect(0,0,40,1), baseDl, b);
        var dl = b.Build();
        var tr = dl.Ops.OfType<DL.TextRun>().FirstOrDefault();
        Assert.NotNull(tr.Content);
        Assert.DoesNotContain("]8;;", tr.Content);
    }
}
