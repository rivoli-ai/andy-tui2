using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class FocusRingTests
{
    [Fact]
    public void Next_Prev_Cycles()
    {
        var f = new Andy.Tui.Widgets.FocusRing();
        f.Add("a", new L.Rect(0,0,5,1));
        f.Add("b", new L.Rect(0,1,5,1));
        Assert.Equal("a", f.GetFocusedId());
        f.Next();
        Assert.Equal("b", f.GetFocusedId());
        f.Next();
        Assert.Equal("a", f.GetFocusedId());
        f.Prev();
        Assert.Equal("b", f.GetFocusedId());
    }

    [Fact]
    public void Render_Draws_Highlight()
    {
        var f = new Andy.Tui.Widgets.FocusRing();
        f.Add("a", new L.Rect(0,0,5,2));
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        f.Render(baseDl, b);
        var dl = b.Build();
        Assert.True(dl.Ops.OfType<DL.Rect>().Any());
    }
}
