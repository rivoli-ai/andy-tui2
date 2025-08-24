using System;
using System.Linq;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class DataGrid
    {
        private string[] _columns = Array.Empty<string>();
        private int[] _columnWidths = Array.Empty<int>();
        private int _rowCount;
        private Func<int, int, string>? _cellTextProvider;

        private int _scrollRows;
        private int _activeRow;
        private int _activeCol;

        public void SetColumns(string[] headers, int[] widths)
        {
            _columns = headers ?? Array.Empty<string>();
            _columnWidths = widths?.ToArray() ?? Array.Empty<int>();
            if (_columns.Length != _columnWidths.Length)
                throw new ArgumentException("headers and widths must match length");
        }

        public void SetRowCount(int rows) => _rowCount = Math.Max(0, rows);
        public void SetCellTextProvider(Func<int, int, string> provider) => _cellTextProvider = provider ?? throw new ArgumentNullException(nameof(provider));

        public void SetActiveCell(int row, int col)
        {
            _activeRow = Math.Clamp(row, 0, Math.Max(0, _rowCount - 1));
            _activeCol = Math.Clamp(col, 0, Math.Max(0, _columns.Length - 1));
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
            if (_activeRow < _scrollRows) _scrollRows = _activeRow;
            else if (_activeRow >= _scrollRows + viewportRows)
                _scrollRows = Math.Min(_activeRow - viewportRows + 1, Math.Max(0, _rowCount - viewportRows));
        }

        public (int VisibleRows, int VisibleCols) MeasureVisible(in L.Rect rect)
        {
            int innerH = Math.Max(0, (int)rect.Height - 2); // header + separator
            int rows = Math.Min(_rowCount - _scrollRows, innerH);
            int remain = Math.Max(0, (int)rect.Width);
            int visCols = 0;
            for (int c = 0; c < _columns.Length && remain > 0; c++)
            {
                int w = Math.Max(1, _columnWidths[c]);
                if (remain < w + (c == 0 ? 0 : 1)) break;
                remain -= w + (c == 0 ? 0 : 1);
                visCols++;
            }
            return (Math.Max(0, rows), Math.Max(0, visCols));
        }

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x = (int)rect.X; int y = (int)rect.Y; int w = (int)rect.Width; int h = (int)rect.Height;
            if (w <= 0 || h <= 0) return;
            b.PushClip(new DL.ClipPush(x, y, w, h));
            b.DrawRect(new DL.Rect(x, y, w, h, new DL.Rgb24(0, 0, 0)));

            // Header
            int curX = x; int headerY = y;
            int visCols;
            {
                int remain = w; visCols = 0;
                for (int c = 0; c < _columns.Length && remain > 0; c++)
                {
                    int cw = Math.Max(1, _columnWidths[c]);
                    if (remain < cw + (c == 0 ? 0 : 1)) break;
                    string hdr = _columns[c]; if (hdr.Length > cw) hdr = hdr.Substring(0, cw);
                    b.DrawText(new DL.TextRun(curX, headerY, hdr.PadRight(cw), new DL.Rgb24(180, 200, 240), null, DL.CellAttrFlags.Bold));
                    curX += cw + 1; remain -= cw + (c == 0 ? 0 : 1); visCols++;
                }
            }
            // Separator under header
            b.DrawRect(new DL.Rect(x, y + 1, w, 1, new DL.Rgb24(20, 20, 20)));

            if (_cellTextProvider is null || _rowCount == 0 || _columns.Length == 0)
            {
                b.Pop(); return;
            }

            var (visRows, _) = MeasureVisible(rect);
            int startRow = _scrollRows;
            int curY = y + 2;
            for (int r = 0; r < visRows; r++)
            {
                int cx = x;
                for (int c = 0; c < visCols; c++)
                {
                    int cw = Math.Max(1, _columnWidths[c]);
                    string text = _cellTextProvider(startRow + r, c) ?? string.Empty;
                    if (text.Length > cw) text = text.Substring(0, cw);
                    if (startRow + r == _activeRow && c == _activeCol)
                    {
                        b.DrawRect(new DL.Rect(cx, curY, cw, 1, new DL.Rgb24(50, 80, 140)));
                        b.DrawText(new DL.TextRun(cx, curY, text.PadRight(cw), new DL.Rgb24(255, 255, 255), null, DL.CellAttrFlags.Bold));
                    }
                    else
                    {
                        b.DrawText(new DL.TextRun(cx, curY, text.PadRight(cw), new DL.Rgb24(220, 220, 220), null, DL.CellAttrFlags.None));
                    }
                    cx += cw + 1;
                }
                curY += 1;
                if (curY >= y + h) break;
            }

            b.Pop();
        }

        public void AdjustScroll(int deltaRows, int viewportRows)
        {
            int maxFirst = Math.Max(0, _rowCount - Math.Max(1, viewportRows));
            int next = _scrollRows + deltaRows;
            _scrollRows = Math.Max(0, Math.Min(next, maxFirst));
        }
    }
}
