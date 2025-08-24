using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class TimelineTests
{
    [Fact]
    public void Renders_Time_And_Text()
    {
        var t = new Andy.Tui.Widgets.Timeline();
        t.SetItems(new[]{new Andy.Tui.Widgets.Timeline.Item("09:00","Started")});
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        t.Render(new L.Rect(0,0,30,3), baseDl, b);
        var dl = b.Build();
        var text = string.Join("", dl.Ops.OfType<DL.TextRun>().Select(x => x.Content));
        Assert.Contains("09:00", text);
        Assert.Contains("Started", text);
    }
}
