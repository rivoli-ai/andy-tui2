using System;
using System.Collections.Generic;
using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;
using Xunit;

namespace Andy.Tui.Rendering.Tests;

/// <summary>
/// Regression tests for issue #21: harden compositor drawing for empty, clipped,
/// and invalid geometry. No geometry input may cause an out-of-range cell access.
/// </summary>
public class CompositorGeometryHardeningTests
{
    private static readonly Rgb24 Ink = new Rgb24(200, 200, 200);
    private static readonly Rgb24 Fill = new Rgb24(10, 20, 30);

    private static CellGrid Composite(Action<DisplayListBuilder> build, (int W, int H) viewport)
    {
        var b = new DisplayListBuilder();
        build(b);
        return new TtyCompositor().Composite(b.Build(), viewport);
    }

    // ---- Borders fully off-screen in each of the four directions ----

    [Theory]
    [InlineData(-10, 1)]   // fully left
    [InlineData(50, 1)]    // fully right
    [InlineData(1, -10)]   // fully above
    [InlineData(1, 50)]    // fully below
    public void Border_FullyOffscreen_DoesNotThrow_And_DrawsNothing(int bx, int by)
    {
        var g = Composite(b =>
        {
            b.PushClip(new ClipPush(0, 0, 10, 5));
            b.DrawBorder(new Border(bx, by, 4, 3, "single", Ink));
            b.Pop();
        }, (10, 5));

        // Nothing was painted anywhere inside the grid.
        for (int y = 0; y < g.Height; y++)
            for (int x = 0; x < g.Width; x++)
                Assert.Null(g[x, y].Grapheme);
    }

    [Fact]
    public void Border_FullyOffscreen_RightEdge_ExactlyAtWidth_DoesNotThrow()
    {
        // X == viewport width: x0 would be at the grid's right edge (out of range)
        // for the unconditional corner writes prior to the fix.
        var ex = Record.Exception(() => Composite(b =>
        {
            b.PushClip(new ClipPush(0, 0, 10, 5));
            b.DrawBorder(new Border(10, 0, 4, 3, "single", Ink));
            b.Pop();
        }, (10, 5)));
        Assert.Null(ex);
    }

    // ---- Zero and negative sizes ----

    [Theory]
    [InlineData(0, 3)]    // zero width
    [InlineData(4, 0)]    // zero height
    [InlineData(0, 0)]    // zero both
    [InlineData(-4, 3)]   // negative width
    [InlineData(4, -3)]   // negative height
    [InlineData(-4, -3)]  // negative both
    public void Border_ZeroOrNegativeSize_DoesNotThrow_And_DrawsNothing(int w, int h)
    {
        var g = Composite(b =>
        {
            b.PushClip(new ClipPush(0, 0, 10, 5));
            b.DrawBorder(new Border(2, 1, w, h, "single", Ink));
            b.Pop();
        }, (10, 5));

        for (int y = 0; y < g.Height; y++)
            for (int x = 0; x < g.Width; x++)
                Assert.Null(g[x, y].Grapheme);
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(4, 0)]
    [InlineData(-4, 3)]
    [InlineData(4, -3)]
    public void Rect_ZeroOrNegativeSize_DoesNotThrow_And_DrawsNothing(int w, int h)
    {
        var g = Composite(b =>
        {
            b.PushClip(new ClipPush(0, 0, 10, 5));
            b.DrawRect(new Rect(2, 1, w, h, Fill));
            b.Pop();
        }, (10, 5));

        for (int y = 0; y < g.Height; y++)
            for (int x = 0; x < g.Width; x++)
                Assert.Null(g[x, y].Grapheme);
    }

    [Theory]
    [InlineData(-100, 1)]
    [InlineData(100, 1)]
    [InlineData(1, -100)]
    [InlineData(1, 100)]
    public void Rect_FullyOffscreen_DoesNotThrow_And_DrawsNothing(int rx, int ry)
    {
        var g = Composite(b =>
        {
            b.PushClip(new ClipPush(0, 0, 10, 5));
            b.DrawRect(new Rect(rx, ry, 4, 3, Fill));
            b.Pop();
        }, (10, 5));

        for (int y = 0; y < g.Height; y++)
            for (int x = 0; x < g.Width; x++)
                Assert.Null(g[x, y].Grapheme);
    }

    // ---- Text fully off-screen in each direction ----

    [Theory]
    [InlineData(-20, 2)]  // left
    [InlineData(40, 2)]   // right
    [InlineData(2, -5)]   // above
    [InlineData(2, 20)]   // below
    public void Text_FullyOffscreen_DoesNotThrow(int tx, int ty)
    {
        var ex = Record.Exception(() => Composite(b =>
        {
            b.PushClip(new ClipPush(0, 0, 10, 5));
            b.DrawText(new TextRun(tx, ty, "hello world", Ink, null, CellAttrFlags.None));
            b.Pop();
        }, (10, 5)));
        Assert.Null(ex);
    }

    // ---- Nested empty clips ----

