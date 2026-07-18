using Andy.Tui.DisplayList;

namespace Andy.Tui.Compositor;

public sealed class TtyCompositor : ICompositor
{
    public CellGrid Composite(Andy.Tui.DisplayList.DisplayList dl, (int Width, int Height) viewport)
    {
        // Validate viewport dimensions at the public boundary. A non-positive
        // viewport has no representable cells, so reject it deterministically
        // instead of letting an out-of-range allocation surface deeper down.
        if (viewport.Width <= 0)
            throw new ArgumentOutOfRangeException(nameof(viewport), viewport.Width, "Viewport width must be positive.");
        if (viewport.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(viewport), viewport.Height, "Viewport height must be positive.");

        var grid = new CellGrid(viewport.Width, viewport.Height);
        var clipStack = new Stack<(int x, int y, int w, int h)>();
        clipStack.Push((0, 0, viewport.Width, viewport.Height));

        foreach (var op in dl.Ops)
        {
            switch (op)
            {
                case ClipPush cp:
                    var top = clipStack.Peek();
                    var x1 = Math.Max(top.x, cp.X);
                    var y1 = Math.Max(top.y, cp.Y);
                    var x2 = Math.Min(top.x + top.w, cp.X + cp.Width);
                    var y2 = Math.Min(top.y + top.h, cp.Y + cp.Height);
                    var w = Math.Max(0, x2 - x1);
                    var h = Math.Max(0, y2 - y1);
                    clipStack.Push((x1, y1, w, h));
                    break;
                case LayerPush:
                    // For TTY MVP, treat like a grouping scope only
                    clipStack.Push(clipStack.Peek());
                    break;
                case Pop:
                    if (clipStack.Count > 1) clipStack.Pop();
                    break;
                case Rect r:
                    DrawRect(ref grid, clipStack.Peek(), r);
                    break;
                case Border b:
                    DrawBorder(ref grid, clipStack.Peek(), b);
                    break;
                case TextRun t:
                    DrawText(ref grid, clipStack.Peek(), t);
                    break;
            }
        }

        return grid;
    }

    public IReadOnlyList<DirtyRect> Damage(CellGrid previous, CellGrid next)
    {
        // If viewport size changed, repaint entire next frame to avoid stale edges
        if (previous.Width != next.Width || previous.Height != next.Height)
        {
            return new List<DirtyRect> { new DirtyRect(0, 0, next.Width, next.Height) };
        }
        // Fallback: compute per-row dirty runs
        var fallback = new List<DirtyRect>();
        int w = Math.Min(previous.Width, next.Width);
        int h = Math.Min(previous.Height, next.Height);
        for (int y = 0; y < h; y++)
        {
            int runStart = -1;
            for (int x = 0; x < w; x++)
            {
                if (!previous[x, y].Equals(next[x, y]))
                {
                    if (runStart == -1) runStart = x;
                }
                else if (runStart != -1)
                {
                    fallback.Add(new DirtyRect(runStart, y, (x - runStart), 1));
                    runStart = -1;
                }
            }
            if (runStart != -1)
                fallback.Add(new DirtyRect(runStart, y, (w - runStart), 1));
        }

        // Try scroll detection and prefer it only if cheaper than fallback
        if (TryDetectVerticalScroll(previous, next, out int dy))
        {
            var scroll = new List<DirtyRect>();
            if (dy > 0)
            {
                scroll.Add(new DirtyRect(0, 0, next.Width, Math.Min(dy, next.Height)));
            }
            else if (dy < 0)
            {
                int hh = Math.Min(-dy, next.Height);
                scroll.Add(new DirtyRect(0, next.Height - hh, next.Width, hh));
            }
            int scrollArea = 0; foreach (var r in scroll) scrollArea += r.Width * r.Height;
            int fallbackArea = 0; foreach (var r in fallback) fallbackArea += r.Width * r.Height;
            if (scrollArea <= fallbackArea)
                return scroll;
        }
        return fallback;
    }

