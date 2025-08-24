using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class MapViewTests
{
    [Fact]
    public void Renders_Tiles()
    {
        var m = new Andy.Tui.Widgets.MapView();
        m.SetGrid(4,2);
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        m.Render(new L.Rect(0,0,20,10), baseDl, b);
        var dl = b.Build();
        Assert.True(dl.Ops.OfType<DL.Rect>().Any());
    }
}
