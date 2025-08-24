using System;
using System.Linq;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets;

public sealed class VirtualizedGrid
{
    private int _rowCount;
    private int _colCount;
    private int[] _columnWidths = Array.Empty<int>();
    private Func<int, int, string>? _cellTextProvider;

    private int _scrollYRows;
    private int _scrollXCols;
    private int _activeRow;
    private int _activeCol;

    public void SetDimensions(int rows, int cols)
    {
        _rowCount = Math.Max(0, rows);
        _colCount = Math.Max(0, cols);
        if (_columnWidths.Length != _colCount)
        {
            _columnWidths = Enumerable.Repeat(8, _colCount).ToArray();
        }
    }

    public void SetColumnWidths(int[] widths)
    {
        if (widths is null) throw new ArgumentNullException(nameof(widths));
        _columnWidths = widths.ToArray();
        _colCount = widths.Length;
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
        int innerH = Math.Max(0, (int)rect.Height - 0);
        int rows = Math.Min(_rowCount - _scrollYRows, innerH);
        // Columns are width-based: fit as many as possible
        int remain = Math.Max(0, (int)rect.Width);
        int visCols = 0;
        for (int c = _scrollXCols; c < _colCount && remain > 0; c++)
        {
            int w = Math.Max(1, _columnWidths[c]);
            if (remain < w + (c == 0 ? 0 : 1)) break;
            remain -= w + (c == 0 ? 0 : 1); // +1 for a single space separator
            visCols++;
        }
        return (Math.Max(0, rows), Math.Max(0, visCols));
    }

    public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        int x = (int)rect.X;
        int y = (int)rect.Y;
        int w = (int)rect.Width;
        int h = (int)rect.Height;
        builder.PushClip(new DL.ClipPush(x, y, w, h));
        builder.DrawRect(new DL.Rect(x, y, w, h, new DL.Rgb24(0, 0, 0)));

        if (_cellTextProvider is null || _rowCount == 0 || _colCount == 0)
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
                int colWidth = Math.Max(1, _columnWidths[colIndex]);
                string text = _cellTextProvider(startRow + r, colIndex) ?? string.Empty;
                if (text.Length > colWidth) text = text.Substring(0, colWidth);
                // Active cell highlight
                if (startRow + r == _activeRow && colIndex == _activeCol)
                {
                    builder.DrawRect(new DL.Rect(curX, curY, colWidth, 1, new DL.Rgb24(50, 80, 140)));
                    builder.DrawText(new DL.TextRun(curX, curY, text.PadRight(colWidth), new DL.Rgb24(255, 255, 255), null, DL.CellAttrFlags.Bold));
                }
                else
                {
                    builder.DrawText(new DL.TextRun(curX, curY, text.PadRight(colWidth), new DL.Rgb24(220, 220, 220), null, DL.CellAttrFlags.None));
                }
                curX += colWidth + 1; // gap
            }
            curY += 1;
            if (curY >= y + h) break;
        }
        builder.Pop();
    }

    public void AdjustScroll(int deltaRows, int viewportRows)
    {
        int maxFirst = Math.Max(0, _rowCount - Math.Max(1, viewportRows));
        int next = _scrollYRows + deltaRows;
        _scrollYRows = Math.Max(0, Math.Min(next, maxFirst));
    }

    public void EnsureVisibleCols(int viewportWidth)
    {
        // Make active column visible by adjusting _scrollXCols based on widths
        viewportWidth = Math.Max(1, viewportWidth);
        // If active before first visible col, scroll left
        if (_activeCol < _scrollXCols) { _scrollXCols = _activeCol; return; }
        // If active beyond last visible col, scroll right just enough
        int remain = viewportWidth;
        int c = _scrollXCols;
        while (c < _colCount && remain > 0)
        {
            int w = Math.Max(1, _columnWidths[c]);
            if (remain < w + (c == 0 ? 0 : 1)) break;
            remain -= w + (c == 0 ? 0 : 1);
            if (c == _activeCol) return; // already visible
            c++;
        }
        // Not visible: advance scroll to place active at end of window
        _scrollXCols = Math.Min(_activeCol, _colCount - 1);
        // Back up until active fits within width
        remain = viewportWidth;
        int start = _scrollXCols;
        while (start > 0)
        {
            int w = Math.Max(1, _columnWidths[start]);
            int sep = (start == 0) ? 0 : 1;
            if (remain >= w + sep)
            {
                remain -= w + sep;
                start--;
            }
            else break;
        }
        _scrollXCols = start;
    }
}
