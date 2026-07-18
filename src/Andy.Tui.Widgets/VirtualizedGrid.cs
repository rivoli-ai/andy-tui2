using System;
using System.Linq;
using System.Text;
using Andy.Tui.Text;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets;

public sealed class VirtualizedGrid : WidgetBase
{
    // Width of the single-cell separator drawn between adjacent visible columns.
    private const int SeparatorWidth = 1;

    private int _rowCount;
    private int _colCount;
    private int[] _columnWidths = Array.Empty<int>();
    private Func<int, int, string>? _cellTextProvider;

    private int _scrollYRows;
    private int _scrollXCols;
    private int _activeRow;
    private int _activeCol;

    private readonly TextMeasurer _measurer = new();

    public void SetDimensions(int rows, int cols)
    {
        _rowCount = Math.Max(0, rows);
        _colCount = Math.Max(0, cols);
        if (_columnWidths.Length != _colCount)
        {
            _columnWidths = Enumerable.Repeat(8, _colCount).ToArray();
        }
        ClampState();
    }

    public void SetColumnWidths(int[] widths)
    {
        if (widths is null) throw new ArgumentNullException(nameof(widths));
        _columnWidths = widths.ToArray();
        _colCount = widths.Length;
        ClampState();
    }

    public void SetCellTextProvider(Func<int, int, string> provider)
    {
        _cellTextProvider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public void SetScrollRows(int firstVisibleRow) => _scrollYRows = Math.Max(0, Math.Min(firstVisibleRow, Math.Max(0, _rowCount - 1)));

    public int GetFirstVisibleRow() => _scrollYRows;

    public void SetActiveCell(int row, int col)
    {
        _activeRow = Math.Clamp(row, 0, Math.Max(0, _rowCount - 1));
        _activeCol = Math.Clamp(col, 0, Math.Max(0, _colCount - 1));
    }

    public (int Row, int Col) GetActiveCell() => (_activeRow, _activeCol);

    public void MoveActiveCell(int dRow, int dCol, int viewportRows)
    {
        SetActiveCell(_activeRow + dRow, _activeCol + dCol);
        EnsureVisible(viewportRows);
    }

    /// <summary>
    /// Moves the active cell and guarantees it is visible both vertically and horizontally.
    /// </summary>
    public void MoveActiveCell(int dRow, int dCol, int viewportRows, int viewportWidth)
    {
        SetActiveCell(_activeRow + dRow, _activeCol + dCol);
        EnsureVisible(viewportRows);
        EnsureVisibleCols(viewportWidth);
    }

    public void EnsureVisible(int viewportRows)
    {
        viewportRows = Math.Max(1, viewportRows);
        if (_activeRow < _scrollYRows) _scrollYRows = _activeRow;
        else if (_activeRow >= _scrollYRows + viewportRows)
            _scrollYRows = Math.Min(_activeRow - viewportRows + 1, Math.Max(0, _rowCount - viewportRows));
    }

    public int GetFirstVisibleColumn() => _scrollXCols;

    public (int VisibleRows, int VisibleCols) MeasureVisible(in L.Rect rect)
    {
        int innerH = Math.Max(0, (int)rect.Height);
        int rows = _rowCount <= 0 ? 0 : Math.Min(_rowCount - _scrollYRows, innerH);
        int visCols = ComputeVisibleCols(_scrollXCols, (int)rect.Width);
        return (Math.Max(0, rows), Math.Max(0, visCols));
    }

    /// <summary>
    /// Shared column-visibility model: given a starting absolute column and an available
    /// terminal-cell width, returns how many columns are visible. The first visible column
    /// has no leading separator; every subsequent visible column consumes one separator cell.
    /// A single column that is wider than the viewport still counts as one (partially) visible
    /// column so the caller can render it truncated rather than showing nothing.
    /// </summary>
    private int ComputeVisibleCols(int startCol, int viewportWidth)
    {
        if (viewportWidth <= 0 || _colCount == 0) return 0;
        startCol = Math.Clamp(startCol, 0, _colCount - 1);
        int remain = viewportWidth;
        int visCols = 0;
        for (int c = startCol; c < _colCount; c++)
        {
            int sep = (c == startCol) ? 0 : SeparatorWidth;
            int w = Math.Max(1, _columnWidths[c]);
            int need = w + sep;
            if (remain < need)
            {
                // The very first considered column does not fit fully: still show it
                // (truncated) so the active/left-most column is never rendered as empty.
                if (visCols == 0) visCols = 1;
                break;
            }
            remain -= need;
            visCols++;
        }
        return visCols;
    }

    protected override void RenderCore(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        int x = (int)rect.X;
        int y = (int)rect.Y;
        int w = (int)rect.Width;
        int h = (int)rect.Height;
        builder.PushClip(new DL.ClipPush(x, y, w, h));
        builder.DrawRect(new DL.Rect(x, y, w, h, new DL.Rgb24(0, 0, 0)));

        if (_cellTextProvider is null || _rowCount == 0 || _colCount == 0 || w <= 0 || h <= 0)
        {
            builder.Pop();
            return;
        }

        var (visRows, visCols) = MeasureVisible(rect);
        int curY = y;
        int startRow = _scrollYRows;
        for (int r = 0; r < visRows; r++)
        {
            int curX = x;
            for (int c = 0; c < visCols; c++)
            {
                int colIndex = _scrollXCols + c;
                if (colIndex >= _colCount) break;
                if (c > 0) curX += SeparatorWidth; // separator between visible columns

                int avail = (x + w) - curX;
                if (avail <= 0) break;
                int colWidth = Math.Min(Math.Max(1, _columnWidths[colIndex]), avail);

                string text = _cellTextProvider(startRow + r, colIndex) ?? string.Empty;
                string cell = FitToWidth(text, colWidth);

                if (startRow + r == _activeRow && colIndex == _activeCol)
                {
                    builder.DrawRect(new DL.Rect(curX, curY, colWidth, 1, new DL.Rgb24(50, 80, 140)));
                    builder.DrawText(new DL.TextRun(curX, curY, cell, new DL.Rgb24(255, 255, 255), null, DL.CellAttrFlags.Bold));
                }
                else
                {
                    builder.DrawText(new DL.TextRun(curX, curY, cell, new DL.Rgb24(220, 220, 220), null, DL.CellAttrFlags.None));
                }
                curX += colWidth;
            }
            curY += 1;
            if (curY >= y + h) break;
        }
        builder.Pop();
    }

    /// <summary>
    /// Truncates and pads <paramref name="text"/> to exactly <paramref name="cellWidth"/> terminal
    /// cells, measuring by grapheme cluster so CJK, emoji, and combining sequences never overflow
    /// or split a wide glyph across the boundary.
    /// </summary>
    private string FitToWidth(string text, int cellWidth)
    {
        if (cellWidth <= 0) return string.Empty;
        text ??= string.Empty;
        var sb = new StringBuilder();
        int used = 0;
        foreach (var g in new GraphemeEnumerator(text))
        {
            int gw = _measurer.MeasureWidth(g);
            if (gw == 0)
            {
                // Zero-width (combining) marks attach to the preceding glyph without consuming a cell.
                if (used > 0) sb.Append(g);
                continue;
            }
            if (used + gw > cellWidth) break;
            sb.Append(g);
            used += gw;
        }
        if (used < cellWidth) sb.Append(' ', cellWidth - used);
        return sb.ToString();
    }

    public void AdjustScroll(int deltaRows, int viewportRows)
    {
        int maxFirst = Math.Max(0, _rowCount - Math.Max(1, viewportRows));
        int next = _scrollYRows + deltaRows;
        _scrollYRows = Math.Max(0, Math.Min(next, maxFirst));
    }

    public void EnsureVisibleCols(int viewportWidth)
    {
        viewportWidth = Math.Max(1, viewportWidth);
        if (_colCount == 0) { _scrollXCols = 0; return; }

        // Active is left of the current window: scroll left so it becomes the first column.
        if (_activeCol < _scrollXCols)
        {
            _scrollXCols = _activeCol;
            return;
        }

        // Active already within the window computed from the shared model: nothing to do.
        int visCols = ComputeVisibleCols(_scrollXCols, viewportWidth);
        if (_activeCol < _scrollXCols + visCols)
        {
            return;
        }

        // Active is right of the window: scroll right just enough to place it at the right edge,
        // pulling in as many left-hand columns as still fit. If the active column alone is wider
        // than the viewport, it becomes the sole (truncated) visible column so it is still shown.
        int start = _activeCol;
        int total = Math.Max(1, _columnWidths[_activeCol]);
        while (start > 0)
        {
            int w = Math.Max(1, _columnWidths[start - 1]);
            int need = w + SeparatorWidth;
            if (total + need <= viewportWidth)
            {
                total += need;
                start--;
            }
            else
            {
                break;
            }
        }
        _scrollXCols = start;
    }

    private void ClampState()
    {
        _activeRow = _rowCount == 0 ? 0 : Math.Clamp(_activeRow, 0, _rowCount - 1);
        _activeCol = _colCount == 0 ? 0 : Math.Clamp(_activeCol, 0, _colCount - 1);
        _scrollYRows = _rowCount == 0 ? 0 : Math.Clamp(_scrollYRows, 0, _rowCount - 1);
        _scrollXCols = _colCount == 0 ? 0 : Math.Clamp(_scrollXCols, 0, _colCount - 1);
    }
}
