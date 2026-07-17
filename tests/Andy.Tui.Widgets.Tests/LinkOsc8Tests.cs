using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class LinkOsc8Tests
{
    [Fact]
    public void Attaches_Hyperlink_Metadata_When_Enabled()
    {
        var link = new Andy.Tui.Widgets.Link();
        link.SetText("Homepage");
        link.SetUrl("https://example.com");
        link.EnableOsc8(true);
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        link.Render(new L.Rect(0, 0, 40, 1), baseDl, b);
        var dl = b.Build();
        var tr = dl.Ops.OfType<DL.TextRun>().First();

        // The URL travels as structured metadata, NOT embedded in the visible content.
        Assert.Equal("https://example.com", tr.Hyperlink);
        Assert.Equal("Homepage", tr.Content);
        Assert.False(tr.Content.Contains('')); // no ESC control byte in the cell stream
        Assert.DoesNotContain("]8;;", tr.Content);   // no OSC 8 introducer in the content
    }

    [Fact]
    public void No_Hyperlink_Metadata_When_Disabled()
    {
        var link = new Andy.Tui.Widgets.Link();
        link.SetText("Homepage");
        link.SetUrl("https://example.com");
        link.EnableOsc8(false);
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        link.Render(new L.Rect(0, 0, 40, 1), baseDl, b);
        var dl = b.Build();
        var tr = dl.Ops.OfType<DL.TextRun>().First();

        Assert.Null(tr.Hyperlink);
        Assert.DoesNotContain("]8;;", tr.Content);
    }

    [Fact]
    public void Hyperlink_Controls_Do_Not_Consume_Layout_Cells()
    {
        var link = new Andy.Tui.Widgets.Link();
        link.SetText("Hi");
        link.SetUrl("https://example.com/a/very/long/url/that/exceeds/width");
        link.EnableOsc8(true);
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        link.Render(new L.Rect(0, 0, 40, 1), baseDl, b);
        var dl = b.Build();
        var tr = dl.Ops.OfType<DL.TextRun>().First();

        // The visible content is exactly the label; the long URL never inflates it.
        Assert.Equal("Hi", tr.Content);
        Assert.Equal(2, tr.Content.Length);
    }
}
