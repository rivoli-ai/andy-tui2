using System.Linq;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class ToggleCheckboxRadioTests
{
    [Fact]
    public void Toggle_Renders_On_And_Focused_Bold()
    {
        var tgl = new Andy.Tui.Widgets.Toggle(initial: true, label: "WiFi");
        tgl.SetFocused(true);
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        tgl.Render(new L.Rect(0, 0, 12, 1), baseDl, b);
        var dl = b.Build();
        var text = dl.Ops.OfType<DL.TextRun>().FirstOrDefault();
        Assert.Contains("ON", text!.Content);
        Assert.True((text!.Attrs & DL.CellAttrFlags.Bold) == DL.CellAttrFlags.Bold);
    }

    [Fact]
    public void Checkbox_Renders_Mark_When_Checked()
    {
        var cb = new Andy.Tui.Widgets.Checkbox("Receive", initial: true);
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        cb.Render(new L.Rect(0, 0, 20, 1), baseDl, b);
        var dl = b.Build();
        var text = dl.Ops.OfType<DL.TextRun>().Select(t => t.Content).FirstOrDefault();
        Assert.NotNull(text);
        Assert.Contains("[x]", text);
    }

    [Fact]
    public void RadioGroup_Renders_Selected_With_Bold()
    {
        var rg = new Andy.Tui.Widgets.RadioGroup();
        rg.SetItems(new[] { "Red", "Green", "Blue" });
        rg.SetSelectedIndex(1);
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        rg.Render(new L.Rect(0, 0, 20, 5), baseDl, b);
        var dl = b.Build();
        var runs = dl.Ops.OfType<DL.TextRun>().ToList();
        Assert.True(runs.Count >= 3);
        // Selected line should include marker and be bold
        var selected = runs[1];
        Assert.Contains("(o)", selected.Content);
        Assert.True((selected.Attrs & DL.CellAttrFlags.Bold) == DL.CellAttrFlags.Bold);
    }
}

public class TextInputScrollViewTests
{
    [Fact]
    public void TextInput_Shows_Caret_When_Focused()
    {
        var ti = new Andy.Tui.Widgets.TextInput();
        ti.SetText("Hello");
        ti.SetFocused(true);
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        ti.Render(new L.Rect(0, 0, 10, 1), baseDl, b);
        var dl = b.Build();
        var contents = dl.Ops.OfType<DL.TextRun>().Select(t => t.Content).ToList();
        Assert.Contains("Hello", contents);
        Assert.Contains("|", contents);
    }

    [Fact]
    public void ScrollView_Renders_From_ScrollY_And_Clamps()
    {
        var sv = new Andy.Tui.Widgets.ScrollView();
        sv.SetContent(string.Join('\n', Enumerable.Range(1, 50).Select(i => $"Line {i}")));
        sv.SetScrollY(48); // near end
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        sv.Render(new L.Rect(0, 0, 20, 3), baseDl, b);
        var dl = b.Build();
        var lines = dl.Ops.OfType<DL.TextRun>().Select(t => t.Content).ToList();
        // Should include last lines; clamp prevents out-of-range
        Assert.Contains("Line 49", lines);
        Assert.Contains("Line 50", lines);
    }
}

public class ProgressSliderListBoxTests
{
    [Fact]
    public void ProgressBar_Fill_Width_Matches_Value()
    {
        var pb = new Andy.Tui.Widgets.ProgressBar { Value = 0.6 };
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        pb.Render(new L.Rect(0, 0, 20, 1), baseDl, b);
        var dl = b.Build();
        // Find the fill rect by its fill color (60,140,220) and width 12 (approx)
        var fill = dl.Ops.OfType<DL.Rect>().FirstOrDefault(r => r.Fill.R == 60 && r.Fill.G == 140 && r.Fill.B == 220);
        Assert.True(fill.Width == 12);
    }

    [Fact]
    public void Slider_Thumb_Position_Follows_Value()
    {
        var sl = new Andy.Tui.Widgets.Slider { Value = 0.5 };
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        sl.Render(new L.Rect(0, 0, 20, 1), baseDl, b);
        var dl = b.Build();
        var thumb = dl.Ops.OfType<DL.TextRun>().FirstOrDefault(tr => tr.Content == "|");
        // Expected around middle: x = 1 + (w-3)*0.5 ≈ 1 + 17*0.5 = 9.5 -> 10
        Assert.Equal(10, thumb!.X);
    }

    [Fact]
    public void ListBox_Renders_Selected_Bold()
    {
        var lb = new Andy.Tui.Widgets.ListBox();
        lb.SetItems(new[] { "A", "B", "C" });
        lb.SetSelectedIndex(0);
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        lb.Render(new L.Rect(0, 0, 10, 3), baseDl, b);
        var dl = b.Build();
        var first = dl.Ops.OfType<DL.TextRun>().First();
        Assert.True((first.Attrs & DL.CellAttrFlags.Bold) == DL.CellAttrFlags.Bold);
    }
}
