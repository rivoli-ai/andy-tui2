using Xunit;
using System.Linq;

namespace Andy.Tui.Text.Tests;

public class UnitTest1
{
    [Fact]
    public void GraphemeEnumerator_Enumerates_Clusters()
    {
        var list = new Andy.Tui.Text.GraphemeEnumerator("A🇫🇷e\u0301").ToList();
        Assert.True(list.Count >= 3);
    }

    [Fact]
    public void TextMeasurer_Counts_Graphemes_With_Wide()
    {
        var m = new Andy.Tui.Text.TextMeasurer();
        Assert.Equal(3, m.MeasureWidth("abc"));
        Assert.True(m.MeasureWidth("好") >= 2); // CJK wide approx
    }

    [Fact]
    public void TextWrapper_WordWrap_Wraps_By_Words()
    {
        var w = new Andy.Tui.Text.TextWrapper();
        var lines = w.Wrap("hello world here", new Andy.Tui.Text.WrapOptions(5, Andy.Tui.Text.WrapStrategy.WordWrap));
        Assert.Equal(new[] { "hello", "world", "here" }, lines);
    }

    [Fact]
    public void TextWrapper_CharacterWrap_Wraps_By_Graphemes()
    {
        var w = new Andy.Tui.Text.TextWrapper();
        var lines = w.Wrap("abcdef", new Andy.Tui.Text.WrapOptions(3, Andy.Tui.Text.WrapStrategy.CharacterWrap));
        Assert.Equal(new[] { "abc", "def" }, lines);
    }
}
