using System.Linq;
using Andy.Tui.Text;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

/// <summary>
/// Regression tests for issue #77: ListView truncation and TokenCounter.GetWidth
/// must measure in terminal cells (via <see cref="TerminalText"/>) rather than
/// UTF-16 code units / grapheme counts, so widget width math agrees with the
/// cell-accurate rendering for wide glyphs (CJK / flag emoji / ZWJ sequences).
/// </summary>
public class CellWidthCursorTests
{
    private static string RenderedRow(Andy.Tui.Widgets.ListView lv, int w, int h)
    {
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        lv.Render(new L.Rect(0, 0, w, h), baseDl, b);
        var dl = b.Build();
        // First (and only) content row text run.
        return dl.Ops.OfType<DL.TextRun>().First().Content;
    }

    [Fact]
    public void ListView_Truncates_Cjk_By_Cells_Not_CodeUnits()
    {
        var lv = new Andy.Tui.Widgets.ListView();
        lv.SetItems(new[] { "中中中中中" }); // 5 wide glyphs = 10 cells, but each is 1 UTF-16 unit
        // width 6 -> contentW = 4 cells -> exactly two CJK glyphs fit (4 cells).
        string text = RenderedRow(lv, 6, 3);

        Assert.Equal("中中", text);
        Assert.Equal(4, TerminalText.MeasureWidth(text));
        // Never exceeds the content width in cells.
        Assert.True(TerminalText.MeasureWidth(text) <= 4);
        // Reverting to text.Substring(0, contentW) would yield "中中中中" (8 cells), overflowing.
    }

    [Fact]
    public void ListView_Truncates_Flag_Emoji_On_Cluster_Boundary()
    {
        var lv = new Andy.Tui.Widgets.ListView();
        lv.SetItems(new[] { "🇺🇸abc" }); // flag (2 cells / 4 UTF-16 units) + a,b,c
        // width 5 -> contentW = 3 cells -> flag(2) + 'a'(1) fit; 'b' would overflow.
        string text = RenderedRow(lv, 5, 3);

        Assert.Equal("🇺🇸a", text);
        Assert.Equal(3, TerminalText.MeasureWidth(text));
        // A code-unit Substring(0,3) would split the flag's surrogate pairs.
    }

    [Fact]
    public void ListView_Truncates_Zwj_Family_On_Cluster_Boundary()
    {
        var lv = new Andy.Tui.Widgets.ListView();
        lv.SetItems(new[] { "👨‍👩‍👧hi" }); // ZWJ family cluster (2 cells, many UTF-16 units) + h,i
        // width 5 -> contentW = 3 cells -> family(2) + 'h'(1) fit; 'i' would overflow.
        string text = RenderedRow(lv, 5, 3);

        Assert.Equal("👨‍👩‍👧h", text);
        Assert.Equal(3, TerminalText.MeasureWidth(text));
        // A code-unit Substring would slice the ZWJ cluster mid-sequence.
    }

    [Fact]
    public void ListView_Short_Text_Is_Not_Truncated()
    {
        var lv = new Andy.Tui.Widgets.ListView();
        lv.SetItems(new[] { "中a" }); // 3 cells
        string text = RenderedRow(lv, 8, 3); // contentW = 6 cells, plenty
        Assert.Equal("中a", text);
    }

    [Fact]
    public void ListView_Truncates_On_Odd_Cell_Budget()
    {
        var lv = new Andy.Tui.Widgets.ListView();
        lv.SetItems(new[] { "中中中" }); // 6 cells
        // width 5 -> contentW = 3 cells (odd): only one wide glyph fits (2 cells),
        // the next would need cells 3-4 and overflow.
        string text = RenderedRow(lv, 5, 3);
        Assert.Equal("中", text);
        Assert.True(TerminalText.MeasureWidth(text) <= 3);
    }

    [Fact]
    public void TokenCounter_GetWidth_Returns_Cell_Width()
    {
        var tc = new Andy.Tui.CliWidgets.TokenCounter();
        tc.AddTokens(12, 34);
        string label = "Total: 12→34 (46)";

        // GetWidth agrees with the shared cell-width service, not a raw code-unit count.
        Assert.Equal(TerminalText.MeasureWidth(label), tc.GetWidth());
    }

    [Fact]
    public void TerminalText_MeasureWidth_Cell_Semantics()
    {
        // The cell-width semantics that GetWidth / ListView truncation now rely on.
        Assert.Equal(1, TerminalText.MeasureWidth("a"));
        Assert.Equal(2, TerminalText.MeasureWidth("中"));   // CJK wide
        Assert.Equal(2, TerminalText.MeasureWidth("🇺🇸"));  // flag emoji cluster
        Assert.Equal(2, TerminalText.MeasureWidth("👨‍👩‍👧")); // ZWJ family cluster
    }
}
