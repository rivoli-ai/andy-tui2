using Andy.Tui.Text;
using Xunit;

namespace Andy.Tui.Text.Tests;

public class GraphemeAndWidthTests
{
    [Fact]
    public void Combining_Marks_Treated_As_Single_Grapheme()
    {
        var s = "e\u0301"; // e + combining acute
        var graphemes = new GraphemeEnumerator(s).ToList();
        Assert.Single(graphemes);
    }

    [Fact]
    public void ZWJ_Sequence_Treated_As_Single_Grapheme()
    {
        var s = "👨\u200D👩\u200D👧\u200D👦"; // family emoji via ZWJ
        var graphemes = new GraphemeEnumerator(s).ToList();
        Assert.Single(graphemes);
    }

    [Fact(Skip = "Wide-at-edge policy pending")]
    public void Double_Width_At_Edge_Policy()
    {
        var s = "漢"; // likely double-width
        var cp = char.ConvertToUtf32(s, 0);
        var width = WcWidthProxy.GetCharWidth(cp);
        Assert.Equal(2, width);
        // Policy check to be asserted when compositor/oracle enforce it
    }
}

internal static class WcWidthProxy
{
    public static int GetCharWidth(int codePoint)
    {
        // Reflection-free proxy using the same approximations as implementation
        if (codePoint == 0) return 0;
        if (codePoint < 32 || (codePoint >= 0x7f && codePoint < 0xa0)) return 0;
        return IsWide(codePoint) ? 2 : 1;
    }

    private static bool IsWide(int codePoint)
    {
        return
            (codePoint >= 0x1100 && codePoint <= 0x115F) ||
            (codePoint == 0x2329 || codePoint == 0x232A) ||
            (codePoint >= 0x2E80 && codePoint <= 0xA4CF) ||
            (codePoint >= 0xAC00 && codePoint <= 0xD7A3) ||
            (codePoint >= 0xF900 && codePoint <= 0xFAFF) ||
            (codePoint >= 0xFE10 && codePoint <= 0xFE19) ||
            (codePoint >= 0xFE30 && codePoint <= 0xFE6F) ||
            (codePoint >= 0xFF00 && codePoint <= 0xFF60) ||
            (codePoint >= 0xFFE0 && codePoint <= 0xFFE6) ||
            (codePoint >= 0x1F300 && codePoint <= 0x1F64F) ||
            (codePoint >= 0x1F900 && codePoint <= 0x1F9FF) ||
            (codePoint >= 0x20000 && codePoint <= 0x3FFFD);
    }
}
