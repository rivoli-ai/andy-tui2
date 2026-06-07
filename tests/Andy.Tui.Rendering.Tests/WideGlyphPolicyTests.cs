using System.Linq;
using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Rendering.Tests;

public class WideGlyphPolicyTests
{
    [Fact]
    public void Double_Width_Glyph_At_Edge_Uses_Placeholder()
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, 3, 1));
        var wideChar = "漢"; // double-width CJK
        b.DrawText(new TextRun(2, 0, wideChar, new Rgb24(255, 255, 255), null, CellAttrFlags.None));
        b.Pop();
        var g = new TtyCompositor().Composite(b.Build(), (3, 1));
        // Last column wide glyph replaced with a single-cell placeholder.
        Assert.Equal("?", g[2, 0].Grapheme);
    }

    [Fact]
    public void Cjk_Glyph_Occupies_Two_Cells_With_Continuation()
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, 6, 1));
        b.DrawText(new TextRun(0, 0, "漢字", new Rgb24(200, 200, 200), null, CellAttrFlags.None));
        b.Pop();
        var g = new TtyCompositor().Composite(b.Build(), (6, 1));

        Assert.Equal("漢", g[0, 0].Grapheme);
        Assert.Equal(2, g[0, 0].Width);
        Assert.Equal("", g[1, 0].Grapheme);   // continuation: emits nothing
        Assert.Equal(0, g[1, 0].Width);
        Assert.Equal("字", g[2, 0].Grapheme);
        Assert.Equal(2, g[2, 0].Width);
        Assert.Equal("", g[3, 0].Grapheme);
    }

    [Fact]
    public void Emoji_Surrogate_Pair_Is_One_Wide_Glyph()
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, 6, 1));
        b.DrawText(new TextRun(0, 0, "🌲🐄", new Rgb24(0, 200, 0), null, CellAttrFlags.None));
        b.Pop();
        var g = new TtyCompositor().Composite(b.Build(), (6, 1));

        Assert.Equal("🌲", g[0, 0].Grapheme);
        Assert.Equal(2, g[0, 0].Width);
        Assert.Equal("", g[1, 0].Grapheme);
        Assert.Equal("🐄", g[2, 0].Grapheme);
        Assert.Equal(2, g[2, 0].Width);
    }

    [Fact]
    public void Variation_Selector_Merges_Into_Preceding_Glyph()
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, 6, 1));
        // Desktop computer + VS16 (emoji presentation).
        b.DrawText(new TextRun(0, 0, "🖥️", new Rgb24(200, 200, 200), null, CellAttrFlags.None));
        b.Pop();
        var g = new TtyCompositor().Composite(b.Build(), (6, 1));

        Assert.Equal("🖥️", g[0, 0].Grapheme); // base + VS16 in one cell
        Assert.Equal(2, g[0, 0].Width);
        Assert.Equal("", g[1, 0].Grapheme);
    }

    [Fact]
    public void RowRuns_Emit_Wide_Glyph_Once_At_Correct_Column()
    {
        var comp = new TtyCompositor();
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, 6, 1));
        b.DrawText(new TextRun(2, 0, "🌲", new Rgb24(0, 200, 0), null, CellAttrFlags.None));
        b.Pop();
        var g = comp.Composite(b.Build(), (6, 1));

        var runs = comp.RowRuns(g, new[] { new DirtyRect(2, 0, 2, 1) });
        var run = runs.Single(r => r.Text.Contains("🌲"));
        Assert.Equal(2, run.ColStart);                 // true column, not a string offset
        Assert.Equal("🌲", run.Text);                  // continuation contributes nothing
    }

    [Fact]
    public void Narrow_Glyphs_Are_Unchanged()
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, 5, 1));
        b.DrawText(new TextRun(0, 0, "abc", new Rgb24(255, 255, 255), null, CellAttrFlags.None));
        b.Pop();
        var g = new TtyCompositor().Composite(b.Build(), (5, 1));

        Assert.Equal("a", g[0, 0].Grapheme);
        Assert.Equal(1, g[0, 0].Width);
        Assert.Equal("b", g[1, 0].Grapheme);
        Assert.Equal("c", g[2, 0].Grapheme);
    }
}
