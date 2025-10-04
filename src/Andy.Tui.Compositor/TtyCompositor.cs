using Andy.Tui.DisplayList;

namespace Andy.Tui.Compositor;

public sealed class TtyCompositor : ICompositor
{
    public CellGrid Composite(Andy.Tui.DisplayList.DisplayList dl, (int Width, int Height) viewport)
    {
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
        foreach (var dr in dirty)
        {
            int row = dr.Y;
            int x = dr.X;
            int end = dr.X + dr.Width;
            while (x < end)
            {
                var cell = grid[x, row];
                var start = x;
                var attrs = cell.Attrs;
                var fg = cell.Fg;
                var bg = cell.Bg;
                var text = new System.Text.StringBuilder();
                while (x < end)
                {
                    var c2 = grid[x, row];
                    if (c2.Attrs != attrs || c2.Fg != fg || c2.Bg != bg) break;
                    // Use space for null graphemes to maintain correct text length
                    text.Append(c2.Grapheme ?? " ");
                    x++;
                }
                runs.Add(new RowRun(row, start, x, attrs, fg, bg, text.ToString()));
            }
        }
        return runs;
    }

    private static void DrawRect(ref CellGrid grid, (int x, int y, int w, int h) clip, Rect r)
    {
        int x0 = Math.Max(clip.x, r.X);
        int y0 = Math.Max(clip.y, r.Y);
        int x1 = Math.Min(clip.x + clip.w, r.X + r.Width);
        int y1 = Math.Min(clip.y + clip.h, r.Y + r.Height);
        for (int y = y0; y < y1; y++)
        {
            for (int x = x0; x < x1; x++)
            {
                grid[x, y] = new Cell(" ", 1, r.Fill, r.Fill, CellAttrFlags.None);
            }
        }
    }

    private static void DrawBorder(ref CellGrid grid, (int x, int y, int w, int h) clip, Border b)
    {
        int x0 = Math.Max(clip.x, b.X);
        int y0 = Math.Max(clip.y, b.Y);
        int x1 = Math.Min(clip.x + clip.w, b.X + b.Width);
        int y1 = Math.Min(clip.y + clip.h, b.Y + b.Height);
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
        int x = Math.Max(clip.x, t.X);
        int y = t.Y;
        if (y < clip.y || y >= clip.y + clip.h) return;
        var start = x - t.X;
        if (start < 0) start = 0;
        for (int i = start; i < t.Content.Length && x < clip.x + clip.w; i++)
        {
            var ch = t.Content[i];
            int width = GetCharDisplayWidth(ch);
            // Edge policy: if double-width at last column, replace with placeholder
            if (width == 2 && x == clip.x + clip.w - 1)
            {
                var bgEdge = t.Bg ?? grid[x, y].Bg;
                grid[x, y] = new Cell("?", 1, t.Fg, bgEdge, t.Attrs);
                x += 1;
                continue;
            }

            var bg = t.Bg ?? grid[x, y].Bg;
            grid[x, y] = new Cell(ch.ToString(), (byte)Math.Min(width, 1), t.Fg, bg, t.Attrs);
            x += 1;
            // Note: not filling trailing cell for width=2 in MVP
        }
    }

    private static int GetCharDisplayWidth(char ch)
    {
        int codePoint = ch;
        // Basic BMP approximation for wide chars; surrogate pairs not handled in MVP
        return IsWideCodePoint(codePoint) ? 2 : 1;
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
            (codePoint >= 0x1F900 && codePoint <= 0x1F9FF) ||
            (codePoint >= 0x20000 && codePoint <= 0x3FFFD);
    }
}