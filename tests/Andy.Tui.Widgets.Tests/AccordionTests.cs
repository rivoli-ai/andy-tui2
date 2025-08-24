using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class AccordionTests
{
    [Fact]
    public void Renders_Headers_And_Toggles_Content()
    {
        var a = new Andy.Tui.Widgets.Accordion();
        a.SetItems(new[]
        {
            new Andy.Tui.Widgets.Accordion.Item("H1", (r,bd,b) => b.DrawText(new DL.TextRun((int)r.X, (int)r.Y, "C1", new DL.Rgb24(255,255,255), null, DL.CellAttrFlags.None)))
        });
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        a.Render(new L.Rect(0, 0, 30, 5), baseDl, b);
        var dl = b.Build();
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), t => t.Content.Contains("H1"));
        a.ToggleExpanded(0);
        b = new DL.DisplayListBuilder();
        a.Render(new L.Rect(0, 0, 30, 5), baseDl, b);
        dl = b.Build();
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), t => t.Content.Contains("C1"));
    }

    [Fact]
    public void Toggle_Changes_Arrow_Indicator()
    {
        var a = new Andy.Tui.Widgets.Accordion();
        a.SetItems(new[]
        {
            new Andy.Tui.Widgets.Accordion.Item("Header", (r,bd,b) => { })
        });
        var baseDl = new DL.DisplayListBuilder().Build();

        var b = new DL.DisplayListBuilder();
        a.Render(new L.Rect(0, 0, 20, 5), baseDl, b);
        var dl = b.Build();
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), t => t.Content.Contains("▶"));

        a.ToggleExpanded(0);
        b = new DL.DisplayListBuilder();
        a.Render(new L.Rect(0, 0, 20, 5), baseDl, b);
        dl = b.Build();
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), t => t.Content.Contains("▼"));
    }

    [Fact]
    public void Active_Header_Is_Bold()
    {
        var a = new Andy.Tui.Widgets.Accordion();
        a.SetItems(new[]
        {
            new Andy.Tui.Widgets.Accordion.Item("S1", (r,bd,b) => { }),
            new Andy.Tui.Widgets.Accordion.Item("S2", (r,bd,b) => { }),
        });
        a.SetActive(1);
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        a.Render(new L.Rect(0, 0, 30, 4), baseDl, b);
        var dl = b.Build();
        var tr = dl.Ops.OfType<DL.TextRun>().FirstOrDefault(t => t.Content.Contains("S2"));
        Assert.NotNull(tr);
        Assert.True((tr!.Attrs & DL.CellAttrFlags.Bold) != 0);
    }

    [Fact]
    public void Expanded_Content_Is_Indented()
    {
        var a = new Andy.Tui.Widgets.Accordion();
        a.SetItems(new[]
        {
            new Andy.Tui.Widgets.Accordion.Item("Head", (r,bd,b) => b.DrawText(new DL.TextRun((int)r.X, (int)r.Y, "Payload", new DL.Rgb24(255,255,255), null, DL.CellAttrFlags.None)))
        });
        a.ToggleExpanded(0);
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        a.Render(new L.Rect(0, 0, 20, 6), baseDl, b);
        var dl = b.Build();
        var payload = dl.Ops.OfType<DL.TextRun>().FirstOrDefault(t => t.Content == "Payload");
        Assert.NotNull(payload);
        Assert.True(payload!.X >= 2);
    }
}
