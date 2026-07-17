using System.Linq;
using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;
using Andy.Tui.Text;

namespace Andy.Tui.Rendering.Tests;

/// <summary>
/// Verifies that the compositor and RowRuns now share the single grapheme /
/// cell-width service: whole clusters (flags, skin tones, ZWJ families, keycaps,
/// variation selectors) occupy their true column span, are never split, and are
/// never truncated mid-cluster into a partial grapheme or lone surrogate.
/// </summary>
public class GraphemeUnificationTests
{
    private static CellGrid Render(string content, int width)
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, width, 1));
        b.DrawText(new TextRun(0, 0, content, new Rgb24(200, 200, 200), null, CellAttrFlags.None));
        b.Pop();
        return new TtyCompositor().Composite(b.Build(), (width, 1));
    }

    private static bool HasUnpairedSurrogate(string s)
    {
        for (int i = 0; i < s.Length; i++)
        {
            if (char.IsHighSurrogate(s[i]))
            {
                if (i + 1 >= s.Length || !char.IsLowSurrogate(s[i + 1])) return true;
                i++;
            }
            else if (char.IsLowSurrogate(s[i]))
            {
                return true;
            }
        }
        return false;
    }

    [Fact]
    public void Flag_Occupies_One_Wide_Cell_With_Continuation()
    {
        var jp = "\U0001F1EF\U0001F1F5";
        var g = Render(jp, 6);
        Assert.Equal(jp, g[0, 0].Grapheme); // both regional indicators in one cell
        Assert.Equal(2, g[0, 0].Width);
        Assert.Equal("", g[1, 0].Grapheme); // continuation contributes nothing
        Assert.Equal(0, g[1, 0].Width);
    }

    [Fact]
    public void Skin_Tone_Sequence_Is_A_Single_Wide_Glyph()
    {
        var thumb = "\U0001F44D\U0001F3FD";
        var g = Render(thumb, 6);
        Assert.Equal(thumb, g[0, 0].Grapheme);
        Assert.Equal(2, g[0, 0].Width);
        Assert.Equal("", g[1, 0].Grapheme);
    }

    [Fact]
    public void Zwj_Family_Is_A_Single_Wide_Glyph()
    {
        var family = "\U0001F468‍\U0001F469‍\U0001F467‍\U0001F466";
        var g = Render(family, 6);
        Assert.Equal(family, g[0, 0].Grapheme);
        Assert.Equal(2, g[0, 0].Width);
        Assert.Equal("", g[1, 0].Grapheme);
    }

    [Fact]
    public void Keycap_Sequence_Is_A_Single_Wide_Glyph()
    {
        var keycap = "1️⃣";
        var g = Render(keycap, 6);
        Assert.Equal(keycap, g[0, 0].Grapheme);
        Assert.Equal(2, g[0, 0].Width);
    }

    [Fact]
    public void RowRuns_Report_True_Columns_Not_Utf16_Length()
    {
        // A keycap cluster is 3 UTF-16 units but only 2 terminal columns. The
        // old RowRuns used string length for ColEnd; it must now be the column x.
        var comp = new TtyCompositor();
        var g = Render("1️⃣", 6);
        var runs = comp.RowRuns(g, new[] { new DirtyRect(0, 0, 2, 1) });
        var run = runs.First(r => r.Text.Contains("1"));
        Assert.Equal(0, run.ColStart);
        Assert.Equal(2, run.ColEnd);            // true columns, not the 3-unit length
        Assert.Equal("1️⃣", run.Text);          // full cluster preserved
        Assert.False(HasUnpairedSurrogate(run.Text));
    }

    [Fact]
    public void RowRuns_Never_Truncate_A_Cluster_Into_A_Partial_Grapheme()
    {
        // Place a family emoji (11 UTF-16 units, 2 columns) at the right edge of a
        // narrow grid. The old length-based truncation (maxLen = width - start)
        // would have chopped the cluster and emitted a lone surrogate.
        var family = "\U0001F468‍\U0001F469‍\U0001F467‍\U0001F466";
        var comp = new TtyCompositor();
        var g = Render(family, 2);
        var runs = comp.RowRuns(g, new[] { new DirtyRect(0, 0, 2, 1) });
        var run = runs.First(r => r.Text.Length > 0);
        Assert.Equal(family, run.Text);
        Assert.False(HasUnpairedSurrogate(run.Text));
        Assert.Equal(2, run.ColEnd);
    }

    [Fact]
    public void Measured_Width_Agrees_With_Rendered_Columns()
    {
        // The width the text service measures must equal the number of columns the
        // compositor actually advances across (lead + continuation cells).
        var content = "A漢\U0001F1EF\U0001F1F5B"; // 1 + 2 + 2 + 1 = 6 columns
        int measured = TerminalText.MeasureWidth(content);
        Assert.Equal(6, measured);

        var g = Render(content, 10);
        int columns = 0;
        for (int x = 0; x < 10; x++)
        {
            var cell = g[x, 0];
            if (cell.Width == 0 && string.IsNullOrEmpty(cell.Grapheme)) continue;
            columns += cell.Width;
        }
        Assert.Equal(measured, columns);
    }
}
