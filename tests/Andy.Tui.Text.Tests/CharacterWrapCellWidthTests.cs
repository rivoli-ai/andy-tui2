using System.Linq;
using Andy.Tui.Text;
using Xunit;

namespace Andy.Tui.Text.Tests;

/// <summary>
/// Character wrapping must respect terminal-cell width, not grapheme count, so
/// wide clusters (CJK, emoji, flags) never overflow the target width and whole
/// clusters are never split.
/// </summary>
public class CharacterWrapCellWidthTests
{
    private static IReadOnlyList<string> Wrap(string text, int maxWidth)
        => new TextWrapper().Wrap(text, new WrapOptions(maxWidth, WrapStrategy.CharacterWrap));

    [Fact]
    public void Wide_Glyphs_Wrap_On_Cell_Width_Not_Grapheme_Count()
    {
        // Three double-width ideographs into a 4-column line: two fit (width 4),
        // the third wraps. A naive one-cell-per-grapheme wrapper would have put
        // all three (6 columns) on one line.
        var lines = Wrap("漢字漢", 4);
        Assert.Equal(2, lines.Count);
        Assert.Equal("漢字", lines[0]);
        Assert.Equal("漢", lines[1]);
        Assert.All(lines, l => Assert.True(TerminalText.MeasureWidth(l) <= 4));
    }

    [Fact]
    public void Emoji_Cluster_Is_Never_Split_By_Wrapping()
    {
        var family = "\U0001F468‍\U0001F469‍\U0001F467‍\U0001F466";
        var lines = Wrap("ab" + family + "cd", 3);
        // Every produced line must still contain only whole grapheme clusters.
        Assert.Contains(lines, l => l.Contains(family));
        Assert.All(lines, l => Assert.True(TerminalText.MeasureWidth(l) <= 3));
    }

    [Fact]
    public void Narrow_Text_Unchanged()
    {
        var lines = Wrap("abcdef", 3);
        Assert.Equal(new[] { "abc", "def" }, lines);
    }
}
