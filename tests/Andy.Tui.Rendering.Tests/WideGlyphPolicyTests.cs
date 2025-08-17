using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Rendering.Tests;

public class WideGlyphPolicyTests
{
    [Fact(Skip = "Wide glyph policy pending implementation")]
    public void Double_Width_Glyph_At_Edge_Uses_Placeholder()
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0,0,3,1));
        // Use a representative wide character (CJK). Here just placeholder string; real test should use actual wide char
        var wideChar = "漢"; // double-width in most terminals
        b.DrawText(new TextRun(2,0,wideChar, new Rgb24(255,255,255), null, CellAttrFlags.None));
        b.Pop();
        var g = new TtyCompositor().Composite(b.Build(), (3,1));
        // Policy: last column wide glyph replaced with placeholder or truncated; assert single-cell placeholder
        Assert.Equal("?", g[2,0].Grapheme);
    }
}
