using System;
using System.Linq;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets;

public sealed class Select
{
    private string[] _items = Array.Empty<string>();
    private int _selectedIndex;
    private int _highlightIndex;
    private bool _isOpen;

    public DL.Rgb24 Bg { get; private set; } = new(12, 12, 12);
    public DL.Rgb24 Fg { get; private set; } = new(220, 220, 220);
    public DL.Rgb24 Border { get; private set; } = new(90, 90, 90);
    public DL.Rgb24 Accent { get; private set; } = new(180, 180, 220);

    public void SetItems(string[] items)
    {
        _items = items?.ToArray() ?? Array.Empty<string>();
        if (_items.Length == 0) _selectedIndex = 0;
        else _selectedIndex = Math.Max(0, Math.Min(_selectedIndex, _items.Length - 1));
    }

    public void SetSelectedIndex(int index)
    {
        _selectedIndex = _items.Length == 0 ? 0 : Math.Max(0, Math.Min(index, _items.Length - 1));
    }

    public int GetSelectedIndex() => _selectedIndex;
    public int GetHighlightIndex() => _highlightIndex;
    public bool IsOpen() => _isOpen;

    public void SetOpen(bool open)
    {
        _isOpen = open;
        if (_isOpen)
        {
            _highlightIndex = _selectedIndex;
        }
    }

    public void ToggleOpen() => SetOpen(!_isOpen);

    public void MoveHighlight(int delta)
    {
        if (!_isOpen || _items.Length == 0) return;
        int next = _highlightIndex + delta;
        _highlightIndex = Math.Max(0, Math.Min(next, _items.Length - 1));
    }

    public void ConfirmSelection()
    {
        if (_isOpen && _items.Length > 0)
        {
            _selectedIndex = _highlightIndex;
            _isOpen = false;
        }
    }

    public void Cancel() { _isOpen = false; }

    public string? GetSelectedText() => (_items.Length == 0 || _selectedIndex < 0 || _selectedIndex >= _items.Length) ? null : _items[_selectedIndex];

    public int MeasureClosedWidth()
    {
        int textW = _items.Length == 0 ? 0 : _items.Max(s => s?.Length ?? 0);
        // padding 2 + arrow area 2
        return Math.Max(6, textW + 4);
    }

    public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        int x = (int)rect.X;
        int y = (int)rect.Y;
        int w = (int)rect.Width;
        int h = (int)rect.Height;
        if (w <= 0 || h <= 0) return;
        builder.PushClip(new DL.ClipPush(x, y, w, h));
        builder.DrawRect(new DL.Rect(x, y, w, h, Bg));
        builder.DrawBorder(new DL.Border(x, y, w, h, "single", Border));
        string text = GetSelectedText() ?? string.Empty;
        int innerW = Math.Max(0, w - 2);
        int contentW = Math.Max(0, innerW - 2); // reserve 2 cols for arrow area
        string clipped = text.Length > contentW ? text.Substring(0, contentW) : text;
        builder.DrawText(new DL.TextRun(x + 1, y, clipped.PadRight(contentW), Fg, Bg, DL.CellAttrFlags.None));
        // draw arrow at far right inside
        if (w >= 2)
        {
            string arrow = _isOpen ? "▲" : "▼";
            builder.DrawText(new DL.TextRun(x + w - 2, y, arrow, Accent, Bg, DL.CellAttrFlags.Bold));
        }
        builder.Pop();
    }

    public (int Width, int Height) MeasurePopup()
    {
        int textW = _items.Length == 0 ? 0 : _items.Max(s => s?.Length ?? 0);
        int pw = Math.Max(8, textW + 4);
        int ph = Math.Min(Math.Max(2, _items.Length + 2), 8);
        return (pw, ph);
    }

    public void RenderPopup(int anchorX, int anchorY, int viewportW, int viewportH, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        if (!_isOpen || _items.Length == 0) return;
        var (pw, ph) = MeasurePopup();
        var (px, py) = MenuHelpers.ComputePopupPosition(anchorX, anchorY, pw, ph, viewportW, viewportH);
        builder.PushClip(new DL.ClipPush(px, py, pw, ph));
        builder.DrawRect(new DL.Rect(px, py, pw, ph, new DL.Rgb24(20, 20, 20)));
        builder.DrawBorder(new DL.Border(px, py, pw, ph, "single", Border));
        int innerW2 = Math.Max(0, pw - 2);
        int visible = Math.Max(0, ph - 2);
        int start = Math.Max(0, Math.Min(_highlightIndex - (visible / 2), Math.Max(0, _items.Length - visible)));
        for (int i = 0; i < visible && start + i < _items.Length; i++)
        {
            int iy = py + 1 + i;
            bool isSel = (start + i) == _highlightIndex;
            var rowBg = isSel ? new DL.Rgb24(60, 60, 90) : new DL.Rgb24(20, 20, 20);
            builder.DrawRect(new DL.Rect(px + 1, iy, innerW2, 1, rowBg));
            string t = _items[start + i] ?? string.Empty;
            string tclip = t.Length > innerW2 - 1 ? t.Substring(0, Math.Max(0, innerW2 - 1)) : t;
            builder.DrawText(new DL.TextRun(px + 2, iy, tclip, Fg, rowBg, isSel ? DL.CellAttrFlags.Bold : DL.CellAttrFlags.None));
        }
        builder.Pop();
    }
}
