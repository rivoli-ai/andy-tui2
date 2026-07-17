using Andy.Tui.Backend.Terminal;
using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Rendering.Tests;

/// <summary>
/// Proves that the vertical-scroll damage optimisation produces incremental
/// output which, applied to the previously displayed frame, reproduces the next
/// frame exactly — the same result a full repaint would give. A virtual-screen
/// oracle (which understands the SU/SD scroll operations) is the arbiter.
/// </summary>
public class ScrollDamageParityTests
{
    private static readonly TerminalCapabilities Caps = new()
    {
        TrueColor = true,
        Palette256 = true,
        ScrollRegion = true
    };

    // Distinct, fully-opaque content per source row so a shift is unambiguous.
    private static Cell RowCell(int x, int contentRow)
    {
        var g = ((char)('A' + (contentRow % 26))).ToString();
        var fg = new Rgb24((byte)(30 + contentRow * 5), 180, 90);
        var bg = new Rgb24(0, 0, (byte)(contentRow % 7));
        return new Cell(g, 1, fg, bg, CellAttrFlags.None);
    }

    /// <summary>Builds next by shifting prev's rows by <paramref name="dy"/>.</summary>
    private static (CellGrid prev, CellGrid next) MakeShift(int w, int h, int dy)
    {
        var prev = new CellGrid(w, h);
        var next = new CellGrid(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                prev[x, y] = RowCell(x, y);
        for (int y = 0; y < h; y++)
        {
            int src = y - dy;
            for (int x = 0; x < w; x++)
                next[x, y] = (src >= 0 && src < h) ? prev[x, src] : new Cell(" ", 1, new Rgb24(200, 200, 200), new Rgb24(10, 10, 10), CellAttrFlags.None);
        }
        return (prev, next);
    }

    private static CellGrid ApplyIncremental(TtyCompositor comp, CellGrid prev, CellGrid next, out int scrollDy)
    {
        var plan = comp.ComputeDamagePlan(prev, next, allowScroll: true);
        scrollDy = plan.ScrollDy;
        var runs = comp.RowRuns(next, plan.Dirty);
        var bytes = new AnsiEncoder().Encode(runs, Caps, plan.ScrollDy);
        return VirtualScreenOracle.Decode(bytes.Span, (next.Width, next.Height), prev);
    }

    private static CellGrid FullRepaint(TtyCompositor comp, CellGrid next)
    {
        var dirty = new List<DirtyRect>();
        for (int y = 0; y < next.Height; y++) dirty.Add(new DirtyRect(0, y, next.Width, 1));
        var runs = comp.RowRuns(next, dirty);
        var bytes = new AnsiEncoder().Encode(runs, Caps, 0);
        return VirtualScreenOracle.Decode(bytes.Span, (next.Width, next.Height), new CellGrid(next.Width, next.Height));
    }

    private static void AssertSame(CellGrid expected, CellGrid actual)
    {
        Assert.Equal(expected.Width, actual.Width);
        Assert.Equal(expected.Height, actual.Height);
        for (int y = 0; y < expected.Height; y++)
        {
            for (int x = 0; x < expected.Width; x++)
            {
                var e = expected[x, y];
                var a = actual[x, y];
                string eg = string.IsNullOrEmpty(e.Grapheme) ? " " : e.Grapheme;
                string ag = string.IsNullOrEmpty(a.Grapheme) ? " " : a.Grapheme;
                Assert.True(eg == ag, $"grapheme@({x},{y}) expected '{eg}' got '{ag}'");
                Assert.True(Norm(e.Fg, 255) == Norm(a.Fg, 255), $"fg@({x},{y})");
                Assert.True(Norm(e.Bg, 0) == Norm(a.Bg, 0), $"bg@({x},{y})");
                Assert.True(e.Attrs == a.Attrs, $"attrs@({x},{y})");
            }
        }
    }

    private static Rgb24 Norm(Rgb24? c, byte def) => c ?? new Rgb24(def, def, def);

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Scroll_Down_Incremental_Equals_Full_Repaint(int dy)
    {
        var comp = new TtyCompositor();
        var (prev, next) = MakeShift(20, 12, dy);
        var incremental = ApplyIncremental(comp, prev, next, out int usedDy);
        Assert.Equal(dy, usedDy); // scroll optimisation was actually used
        AssertSame(next, incremental);
        AssertSame(FullRepaint(comp, next), incremental);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public void Scroll_Up_Incremental_Equals_Full_Repaint(int mag)
    {
        var comp = new TtyCompositor();
        var (prev, next) = MakeShift(20, 12, -mag);
        var incremental = ApplyIncremental(comp, prev, next, out int usedDy);
        Assert.Equal(-mag, usedDy);
        AssertSame(next, incremental);
        AssertSame(FullRepaint(comp, next), incremental);
    }

    [Fact]
    public void Delta_Larger_Than_Optimization_Window_Falls_Back_But_Stays_Correct()
    {
        var comp = new TtyCompositor();
        // Detection window is +/-5; a shift of 8 must not be reduced to a scroll.
        var (prev, next) = MakeShift(16, 20, 8);
        var incremental = ApplyIncremental(comp, prev, next, out int usedDy);
        Assert.Equal(0, usedDy);
        AssertSame(next, incremental);
        AssertSame(FullRepaint(comp, next), incremental);
    }

    [Fact]
    public void Partial_Match_In_Shifted_Region_Disables_Scroll_But_Stays_Correct()
    {
        var comp = new TtyCompositor();
        var (prev, next) = MakeShift(18, 12, 2);
        // Corrupt one row inside the would-be shifted region so the shift is no
        // longer exact. The optimisation must back off to a full per-row diff.
        for (int x = 0; x < next.Width; x++)
            next[x, 6] = new Cell("Z", 1, new Rgb24(1, 2, 3), new Rgb24(4, 5, 6), CellAttrFlags.Bold);
        var incremental = ApplyIncremental(comp, prev, next, out int usedDy);
        Assert.Equal(0, usedDy);
        AssertSame(next, incremental);
        AssertSame(FullRepaint(comp, next), incremental);
    }

    [Fact]
    public void Repeated_Identical_Rows_Do_Not_Trigger_A_Spurious_Scroll()
    {
        var comp = new TtyCompositor();
        int w = 12, h = 10;
        var prev = new CellGrid(w, h);
        var next = new CellGrid(w, h);
        var fill = new Cell("=", 1, new Rgb24(120, 120, 120), new Rgb24(0, 0, 0), CellAttrFlags.None);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                prev[x, y] = fill;
                next[x, y] = fill; // identical frames
            }
        var incremental = ApplyIncremental(comp, prev, next, out int usedDy);
        // No change => no scroll and no repaint, yet still correct.
        Assert.Equal(0, usedDy);
        AssertSame(next, incremental);
    }

    [Fact]
    public void Transparent_Backgrounds_Scroll_Correctly()
    {
        var comp = new TtyCompositor();
        int w = 14, h = 12, dy = 2;
        var prev = new CellGrid(w, h);
        var next = new CellGrid(w, h);
        // Transparent background (null Bg) throughout, distinct per-row glyphs.
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                prev[x, y] = new Cell(((char)('a' + (y % 26))).ToString(), 1, new Rgb24(200, 50, 50), null, CellAttrFlags.None);
        for (int y = 0; y < h; y++)
        {
            int src = y - dy;
            for (int x = 0; x < w; x++)
                next[x, y] = (src >= 0) ? prev[x, src] : new Cell(" ", 1, null, null, CellAttrFlags.None);
        }
        var incremental = ApplyIncremental(comp, prev, next, out int usedDy);
        Assert.Equal(dy, usedDy);
        AssertSame(next, incremental);
    }
}
