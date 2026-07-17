using System;
using System.Collections.Generic;
using System.Linq;
using Andy.Tui.Text;
using Andy.Tui.Widgets;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;
using Xunit;

namespace Andy.Tui.Widgets.Tests;

public class VirtualizedGridHorizontalTests
{
    private static readonly TextMeasurer Measurer = new();

    private static VirtualizedGrid MakeGrid(int rows, int cols, int[]? widths = null)
    {
        var g = new VirtualizedGrid();
        g.SetDimensions(rows, cols);
        if (widths is not null) g.SetColumnWidths(widths);
        g.SetCellTextProvider((r, c) => $"r{r}c{c}");
        return g;
    }

    // Returns the number of visible columns starting at the current scroll position for a viewport width.
    private static int VisibleCols(VirtualizedGrid g, int width)
    {
        var (_, visCols) = g.MeasureVisible(new L.Rect(0, 0, width, 4));
        return visCols;
    }

    private static bool ActiveVisible(VirtualizedGrid g, int width)
    {
        int first = g.GetFirstVisibleColumn();
        int vis = VisibleCols(g, width);
        int active = g.GetActiveCell().Col;
        return active >= first && active < first + vis;
    }

    [Fact]
    public void EnsureVisibleCols_KeepsActiveVisible_UniformWidths()
    {
        var g = MakeGrid(3, 20, Enumerable.Repeat(8, 20).ToArray());
        const int viewport = 40;
        for (int target = 0; target < 20; target++)
        {
            g.SetActiveCell(0, target);
            g.EnsureVisibleCols(viewport);
            Assert.True(ActiveVisible(g, viewport), $"active {target} not visible; first={g.GetFirstVisibleColumn()} vis={VisibleCols(g, viewport)}");
        }
    }

    [Fact]
    public void EnsureVisibleCols_Property_ActiveAlwaysVisible_RandomizedWidthsAndMoves()
    {
        var rng = new Random(1234);
        for (int trial = 0; trial < 200; trial++)
        {
            int cols = rng.Next(1, 25);
            var widths = new int[cols];
            for (int c = 0; c < cols; c++) widths[c] = rng.Next(1, 15);
            var g = MakeGrid(2, cols, widths);
            int viewport = rng.Next(1, 60);

            // Perform a sequence of random horizontal moves, ensuring visibility each time.
            for (int step = 0; step < 15; step++)
            {
                int target = rng.Next(0, cols);
                g.SetActiveCell(0, target);
                g.EnsureVisibleCols(viewport);
                Assert.True(ActiveVisible(g, viewport),
                    $"trial={trial} cols={cols} viewport={viewport} target={target} first={g.GetFirstVisibleColumn()} vis={VisibleCols(g, viewport)}");
            }
        }
    }

    [Fact]
    public void EnsureVisibleCols_ScrollsLeftWhenActiveBeforeWindow()
    {
        var g = MakeGrid(2, 10, Enumerable.Repeat(6, 10).ToArray());
        g.SetActiveCell(0, 9);
        g.EnsureVisibleCols(20);
        Assert.True(g.GetFirstVisibleColumn() > 0);

        g.SetActiveCell(0, 0);
        g.EnsureVisibleCols(20);
        Assert.Equal(0, g.GetFirstVisibleColumn());
        Assert.True(ActiveVisible(g, 20));
    }

    [Fact]
    public void ColumnWiderThanViewport_IsStillShown_AndActiveVisible()
    {
        // Single column much wider than the viewport.
        var g = MakeGrid(1, 3, new[] { 5, 40, 5 });
        g.SetActiveCell(0, 1);
        g.EnsureVisibleCols(10);
        Assert.Equal(1, g.GetFirstVisibleColumn());
        Assert.True(ActiveVisible(g, 10));
        Assert.Equal(1, VisibleCols(g, 10)); // the oversized column occupies the whole viewport
    }

    [Fact]
    public void MeasureAndRender_ChooseIdenticalColumns()
    {
        var g = MakeGrid(5, 12, new[] { 3, 4, 5, 2, 6, 3, 4, 8, 2, 5, 3, 4 });
        g.SetActiveCell(0, 7);
        g.EnsureVisibleCols(22);
        var rect = new L.Rect(0, 0, 22, 5);
        var (_, visCols) = g.MeasureVisible(rect);

        var ops = RenderOps(g, rect);
        // Each visible column draws exactly one text run in the first rendered row,
        // so the render's column count must match the measurement's column count.
        int renderedColsInRow0 = ops.OfType<DL.TextRun>().Count(t => t.Y == 0);
        Assert.Equal(visCols, renderedColsInRow0);
    }

