using System.Linq;
using Andy.Tui.Text;
using Xunit;

namespace Andy.Tui.Text.Tests;

/// <summary>
/// Exercises the single shared grapheme/cell-width service. Every case that the
/// issue calls out — combining marks, variation selectors, CJK, flags, keycaps,
/// skin tones, and family emoji — is measured here and must agree with how the
/// compositor lays the same text out (see the rendering-side tests).
/// </summary>
public class TerminalTextTests
{
    [Fact]
    public void Combining_Mark_Is_One_Grapheme_One_Column()
    {
        var s = "é"; // e + combining acute
        Assert.Single(TerminalText.EnumerateGraphemes(s));
        Assert.Equal(1, TerminalText.GraphemeCellWidth(s));
        Assert.Equal(1, TerminalText.MeasureWidth(s));
    }

    [Fact]
    public void Variation_Selector_Forces_Emoji_Presentation_Width_Two()
    {
        var s = "❤️"; // red heart + VS16 (narrow base becomes emoji)
        Assert.Single(TerminalText.EnumerateGraphemes(s));
        Assert.Equal(2, TerminalText.GraphemeCellWidth(s));
    }

    [Fact]
    public void Cjk_Ideograph_Is_Two_Columns()
    {
        Assert.Equal(2, TerminalText.GraphemeCellWidth("漢"));
        Assert.Equal(4, TerminalText.MeasureWidth("漢字"));
    }

    [Fact]
    public void Flag_Is_One_Grapheme_Two_Columns()
    {
        var jp = "\U0001F1EF\U0001F1F5"; // regional indicators J + P
        Assert.Single(TerminalText.EnumerateGraphemes(jp));
        Assert.Equal(2, TerminalText.GraphemeCellWidth(jp));
    }

    [Fact]
    public void Keycap_Sequence_Is_One_Grapheme_Two_Columns()
    {
        var keycap = "1️⃣"; // digit one + VS16 + combining enclosing keycap
        Assert.Single(TerminalText.EnumerateGraphemes(keycap));
        Assert.Equal(2, TerminalText.GraphemeCellWidth(keycap));
    }

    [Fact]
    public void Skin_Tone_Modifier_Stays_In_One_Two_Column_Grapheme()
    {
        var thumb = "\U0001F44D\U0001F3FD"; // thumbs up + medium skin tone
        Assert.Single(TerminalText.EnumerateGraphemes(thumb));
        Assert.Equal(2, TerminalText.GraphemeCellWidth(thumb));
    }

    [Fact]
    public void Zwj_Family_Is_One_Grapheme_Two_Columns()
    {
        var family = "\U0001F468‍\U0001F469‍\U0001F467‍\U0001F466";
        Assert.Single(TerminalText.EnumerateGraphemes(family));
        Assert.Equal(2, TerminalText.GraphemeCellWidth(family));
    }

    [Fact]
    public void MeasureWidth_Sums_Mixed_Content()
    {
        // A(1) + CJK(2) + emoji(2) + B(1)
        Assert.Equal(6, TerminalText.MeasureWidth("A漢🌲B"));
    }

    [Fact]
    public void Control_And_Combining_Scalars_Are_Zero_Width()
    {
        Assert.Equal(0, TerminalText.ScalarCellWidth('\t'));
        Assert.Equal(0, TerminalText.ScalarCellWidth(0x200D)); // ZWJ
        Assert.Equal(0, TerminalText.ScalarCellWidth(0x0301)); // combining acute
        Assert.True(TerminalText.IsZeroWidth(0xFE0F));
    }

    [Fact]
    public void Ambiguous_Width_Policy_Is_Narrow()
    {
        // East Asian Ambiguous characters default to 1 column (narrow).
        Assert.True(TerminalText.IsAmbiguous(0x2190));   // leftwards arrow
        Assert.Equal(1, TerminalText.ScalarCellWidth(0x2190));
        Assert.True(TerminalText.IsAmbiguous(0x25A0));   // black square
        Assert.Equal(1, TerminalText.ScalarCellWidth(0x25A0));
    }
}
