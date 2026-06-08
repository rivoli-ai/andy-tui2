using System.Linq;
using Andy.Tui.Widgets;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;
using Xunit;

namespace Andy.Tui.Widgets.Tests;

public class MarkdownInlineCodeTests
{
    private static DL.DisplayList Render(string md, DL.Rgb24 codeColor)
    {
        var r = new MarkdownRenderer();
        r.SetColors(new DL.Rgb24(220, 220, 220), new DL.Rgb24(0, 0, 0), new DL.Rgb24(200, 200, 80));
        r.SetInlineCodeColor(codeColor);
        r.SetText(md);
        var b = new DL.DisplayListBuilder();
        r.Render(new L.Rect(0, 0, 60, 4), new DL.DisplayListBuilder().Build(), b);
        return b.Build();
    }

    [Fact]
    public void InlineCode_IsDrawnInTheCodeColor_WithoutUnderline()
    {
        var codeColor = new DL.Rgb24(11, 22, 33);
        var dl = Render("call `Method` now", codeColor);

        var runs = dl.Ops.OfType<DL.TextRun>().ToList();
        // The characters of the inline code get the code color...
        var codeChars = runs.Where(t => t.Fg.HasValue && t.Fg.Value.Equals(codeColor))
                            .OrderBy(t => t.X).Select(t => t.Content);
        Assert.Equal("Method", string.Concat(codeChars));

        // ...and nothing is underlined.
        Assert.DoesNotContain(runs, t => (t.Attrs & DL.CellAttrFlags.Underline) != 0);
    }

    [Fact]
    public void Italics_AreNotUnderlined()
    {
        var dl = Render("this is *emphasis* text", new DL.Rgb24(11, 22, 33));
        var runs = dl.Ops.OfType<DL.TextRun>().ToList();
        Assert.DoesNotContain(runs, t => (t.Attrs & DL.CellAttrFlags.Underline) != 0);
        // The emphasized word is still rendered (its markers are stripped).
        var text = string.Concat(runs.OrderBy(t => t.X).Select(t => t.Content));
        Assert.Contains("emphasis", text);
    }
}
