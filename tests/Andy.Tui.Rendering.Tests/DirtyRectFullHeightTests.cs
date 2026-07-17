using System;
using System.Collections.Generic;
using System.Linq;
using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Rendering.Tests;

/// <summary>
/// Regression coverage for issue #26: <see cref="TtyCompositor.RowRuns"/> must
/// honor the full height and bounds of every dirty rectangle, clip out-of-range
/// damage, and avoid duplicate output when rectangles overlap.
/// </summary>
public class DirtyRectFullHeightTests
{
    private static CellGrid MakeGrid(int width, int height, Func<int, int, string> glyph)
    {
        var g = new CellGrid(width, height);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                g[x, y] = new Cell(glyph(x, y), 1, new Rgb24(1, 1, 1), new Rgb24(0, 0, 0), CellAttrFlags.None);
        return g;
    }

    /// <summary>
    /// Reconstructs a per-cell grapheme picture of the emitted runs so tests can
    /// compare RowRuns output against the source grid directly.
    /// </summary>
    private static string?[,] Reconstruct(int width, int height, IReadOnlyList<RowRun> runs)
    {
        var picture = new string?[height, width];
        foreach (var run in runs)
        {
            int col = run.ColStart;
            var enumerator = System.Globalization.StringInfo.GetTextElementEnumerator(run.Text);
            while (enumerator.MoveNext() && col < width)
            {
                picture[run.Row, col] = (string)enumerator.Current;
                col++;
            }
        }
        return picture;
    }

    [Fact]
    public void Multi_Row_Rect_Emits_Every_Row()
    {
        var comp = new TtyCompositor();
        var g = MakeGrid(3, 4, (x, y) => ((char)('A' + y)).ToString());

        var runs = comp.RowRuns(g, new[] { new DirtyRect(0, 0, 3, 4) });

        Assert.Equal(4, runs.Count);
        Assert.Equal(new[] { 0, 1, 2, 3 }, runs.Select(r => r.Row).OrderBy(r => r).ToArray());
        Assert.Contains(runs, r => r.Row == 3 && r.Text == "DDD");
    }

    [Fact]
    public void Height_One_Emits_Exactly_One_Row()
    {
        var comp = new TtyCompositor();
        var g = MakeGrid(3, 3, (x, y) => "x");

        var runs = comp.RowRuns(g, new[] { new DirtyRect(0, 1, 3, 1) });

        Assert.Single(runs);
        Assert.Equal(1, runs[0].Row);
    }

    [Fact]
    public void Height_Zero_Emits_Nothing()
    {
        var comp = new TtyCompositor();
        var g = MakeGrid(3, 3, (x, y) => "x");

        var runs = comp.RowRuns(g, new[] { new DirtyRect(0, 1, 3, 0) });

        Assert.Empty(runs);
    }

    [Fact]
    public void Negative_And_Oversized_Damage_Is_Clipped_To_Grid()
    {
        var comp = new TtyCompositor();
        var g = MakeGrid(4, 3, (x, y) => "o");

        // Rectangle overhangs the grid on every side.
        var runs = comp.RowRuns(g, new[] { new DirtyRect(-5, -2, 100, 100) });

        // Only the 3 real rows survive, each fully within [0, width).
        Assert.Equal(3, runs.Count);
        foreach (var run in runs)
        {
            Assert.InRange(run.Row, 0, 2);
            Assert.True(run.ColStart >= 0);
            Assert.True(run.ColEnd <= g.Width);
            Assert.Equal("oooo", run.Text);
        }
    }

    [Fact]
    public void Overlapping_Rects_Emit_Each_Cell_Once()
    {
        var comp = new TtyCompositor();
        var g = MakeGrid(6, 2, (x, y) => "z");

        var rects = new[]
        {
            new DirtyRect(0, 0, 4, 2),
            new DirtyRect(2, 0, 4, 2), // overlaps columns 2..3
        };
        var runs = comp.RowRuns(g, rects);

        // No column is emitted twice: total emitted columns across all runs equals
        // the union area (6 columns * 2 rows).
        int emittedCols = runs.Sum(r => r.ColEnd - r.ColStart);
        Assert.Equal(6 * 2, emittedCols);

        // Reconstruction matches the full grid.
        var picture = Reconstruct(g.Width, g.Height, runs);
        for (int y = 0; y < g.Height; y++)
            for (int x = 0; x < g.Width; x++)
                Assert.Equal("z", picture[y, x]);
    }

    [Fact]
    public void Wide_Glyph_ColSpan_Uses_Cells_Not_Utf16_Length()
    {
        var comp = new TtyCompositor();
        // "漢字" occupies 4 cells: lead+continuation, lead+continuation.
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, 4, 1));
        b.DrawText(new TextRun(0, 0, "漢字", new Rgb24(200, 200, 200), null, CellAttrFlags.None));
        b.Pop();
        var g = comp.Composite(b.Build(), (4, 1));

        var runs = comp.RowRuns(g, new[] { new DirtyRect(0, 0, 4, 1) });

        // A single run covering all four columns; ColEnd reflects cells (4), not
        // the two-code-unit UTF-16 text length.
        Assert.Single(runs);
        Assert.Equal(0, runs[0].ColStart);
        Assert.Equal(4, runs[0].ColEnd);
        Assert.Equal("漢字", runs[0].Text);
    }

    [Fact]
    public void Full_Repaint_Reconstructs_The_Source_Grid()
    {
        var comp = new TtyCompositor();
        var rng = new Random(1234);
        var glyphs = new[] { "a", "b", " ", "#", "." };
        var g = MakeGrid(12, 8, (x, y) => glyphs[rng.Next(glyphs.Length)]);

        var runs = comp.RowRuns(g, new[] { new DirtyRect(0, 0, g.Width, g.Height) });

        var picture = Reconstruct(g.Width, g.Height, runs);
        for (int y = 0; y < g.Height; y++)
            for (int x = 0; x < g.Width; x++)
                Assert.Equal(g[x, y].Grapheme, picture[y, x]);
    }

    [Fact]
    public void Property_RowRuns_Reconstruct_Matches_Grid_For_Random_Rects()
    {
        var comp = new TtyCompositor();
        var glyphs = new[] { "A", "B", "C", " ", "*" };

        for (int seed = 0; seed < 50; seed++)
        {
            var rng = new Random(seed);
            int w = 1 + rng.Next(16);
            int h = 1 + rng.Next(12);
            var g = MakeGrid(w, h, (x, y) => glyphs[rng.Next(glyphs.Length)]);

            // Generate a set of possibly-overlapping, possibly-out-of-bounds rects
            // whose union covers the whole grid, so reconstruction must match.
            var rects = new List<DirtyRect> { new DirtyRect(0, 0, w, h) };
            int extra = rng.Next(4);
            for (int i = 0; i < extra; i++)
            {
                int rx = rng.Next(-3, w + 3);
                int ry = rng.Next(-3, h + 3);
                int rw = rng.Next(0, w + 4);
                int rh = rng.Next(0, h + 4);
                rects.Add(new DirtyRect(rx, ry, rw, rh));
            }

            var runs = comp.RowRuns(g, rects);
            var picture = Reconstruct(w, h, runs);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    Assert.Equal(g[x, y].Grapheme, picture[y, x]);

            // Overlap dedup: emitted column count never exceeds the grid area.
            int emittedCols = runs.Sum(r => r.ColEnd - r.ColStart);
            Assert.True(emittedCols <= w * h, $"seed {seed}: emitted {emittedCols} > area {w * h}");
        }
    }
}
