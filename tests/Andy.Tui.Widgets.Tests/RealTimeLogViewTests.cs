using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class RealTimeLogViewTests
{
    [Fact]
    public void Appends_Render_Last_Lines_When_Following_Tail()
    {
        var v = new Andy.Tui.Widgets.RealTimeLogView();
        for (int i = 1; i <= 50; i++) v.AppendLine($"Line {i}");
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        v.Render(new L.Rect(0, 0, 20, 5), baseDl, b);
        var dl = b.Build();
        var lines = dl.Ops.OfType<DL.TextRun>().Select(r => r.Content).ToList();
        Assert.Contains("Line 46", lines[0]);
        Assert.Contains("Line 50", lines[^1]);
    }

    [Fact]
    public void AdjustScroll_Disables_Follow_And_Shifts_Window()
    {
        var v = new Andy.Tui.Widgets.RealTimeLogView();
        for (int i = 1; i <= 30; i++) v.AppendLine($"L{i}");
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        // Scroll up by 3 in a 5-row viewport
        v.AdjustScroll(-3, viewportRows: 5);
        v.Render(new L.Rect(0, 0, 20, 5), baseDl, b);
        var dl = b.Build();
        var lines = dl.Ops.OfType<DL.TextRun>().Select(r => r.Content).ToList();
        Assert.Equal("L23", lines[0]);
        Assert.Equal("L27", lines[^1]);
    }
}