    internal static bool TryDetectVerticalScroll(CellGrid previous, CellGrid next, out int dy)
    {
        dy = 0;
        if (previous.Width != next.Width || previous.Height != next.Height)
            return false;

        int h = previous.Height;
        if (h < 3) return false; // avoid false positives on tiny viewports
        int w = previous.Width;

        int bestCandidate = 0;
        int bestMatches = -1;
        int bestRowsCompared = 0;

        // Try small range of plausible scroll deltas
        for (int candidate = -Math.Min(5, h - 1); candidate <= Math.Min(5, h - 1); candidate++)
        {
            if (candidate == 0) continue;
            int matches = 0;
            int rowsCompared = 0;
            for (int y = 0; y < h; y++)
            {
                int py = y - candidate;
                if (py < 0 || py >= h) continue;
                rowsCompared++;
                bool rowEqual = true;
                for (int x = 0; x < w; x++)
                {
                    if (!previous[x, py].Equals(next[x, y]))
                    {
                        rowEqual = false;
                        break;
                    }
                }
                if (rowEqual) matches++;
            }
            if (matches > bestMatches || (matches == bestMatches && Math.Abs(candidate) < Math.Abs(bestCandidate)))
            {
                bestMatches = matches;
                bestCandidate = candidate;
                bestRowsCompared = rowsCompared;
            }
        }

        if (bestMatches <= 0) return false;
        // Expected comparable rows for a given dy is h - |dy|
        int expectedComparable = h - Math.Abs(bestCandidate);
        // Require near-perfect match across comparable rows (allow at most 1 mismatch)
        if (bestRowsCompared == expectedComparable && bestMatches >= Math.Max(1, expectedComparable - 1))
        {
            dy = bestCandidate;
            return true;
        }
        return false;
    }

    public IReadOnlyList<RowRun> RowRuns(CellGrid grid, IReadOnlyList<DirtyRect> dirty)
    {
        var runs = new List<RowRun>();
        // Every dirty rectangle must contribute all of its clipped rows, so track
        // which cells have already been emitted. Overlapping rectangles then
        // produce each cell exactly once, keeping output correct and metrics
        // (bytes/run counts) from being inflated by duplicates.
        var emitted = new bool[grid.Width * grid.Height];

        foreach (var dr in dirty)
        {
            // Clip the rectangle to the grid so negative offsets or oversized
            // damage never read out of bounds. Height is honored here: a
            // multi-row rectangle iterates every clipped row, and a zero-height
            // (or otherwise empty) rectangle contributes nothing.
            int rowStart = Math.Max(0, dr.Y);
            int rowEnd = Math.Min(grid.Height, dr.Y + dr.Height);
            int colStart = Math.Max(0, dr.X);
            int colEnd = Math.Min(grid.Width, dr.X + dr.Width);
            if (colStart >= colEnd) continue;

            for (int row = rowStart; row < rowEnd; row++)
            {
                int rowBase = row * grid.Width;
                int x = colStart;
                while (x < colEnd)
                {
                    // Skip any cell an earlier overlapping rectangle already emitted.
                    if (emitted[rowBase + x]) { x++; continue; }

                    var cell = grid[x, row];
                    int start = x;
                    var attrs = cell.Attrs;
                    var fg = cell.Fg;
                    var bg = cell.Bg;
                    var text = new System.Text.StringBuilder();

                    // A run extends while attributes/colors match and no cell has
                    // already been emitted. Each grid cell advances the column by
                    // one, so ColStart/ColEnd stay in terminal-cell units even when
                    // a wide glyph carries a multi-unit grapheme or a cell holds
                    // combining marks (which never widen the column span).
                    while (x < colEnd && !emitted[rowBase + x])
                    {
                        var c2 = grid[x, row];
                        if (c2.Attrs != attrs || c2.Fg != fg || c2.Bg != bg) break;
                        // Use a space for null (untouched/transparent) graphemes so a
                        // full repaint clears the cell. Wide-glyph continuation cells
                        // carry an empty grapheme and contribute no text while still
                        // consuming their column.
                        text.Append(c2.Grapheme ?? " ");
                        emitted[rowBase + x] = true;
                        x++;
                    }

                    var runText = text.ToString();
                    if (runText.Length > 0)
                    {
                        // ColEnd is the exclusive terminal column after the run,
                        // derived from cell columns rather than UTF-16 length.
                        runs.Add(new RowRun(row, start, x, attrs, fg, bg, runText));
                    }
                }
            }
        }
        return runs;
    }

