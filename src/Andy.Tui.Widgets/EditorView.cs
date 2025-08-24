using System;
using System.Linq;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets;

public sealed class EditorView
{
    private string[] _lines = Array.Empty<string>();
    private int _cursorRow;
    private int _cursorCol;
    private int _scrollRow;

    public void SetText(string text)
    {
        _lines = (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        _cursorRow = 0; _cursorCol = 0; _scrollRow = 0;
    }

    public void SetCursor(int row, int col)
    {
        _cursorRow = Math.Max(0, Math.Min(row, Math.Max(0, _lines.Length - 1)));
        _cursorCol = Math.Max(0, Math.Min(col, _lines.Length == 0 ? 0 : _lines[_cursorRow].Length));
    }

    public (int Row, int Col) GetCursor() => (_cursorRow, _cursorCol);

    public void MoveCursorBy(int dRow, int dCol)
    {
        int r = Math.Max(0, Math.Min(_cursorRow + dRow, Math.Max(0, _lines.Length - 1)));
        int c = Math.Max(0, Math.Min(_cursorCol + dCol, _lines.Length == 0 ? 0 : _lines[r].Length));
        _cursorRow = r; _cursorCol = c;
    }

    public void EnsureCursorVisible(int viewportRows)
    {
        if (_cursorRow < _scrollRow) _scrollRow = _cursorRow;
        else if (_cursorRow >= _scrollRow + viewportRows) _scrollRow = _cursorRow - Math.Max(0, viewportRows - 1);
        if (_scrollRow < 0) _scrollRow = 0;
    }

    public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        int x = (int)rect.X;
        int y = (int)rect.Y;
        int w = (int)rect.Width;
        int h = (int)rect.Height;
        builder.PushClip(new DL.ClipPush(x, y, w, h));
        builder.DrawRect(new DL.Rect(x, y, w, h, new DL.Rgb24(0, 0, 0)));

        int visibleRows = Math.Max(0, h);
        EnsureCursorVisible(visibleRows);

        for (int i = 0; i < visibleRows; i++)
        {
            int lineIndex = _scrollRow + i;
            if (lineIndex >= _lines.Length) break;
            string line = _lines[lineIndex] ?? string.Empty;
            // Draw line content, clipped to width
            string snippet = line.Length > w ? line.Substring(0, w) : line;
            builder.DrawText(new DL.TextRun(x, y + i, snippet.PadRight(Math.Max(0, w)), new DL.Rgb24(200, 200, 200), null, DL.CellAttrFlags.None));
        }

        // Draw caret (as a vertical bar) if within viewport
        if (_cursorRow >= _scrollRow && _cursorRow < _scrollRow + visibleRows)
        {
            int caretY = y + (_cursorRow - _scrollRow);
            int caretX = x + Math.Min(Math.Max(0, _cursorCol), Math.Max(0, w - 1));
            builder.DrawText(new DL.TextRun(caretX, caretY, "|", new DL.Rgb24(255, 255, 180), null, DL.CellAttrFlags.Bold));
        }

        builder.Pop();
    }
}
