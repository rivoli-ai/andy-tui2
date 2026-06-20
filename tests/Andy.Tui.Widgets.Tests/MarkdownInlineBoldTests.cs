using System.Linq;
using Andy.Tui.Widgets;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;
using Xunit;

namespace Andy.Tui.Widgets.Tests;

/// <summary>
/// Inline strong/emphasis span boundary tests for <see cref="MarkdownRenderer"/>.
///
/// Context: a chat CLI consumes the renderer's DisplayList of TextRuns and reads the
/// per-cell <see cref="DL.CellAttrFlags.Bold"/> flag. A regression caused the Bold flag to
/// be applied to an entire line (and to leak onto following lines) instead of only the text
/// between a matched pair of <c>**</c> markers. These tests assert on the per-run Bold flag
/// exactly as the CLI consumes it.
/// </summary>
public class MarkdownInlineBoldTests
{
    private static DL.DisplayList Render(string md, L.Rect rect)
    {
        var r = new MarkdownRenderer();
        r.SetColors(new DL.Rgb24(220, 220, 220), new DL.Rgb24(0, 0, 0), new DL.Rgb24(200, 200, 80));
        r.SetInlineCodeColor(new DL.Rgb24(11, 22, 33));
        r.SetText(md);
        var b = new DL.DisplayListBuilder();
        r.Render(rect, new DL.DisplayListBuilder().Build(), b);
        return b.Build();
    }

    private static bool IsBold(DL.TextRun t) => (t.Attrs & DL.CellAttrFlags.Bold) != 0;

    // The characters (in render order) that carry the Bold flag.
    private static string BoldText(DL.DisplayList dl)
        => string.Concat(dl.Ops.OfType<DL.TextRun>()
            .Where(IsBold).OrderBy(t => t.Y).ThenBy(t => t.X).Select(t => t.Content));

    // All rendered characters on a given visual row, in order.
    private static string RowText(DL.DisplayList dl, int y)
        => string.Concat(dl.Ops.OfType<DL.TextRun>()
            .Where(t => t.Y == y).OrderBy(t => t.X).Select(t => t.Content));

    [Fact]
    public void OnlyTextBetweenMatchedStrongMarkers_IsBold()
    {
        var dl = Render("Some **bold** word here", new L.Rect(0, 0, 60, 4));
        Assert.Equal("bold", BoldText(dl));
    }

    [Fact]
    public void TextSurroundingStrongSpan_IsNotBold()
    {
        var dl = Render("Some **bold** word here", new L.Rect(0, 0, 60, 4));
        var runs = dl.Ops.OfType<DL.TextRun>().ToList();
        // "Some " before and " word here" after must not be bold.
        Assert.DoesNotContain(runs.Where(t => "Some word here".Contains(t.Content) && t.Content != "b" && t.Content != "o" && t.Content != "l" && t.Content != "d"), IsBold);
        // The whole line is rendered; only "bold" is bold.
        Assert.Equal("bold", BoldText(dl));
    }

    [Fact]
    public void UnderscoreStrong_OnlyInnerTextIsBold()
    {
        var dl = Render("a __strong__ b", new L.Rect(0, 0, 60, 4));
        Assert.Equal("strong", BoldText(dl));
    }

    [Fact]
    public void MultipleStrongSpansOnOneLine_AreIndependentlyBold()
    {
        var dl = Render("a **b** c **d** e", new L.Rect(0, 0, 60, 4));
        Assert.Equal("bd", BoldText(dl));
    }

    [Fact]
    public void StrongDoesNotLeakOntoFollowingLine()
    {
        var dl = Render("**bold line**\nplain line next", new L.Rect(0, 0, 60, 6));
        Assert.Equal("bold line", BoldText(dl));
        // Second visual row carries no bold cells at all.
        var secondRow = dl.Ops.OfType<DL.TextRun>().Where(t => t.Y == 1).ToList();
        Assert.NotEmpty(secondRow);
        Assert.DoesNotContain(secondRow, IsBold);
    }

    [Fact]
    public void UnclosedStrongMarker_DoesNotBoldRemainderOfLine()
    {
        var dl = Render("Some **bold with no close here", new L.Rect(0, 0, 60, 4));
        // Nothing should be bold; the stray ** is treated as literal text.
        Assert.Equal(string.Empty, BoldText(dl));
        // And the literal markers survive in the output.
        Assert.Contains("**", RowText(dl, 0));
    }

    [Fact]
    public void UnclosedStrongMarker_DoesNotLeakToLaterLines()
    {
        var dl = Render("intro **dangling\nclean second line\nclean third line", new L.Rect(0, 0, 60, 6));
        Assert.Equal(string.Empty, BoldText(dl));
    }

    [Fact]
    public void HeaderRemainsFullyBold_EvenWithInlineMarkersInside()
    {
        // A heading is uniformly styled; inline markers inside it must not punch holes
        // in the bold styling.
        var dl = Render("# Title **x** end", new L.Rect(0, 0, 60, 4));
        var runs = dl.Ops.OfType<DL.TextRun>().Where(t => t.Y == 0).ToList();
        Assert.NotEmpty(runs);
        Assert.All(runs, t => Assert.True(IsBold(t), $"header glyph '{t.Content}' should be bold"));
        // Markers are preserved literally inside the heading text.
        Assert.Contains("**", RowText(dl, 0));
    }

    [Fact]
    public void Emphasis_OnlyInnerTextIsRendered_AndNotBold()
    {
        var dl = Render("this is *emphasis* text", new L.Rect(0, 0, 60, 4));
        var row = RowText(dl, 0);
        Assert.Equal("this is emphasis text", row);
        // Emphasis does not use the Bold flag.
        Assert.Equal(string.Empty, BoldText(dl));
    }

    [Fact]
    public void StrongSpanWrappingAcrossRows_BoldOnBothRows_NoLeakAfter()
    {
        // A long bold span that wraps must keep the wrapped continuation bold, and the
        // trailing non-bold text after the close must not be bold.
        var inner = new string('a', 40);
        var dl = Render($"x **{inner}** tail", new L.Rect(0, 0, 20, 6));
        var boldCount = dl.Ops.OfType<DL.TextRun>().Count(t => IsBold(t) && t.Content == "a");
        Assert.Equal(40, boldCount);
        // "tail" letters must not be bold.
        Assert.DoesNotContain(dl.Ops.OfType<DL.TextRun>().Where(t => "tail".Contains(t.Content) && t.Content != "a"), IsBold);
    }

    [Fact]
    public void EmptyStrongMarkers_DoNotBoldTheRestOfTheLine()
    {
        // "****" has no content; the matched pair "**c**" later on the line must still
        // bold "c", and the empty markers must not bold the entire remainder of the line.
        var dl = Render("start **** then **c** end", new L.Rect(0, 0, 60, 4));
        var bold = BoldText(dl);
        Assert.Contains("c", bold);
        // The trailing literal words must not be bold.
        Assert.DoesNotContain("then", bold);
        Assert.DoesNotContain("end", bold);
    }
}