    // Intersect a clip rectangle with the drawing op's rectangle AND the grid
    // bounds, producing a half-open range [x0,x1) x [y0,y1). Returns false when
    // the result is empty so callers can bail out before touching any cell.
    // Negative positions/dimensions collapse to an empty range here rather than
    // producing negative or out-of-range indices downstream.
    private static bool ClipToGrid(
        CellGrid grid, (int x, int y, int w, int h) clip,
        int rx, int ry, int rw, int rh,
        out int x0, out int y0, out int x1, out int y1)
    {
        x0 = Math.Max(0, Math.Max(clip.x, rx));
        y0 = Math.Max(0, Math.Max(clip.y, ry));
        x1 = Math.Min(grid.Width, Math.Min(clip.x + clip.w, rx + rw));
        y1 = Math.Min(grid.Height, Math.Min(clip.y + clip.h, ry + rh));
        return x1 > x0 && y1 > y0;
    }

    private static void DrawRect(ref CellGrid grid, (int x, int y, int w, int h) clip, Rect r)
    {
        // A transparent fill (null) paints nothing: it leaves the cells underneath
        // (ultimately the terminal's default background) visible.
        if (r.Fill is null) return;
        if (!ClipToGrid(grid, clip, r.X, r.Y, r.Width, r.Height, out int x0, out int y0, out int x1, out int y1))
            return;
        var fill = r.Fill.Value;
        for (int y = y0; y < y1; y++)
        {
            for (int x = x0; x < x1; x++)
            {
                // Use explicit space with both fg and bg set to fill color
                // This ensures the cell is properly cleared
                grid[x, y] = new Cell(" ", 1, fill, fill, CellAttrFlags.None);
            }
        }
    }

    private static void DrawBorder(ref CellGrid grid, (int x, int y, int w, int h) clip, Border b)
    {
        // Clip the border to the current clip AND the grid. When the border is
        // empty, fully off-screen, or has non-positive size the intersection is
        // empty and we return before writing any edge or corner cell — this is
        // what prevents the unconditional corner writes below from indexing a
        // negative or out-of-range cell.
        if (!ClipToGrid(grid, clip, b.X, b.Y, b.Width, b.Height, out int x0, out int y0, out int x1, out int y1))
            return;
        for (int x = x0; x < x1; x++)
        {
            grid[x, y0] = new Cell("─", 1, b.Color, grid[x, y0].Bg, CellAttrFlags.None);
            grid[x, y1 - 1] = new Cell("─", 1, b.Color, grid[x, y1 - 1].Bg, CellAttrFlags.None);
        }
        for (int y = y0; y < y1; y++)
        {
            grid[x0, y] = new Cell("│", 1, b.Color, grid[x0, y].Bg, CellAttrFlags.None);
            grid[x1 - 1, y] = new Cell("│", 1, b.Color, grid[x1 - 1, y].Bg, CellAttrFlags.None);
        }
        // corners
        grid[x0, y0] = new Cell("┌", 1, b.Color, grid[x0, y0].Bg, CellAttrFlags.None);
        grid[x1 - 1, y0] = new Cell("┐", 1, b.Color, grid[x1 - 1, y0].Bg, CellAttrFlags.None);
        grid[x0, y1 - 1] = new Cell("└", 1, b.Color, grid[x0, y1 - 1].Bg, CellAttrFlags.None);
        grid[x1 - 1, y1 - 1] = new Cell("┘", 1, b.Color, grid[x1 - 1, y1 - 1].Bg, CellAttrFlags.None);
    }

