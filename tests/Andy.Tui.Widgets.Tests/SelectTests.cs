using Xunit;
using DL = Andy.Tui.DisplayList;

namespace Andy.Tui.Widgets.Tests;

public class SelectTests
{
    [Fact]
    public void MeasureClosedWidth_Accounts_For_Text_And_Arrow()
    {
        var s = new Select();
        s.SetItems(new[] { "Short", "A bit longer" });
        int w = s.MeasureClosedWidth();
        Assert.True(w >= "A bit longer".Length + 4);
    }

    [Fact]
    public void Render_Draws_Border_Text_And_Arrow()
    {
        var s = new Select();
        s.SetItems(new[] { "Apple", "Banana" });
        s.SetSelectedIndex(1);
        var baseB = new DL.DisplayListBuilder();
        var baseDl = baseB.Build();
        var b = new DL.DisplayListBuilder();
        s.Render(new Andy.Tui.Layout.Rect(0, 0, 16, 1), baseDl, b);
        var dl = b.Build();
        Assert.Contains(dl.Ops, op => op is DL.Border);
        Assert.Contains(dl.Ops, op => op is DL.TextRun tr && tr.Content.Contains("Banana"));
        Assert.Contains(dl.Ops, op => op is DL.TextRun tr && tr.Content.Contains("▼"));
    }

    [Fact]
    public void Open_Renders_Popup_With_Items_And_Highlight()
    {
        var s = new Select();
        s.SetItems(new[] { "One", "Two", "Three" });
        s.SetSelectedIndex(0);
        s.SetOpen(true);
        var baseB = new DL.DisplayListBuilder();
        var baseDl = baseB.Build();
        var b = new DL.DisplayListBuilder();
        s.Render(new Andy.Tui.Layout.Rect(0, 0, 10, 1), baseDl, b);
        s.RenderPopup(0, 1, 40, 10, baseDl, b);
        var dl = b.Build();
        Assert.Contains(dl.Ops, op => op is DL.Border); // popup border
        Assert.Contains(dl.Ops, op => op is DL.TextRun tr && tr.Content.Contains("One"));
        Assert.Contains(dl.Ops, op => op is DL.TextRun tr && tr.Content.Contains("Two"));
    }
}
