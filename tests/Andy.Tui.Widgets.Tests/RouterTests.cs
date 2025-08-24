using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class RouterTests
{
    [Fact]
    public void Navigate_Back_Forward_Tracks_History()
    {
        var r = new Andy.Tui.Widgets.Router();
        r.SetRoute("a", (rect,bd,b) => { });
        r.SetRoute("b", (rect,bd,b) => { });
        r.NavigateTo("a");
        r.NavigateTo("b");
        Assert.Equal("b", r.GetCurrent());
        Assert.True(r.CanBack());
        r.Back();
        Assert.Equal("a", r.GetCurrent());
        Assert.True(r.CanForward());
        r.Forward();
        Assert.Equal("b", r.GetCurrent());
    }
}