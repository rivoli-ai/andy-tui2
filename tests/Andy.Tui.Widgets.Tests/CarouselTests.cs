using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class CarouselTests
{
    [Fact]
    public void Next_Prev_Wraps_Around()
    {
        var c = new Andy.Tui.Widgets.Carousel();
        c.SetItems(new[]{"A","B","C"});
        Assert.Equal(0, c.GetIndex());
        c.Next(); c.Next(); c.Next();
        Assert.Equal(0, c.GetIndex());
        c.Prev();
        Assert.Equal(2, c.GetIndex());
    }

    [Fact]
    public void Render_Draws_Dots()
    {
        var c = new Andy.Tui.Widgets.Carousel();
        c.SetItems(new[]{"A","B","C"});
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        c.Render(new L.Rect(0,0,20,5), baseDl, b);
        var dl = b.Build();
        var text = string.Join("", dl.Ops.OfType<DL.TextRun>().Select(t => t.Content));
        Assert.Contains("•", text);
    }
}
