using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class PagerToastSpinnerTests
{
    [Fact]
    public void Pager_Measure_And_Render()
    {
        var p = new Andy.Tui.Widgets.Pager();
        p.SetTotalItems(250);
        p.SetPageSize(25);
        p.SetCurrentPage(3);
        var (mw, mh) = p.Measure();
        Assert.True(mw >= 12);
        Assert.Equal(1, mh);
        var b = new DL.DisplayListBuilder();
        p.Render(new L.Rect(0, 0, mw, 1), new DL.DisplayListBuilder().Build(), b);
        var dl = b.Build();
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), t => t.Content.Contains("3/10"));
    }

    [Fact]
    public void Toast_Shows_For_Duration()
    {
        var toast = new Andy.Tui.Widgets.Toast();
        toast.Show("Saved!", TimeSpan.FromMilliseconds(200));
        Assert.True(toast.IsVisible());
        var (w, h) = toast.Measure();
        Assert.True(w >= 8);
        Assert.Equal(1, h);
        var b = new DL.DisplayListBuilder();
        toast.Render(new L.Rect(0, 0, w, 1), new DL.DisplayListBuilder().Build(), b);
        var dl = b.Build();
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), t => t.Content.Contains("Saved!"));
    }

    [Fact]
    public void Spinner_Ticks_And_Renders()
    {
        var sp = new Andy.Tui.Widgets.Spinner();
        var (w, h) = sp.Measure();
        Assert.Equal((1, 1), (w, h));
        var b1 = new DL.DisplayListBuilder();
        sp.Render(new L.Rect(0, 0, 1, 1), new DL.DisplayListBuilder().Build(), b1);
        var dl1 = b1.Build();
        Assert.Contains(dl1.Ops.OfType<DL.TextRun>(), _ => true);
        sp.Tick();
        var b2 = new DL.DisplayListBuilder();
        sp.Render(new L.Rect(0, 0, 1, 1), new DL.DisplayListBuilder().Build(), b2);
        var dl2 = b2.Build();
        Assert.Contains(dl2.Ops.OfType<DL.TextRun>(), _ => true);
    }
}