    private static void DrawText(ref CellGrid grid, (int x, int y, int w, int h) clip, TextRun t)
    {
        int y = t.Y;
        if (y < clip.y || y >= clip.y + clip.h) return;
        // The clip rectangle may extend beyond the grid (an off-screen or degenerate
        // clip whose x/width were never clamped to the grid). Clamp the horizontal
        // clip bounds to the grid so no draw branch - including the wide-glyph edge
        // placeholder at clipRight-1 - can index outside the backing cell array.
        if (y < 0 || y >= grid.Height) return;
        int clipLeft = Math.Max(0, clip.x);
        int clipRight = Math.Min(grid.Width, clip.x + clip.w);
        if (clipRight <= clipLeft) return;

        string s = t.Content;
        int x = t.X;
        int lastLeadX = int.MinValue; // column of the last lead cell written on this row
        int i = 0;
        while (i < s.Length)
        {
            // Decode one Unicode scalar, combining a surrogate pair into one grapheme.
            char c0 = s[i];
            int codePoint;
            string grapheme;
            if (char.IsHighSurrogate(c0) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]))
            {
                codePoint = char.ConvertToUtf32(c0, s[i + 1]);
                grapheme = s.Substring(i, 2);
                i += 2;
            }
            else
            {
                codePoint = c0;
                grapheme = c0.ToString();
                i += 1;
            }

            // Zero-width combiners (variation selectors, ZWJ, combining marks) attach to
            // the preceding glyph rather than consuming a column, so emoji presentation
            // sequences (e.g. "\U0001F5A5️") and ZWJ sequences render as one cell.
            if (IsZeroWidth(codePoint))
            {
                if (lastLeadX >= clipLeft && lastLeadX < clipRight)
                {
                    ref var lead = ref grid.GetRef(lastLeadX, y);
                    lead = lead with { Grapheme = (lead.Grapheme ?? "") + grapheme };
                }
                continue;
            }

            int width = IsWideCodePoint(codePoint) ? 2 : 1;

            // Glyph wholly left of the clip: advance without drawing.
            if (x + width <= clipLeft) { x += width; continue; }
            if (x >= clipRight) break;

            // Edge policy: a double-width glyph that would overflow the right edge is
            // replaced with a single-cell placeholder.
            if (width == 2 && x == clipRight - 1)
            {
                var bgEdge = t.Bg ?? grid[x, y].Bg;
                grid[x, y] = new Cell("?", 1, t.Fg, bgEdge, t.Attrs);
                lastLeadX = x;
                x += 1;
                continue;
            }

            // A wide glyph straddling the left clip edge is dropped (no half glyph).
            if (x >= clipLeft)
            {
                var bg = t.Bg ?? grid[x, y].Bg;
                grid[x, y] = new Cell(grapheme, (byte)width, t.Fg, bg, t.Attrs);
                lastLeadX = x;
                if (width == 2 && x + 1 < clipRight)
                {
                    // Continuation cell: an empty grapheme emits nothing, so the wide
                    // glyph owns both columns. Share colors with the lead cell.
                    var bg2 = t.Bg ?? grid[x + 1, y].Bg;
                    grid[x + 1, y] = new Cell("", 0, t.Fg, bg2, t.Attrs);
                }
            }
            x += width;
        }
    }

    private static bool IsZeroWidth(int codePoint)
    {
        return
            codePoint == 0x200D || codePoint == 0x200C ||      // ZWJ / ZWNJ
            (codePoint >= 0xFE00 && codePoint <= 0xFE0F) ||    // variation selectors
            (codePoint >= 0x0300 && codePoint <= 0x036F) ||    // combining diacritical marks
            (codePoint >= 0x1AB0 && codePoint <= 0x1AFF) ||
            (codePoint >= 0x1DC0 && codePoint <= 0x1DFF) ||
            (codePoint >= 0x20D0 && codePoint <= 0x20FF) ||
            (codePoint >= 0xFE20 && codePoint <= 0xFE2F) ||
            (codePoint >= 0xE0100 && codePoint <= 0xE01EF);    // variation selectors supplement
    }

    private static bool IsWideCodePoint(int codePoint)
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
            (codePoint >= 0x1F680 && codePoint <= 0x1F6FF) || // transport & map symbols
            (codePoint >= 0x1F900 && codePoint <= 0x1F9FF) ||
            (codePoint >= 0x1FA00 && codePoint <= 0x1FAFF) || // symbols & pictographs extended-A
            (codePoint >= 0x20000 && codePoint <= 0x3FFFD);
    }
}