    [Fact]
    public void Render_DoesNotOverflowViewport_NarrowViewport()
    {
        var g = MakeGrid(3, 8, Enumerable.Repeat(6, 8).ToArray());
        var rect = new L.Rect(2, 1, 9, 3);
        var ops = RenderOps(g, rect);
        AssertNoHorizontalOverflow(ops, rect);
    }

    [Fact]
    public void Render_CjkEmojiCombining_DoNotOverflow()
    {
        var g = new VirtualizedGrid();
        g.SetDimensions(4, 4);
        g.SetColumnWidths(new[] { 4, 4, 4, 4 });
        string[] samples =
        {
            "你好世界", // CJK (each 2 cells)
            "áb́ć",       // combining accents
            "\U0001F600\U0001F680",        // emoji (wide)
            "mixed世",                  // ascii + wide
        };
        g.SetCellTextProvider((r, c) => samples[c % samples.Length]);
        var rect = new L.Rect(0, 0, 20, 4);
        var ops = RenderOps(g, rect);
        AssertNoHorizontalOverflow(ops, rect);
        // Every rendered cell fits within its allotted column width in terminal cells.
        foreach (var t in ops.OfType<DL.TextRun>())
        {
            Assert.True(Measurer.MeasureWidth(t.Content) <= rect.Width);
        }
    }

    [Fact]
    public void EmptyGrid_IsSafe()
    {
        var g = new VirtualizedGrid();
        g.SetDimensions(0, 0);
        g.EnsureVisibleCols(20);
        g.SetActiveCell(3, 3);
        Assert.Equal((0, 0), g.GetActiveCell());
        var rect = new L.Rect(0, 0, 10, 4);
        var (visRows, visCols) = g.MeasureVisible(rect);
        Assert.Equal(0, visRows);
        Assert.Equal(0, visCols);
        // Render must not throw.
        _ = RenderOps(g, rect);
    }

    [Fact]
    public void DimensionShrink_ClampsActiveAndScroll()
    {
        var g = MakeGrid(10, 20, Enumerable.Repeat(6, 20).ToArray());
        g.SetActiveCell(9, 19);
        g.EnsureVisibleCols(30);
        g.SetScrollRows(9);
        Assert.True(g.GetFirstVisibleColumn() > 0);

        // Shrink dramatically; active + scroll must be clamped to valid ranges.
        g.SetDimensions(2, 3);
        var (row, col) = g.GetActiveCell();
        Assert.InRange(row, 0, 1);
        Assert.InRange(col, 0, 2);
        Assert.InRange(g.GetFirstVisibleColumn(), 0, 2);
        Assert.InRange(g.GetFirstVisibleRow(), 0, 1);

        // After a column-width change that shrinks the column set, scroll clamps too.
        g.SetColumnWidths(new[] { 4 });
        Assert.InRange(g.GetActiveCell().Col, 0, 0);
        Assert.Equal(0, g.GetFirstVisibleColumn());
    }

    [Fact]
    public void MoveActiveCell_WithWidth_EnsuresHorizontalVisibility()
    {
        var g = MakeGrid(3, 15, Enumerable.Repeat(7, 15).ToArray());
        for (int i = 0; i < 14; i++)
        {
            g.MoveActiveCell(0, 1, 3, 25);
            Assert.True(ActiveVisible(g, 25));
        }
    }

    // ---- helpers ----

    private static IReadOnlyList<DL.IDisplayOp> RenderOps(VirtualizedGrid g, L.Rect rect)
    {
        var baseDl = new DL.DisplayListBuilder().Build();
        var builder = new DL.DisplayListBuilder();
        g.Render(rect, baseDl, builder);
        return builder.Build().Ops;
    }

    private static void AssertNoHorizontalOverflow(IReadOnlyList<DL.IDisplayOp> ops, L.Rect rect)
    {
        double left = rect.X;
        double right = rect.X + rect.Width;
        foreach (var op in ops)
        {
            if (op is DL.TextRun t)
            {
                int w = Measurer.MeasureWidth(t.Content);
                Assert.True(t.X >= left, $"text starts before viewport: x={t.X} left={left}");
                Assert.True(t.X + w <= right, $"text overflows viewport: x={t.X} w={w} right={right} content='{t.Content}'");
            }
            else if (op is DL.Rect r && r.Fill is not null)
            {
                // The highlight rect for the active cell must also stay within the viewport.
                Assert.True(r.X >= left && r.X + r.Width <= right,
                    $"rect overflows viewport: x={r.X} w={r.Width} right={right}");
            }
        }
    }
}
