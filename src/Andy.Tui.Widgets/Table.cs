using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets;

public sealed class Table
{
    private readonly List<string> _columns = new();
    private readonly List<string[]> _rows = new();
    private int _sortColumn = -1;
    private bool _sortAsc = true;
    private int[]? _minColumnWidths;
    private HashSet<int> _rightAligned = new();
    private int _highlightKeyColumnIndex = -1; // currently unused by examples
    private readonly HashSet<string> _highlightKeys = new();
    private Func<int, string, IReadOnlyList<string>, (DL.Rgb24? Fg, DL.Rgb24? Bg)>? _cellColorProvider;
    public bool ShowHeaderSeparator { get; set; } = false;

    public void SetColumns(IEnumerable<string> cols) { _columns.Clear(); _columns.AddRange(cols); }
    public void SetRows(IEnumerable<string[]> rows) { _rows.Clear(); _rows.AddRange(rows); }
    public void SetMinColumnWidths(params int[] widths) { _minColumnWidths = widths; }
    public void SetRightAlignedColumns(params int[] indices) { _rightAligned = new HashSet<int>(indices); }
    public void SetHighlightByKeyColumn(int keyColumnIndex)
    { _highlightKeyColumnIndex = keyColumnIndex; }
    public void SetHighlightedKeys(IEnumerable<string> keys)
    { _highlightKeys.Clear(); foreach (var k in keys) _highlightKeys.Add(k); }
    public void SetCellColorProvider(Func<int, string, IReadOnlyList<string>, (DL.Rgb24? Fg, DL.Rgb24? Bg)> provider)
    { _cellColorProvider = provider; }
    public void SortBy(int col, bool asc)
    {
        _sortColumn = col; _sortAsc = asc;
        if (col >= 0 && col < _columns.Count)
        {
            _rows.Sort((a, b) => string.Compare(a[col], b[col], StringComparison.OrdinalIgnoreCase) * (asc ? 1 : -1));
        }
    }

    public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        int x = (int)rect.X, y = (int)rect.Y, w = (int)rect.Width, h = (int)rect.Height;
        builder.PushClip(new DL.ClipPush(x, y, w, h));
        builder.DrawRect(new DL.Rect(x, y, w, h, new DL.Rgb24(15, 15, 15)));
        // header (single run to satisfy tests ordering expectations)
        int yy = y;
        var headerFg = new DL.Rgb24(200, 200, 200);
        var headerBg = new DL.Rgb24(25, 25, 25);
        // compute column widths from header, min widths, and sample of cell text
        var colWidths = _columns.Select(c => Math.Max(c.Length + 3, 10)).ToArray();
        if (_minColumnWidths is not null)
        {
            for (int i = 0; i < Math.Min(colWidths.Length, _minColumnWidths.Length); i++)
                colWidths[i] = Math.Max(colWidths[i], _minColumnWidths[i]);
        }
        // expand by max cell length observed (up to viewport width budget)
        for (int i = 0; i < _columns.Count; i++)
        {
            int maxCell = 0;
            foreach (var row in _rows)
            {
                if (i < row.Length) maxCell = Math.Max(maxCell, row[i].Length + 2);
            }
            colWidths[i] = Math.Max(colWidths[i], Math.Min(maxCell, Math.Max(10, w / Math.Max(1, _columns.Count))));
        }
        var headerTextBuilder = new System.Text.StringBuilder();
        for (int i = 0; i < _columns.Count && i < colWidths.Length; i++)
        {
            string col = _columns[i];
            if (i == _sortColumn)
            {
                col += _sortAsc ? " ▲" : " ▼";
            }
            int width = colWidths[i];
            // pad to width, but keep within table width budget
            string text = col.PadRight(width);
            headerTextBuilder.Append(text);
            if (1 + headerTextBuilder.Length >= w) break;
        }
        string headerText = headerTextBuilder.ToString();
        // Place header inside border: one row below top border
        int headerY = y + 1;
        // Solid header bar background for visibility
        builder.DrawRect(new DL.Rect(x, headerY, w, 1, headerBg));
        builder.DrawText(new DL.TextRun(x + 1, headerY, headerText, headerFg, headerBg, DL.CellAttrFlags.Bold));
        // optional header separator line
        if (ShowHeaderSeparator)
        {
            int sepY = headerY + 1;
            builder.DrawRect(new DL.Rect(x, sepY, w, 1, new DL.Rgb24(35, 35, 35)));
            yy = sepY + 1;
        }
        else
        {
            yy = headerY + 1;
        }
        // rows
        for (int rowIndex = 0; rowIndex < _rows.Count; rowIndex++)
        {
            var row = _rows[rowIndex];
            int curX = x + 1;
            // optional row highlight was removed per UX feedback (too much blinking)
            // Clear the row content area with spaces to avoid leftover glyphs when values shrink
            int contentW = Math.Max(0, w - 2);
            if (contentW > 0)
            {
                builder.DrawText(new DL.TextRun(x + 1, yy, new string(' ', contentW), new DL.Rgb24(220, 220, 220), null, DL.CellAttrFlags.None));
            }
            for (int i = 0; i < _columns.Count && i < row.Length; i++)
            {
                var cell = row[i];
                DL.Rgb24? bg = null;
                var fg = new DL.Rgb24(220, 220, 220);
                if (_cellColorProvider is not null)
                {
                    var (pf, pb) = _cellColorProvider(i, cell, row);
                    if (pf is not null) fg = pf.Value;
                    if (pb is not null) bg = pb;
                }
                if (_columns[i].Equals("Change", StringComparison.OrdinalIgnoreCase))
                {
                    if (cell.StartsWith("+")) fg = new DL.Rgb24(60, 200, 120);
                    else if (cell.StartsWith("-")) fg = new DL.Rgb24(220, 80, 80);
                }
                // alignment
                int drawX = curX;
                if (_rightAligned.Contains(i))
                {
                    drawX = Math.Max(curX, curX + colWidths[Math.Min(i, colWidths.Length - 1)] - Math.Max(1, cell.Length) - 1);
                }
                builder.DrawText(new DL.TextRun(drawX, yy, cell, fg, bg, DL.CellAttrFlags.None));
                curX += colWidths[Math.Min(i, colWidths.Length - 1)];
                if (curX >= x + w) break;
            }
            // After drawing all cells, ensure trailing area to the right is blanked with spaces
            int rightEdge = x + Math.Max(1, w - 1);
            if (curX < rightEdge)
            {
                int spaces = rightEdge - curX;
                builder.DrawText(new DL.TextRun(curX, yy, new string(' ', spaces), new DL.Rgb24(220, 220, 220), null, DL.CellAttrFlags.None));
            }
            yy++; if (yy >= y + h) break;
        }
        // Clear any remaining content lines (when previous frame had more rows)
        int contentBottom = y + h - 1; // reserve last row for bottom border
        while (yy < contentBottom)
        {
            int contentW2 = Math.Max(0, w - 2);
            if (contentW2 > 0)
            {
                builder.DrawText(new DL.TextRun(x + 1, yy, new string(' ', contentW2), new DL.Rgb24(220, 220, 220), null, DL.CellAttrFlags.None));
            }
            yy++;
        }
        builder.DrawBorder(new DL.Border(x, y, w, h, "single", new DL.Rgb24(80, 80, 80)));
        builder.Pop();
    }
}