    [Fact]
    public void NestedEmptyClip_DrawsNothing_And_DoesNotThrow()
    {
        var g = Composite(b =>
        {
            b.PushClip(new ClipPush(0, 0, 10, 5));
            // A second clip with no intersection collapses the clip region to empty.
            b.PushClip(new ClipPush(20, 20, 3, 3));
            b.DrawBorder(new Border(20, 20, 3, 3, "single", Ink));
            b.DrawRect(new Rect(20, 20, 3, 3, Fill));
            b.DrawText(new TextRun(20, 20, "xyz", Ink, null, CellAttrFlags.None));
            b.Pop();
            b.Pop();
        }, (10, 5));

        for (int y = 0; y < g.Height; y++)
            for (int x = 0; x < g.Width; x++)
                Assert.Null(g[x, y].Grapheme);
    }

    // ---- One-cell borders ----

    [Fact]
    public void OneCellBorder_DoesNotThrow_And_PaintsSingleCorner()
    {
        var g = Composite(b =>
        {
            b.PushClip(new ClipPush(0, 0, 10, 5));
            b.DrawBorder(new Border(3, 2, 1, 1, "single", Ink));
            b.Pop();
        }, (10, 5));

        // A 1x1 border collapses to a single cell; all four corner writes land on
        // the same cell, so the last write ("┘") wins. The important guarantee is
        // that exactly one cell is painted and nothing throws.
        Assert.NotNull(g[3, 2].Grapheme);
        int painted = 0;
        for (int y = 0; y < g.Height; y++)
            for (int x = 0; x < g.Width; x++)
                if (g[x, y].Grapheme is not null) painted++;
        Assert.Equal(1, painted);
    }

    [Fact]
    public void OneCellWideBorder_DoesNotThrow()
    {
        // 1-cell-wide, multi-row border: left and right vertical edges coincide.
        var ex = Record.Exception(() => Composite(b =>
        {
            b.PushClip(new ClipPush(0, 0, 10, 5));
            b.DrawBorder(new Border(2, 1, 1, 3, "single", Ink));
            b.Pop();
        }, (10, 5)));
        Assert.Null(ex);
    }

    [Fact]
    public void Border_ClippedToOneCellBySmallClip_DoesNotThrow()
    {
        // Border is large but the clip shrinks its visible region to a single cell.
        var ex = Record.Exception(() => Composite(b =>
        {
            b.PushClip(new ClipPush(4, 2, 1, 1));
            b.DrawBorder(new Border(0, 0, 10, 5, "single", Ink));
            b.Pop();
        }, (10, 5)));
        Assert.Null(ex);
    }

    // ---- Viewport validation at the public boundary ----

    [Theory]
    [InlineData(0, 5)]
    [InlineData(5, 0)]
    [InlineData(-1, 5)]
    [InlineData(5, -1)]
    public void Composite_NonPositiveViewport_Throws(int w, int h)
    {
        var b = new DisplayListBuilder();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TtyCompositor().Composite(b.Build(), (w, h)));
    }

    // ---- Dirty region hardening: RowRuns must clamp to grid bounds ----

    [Fact]
    public void RowRuns_OutOfBoundsDirtyRect_DoesNotThrow_And_IsClamped()
    {
        var g = Composite(b =>
        {
            b.PushClip(new ClipPush(0, 0, 10, 5));
            b.DrawRect(new Rect(0, 0, 10, 5, Fill));
            b.Pop();
        }, (10, 5));

        var compositor = new TtyCompositor();
        var dirty = new List<DirtyRect>
        {
            new DirtyRect(-5, 2, 100, 1),   // starts left of grid, extends past right
            new DirtyRect(0, 99, 10, 1),    // row below the grid
            new DirtyRect(0, -3, 10, 1),    // row above the grid
        };

        IReadOnlyList<RowRun> runs = null!;
        var ex = Record.Exception(() => runs = compositor.RowRuns(g, dirty));
        Assert.Null(ex);
        // Every produced run stays inside the grid.
        foreach (var r in runs)
        {
            Assert.InRange(r.Row, 0, g.Height - 1);
            Assert.True(r.ColStart >= 0);
            Assert.True(r.ColEnd <= g.Width);
        }
    }

    // ---- DisplayList invariant validation and compositor behavior agree ----

    [Fact]
    public void InvariantValidation_Rejects_NestedEmptyClip_That_Compositor_Renders_Safely()
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, 10, 5));
        b.PushClip(new ClipPush(20, 20, 3, 3)); // no intersection
        b.DrawBorder(new Border(20, 20, 3, 3, "single", Ink));
        b.Pop();
        b.Pop();
        var dl = b.Build();

        // The invariant validator rejects the empty nested clip up front...
        Assert.Throws<DisplayListInvariantViolationException>(() => DisplayListInvariants.Validate(dl));

        // ...and the compositor, given the very same list, renders it safely with
        // no out-of-range access and no output. The two boundaries agree: neither
        // corrupts state on degenerate geometry.
        var g = new TtyCompositor().Composite(dl, (10, 5));
        for (int y = 0; y < g.Height; y++)
            for (int x = 0; x < g.Width; x++)
                Assert.Null(g[x, y].Grapheme);
    }
}
