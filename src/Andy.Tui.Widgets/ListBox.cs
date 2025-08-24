using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets;

public sealed class ListBox
{
    private readonly List<string> _items = new();
    public int SelectedIndex { get; private set; } = -1;
    private int _firstVisibleIndex = 0;
    public DL.Rgb24 Fg { get; private set; } = new DL.Rgb24(220, 220, 220);
    public DL.Rgb24 Bg { get; private set; } = new DL.Rgb24(20, 20, 20);
    public DL.Rgb24 SelectedBg { get; private set; } = new DL.Rgb24(60, 60, 100);
    public DL.Rgb24 Border { get; private set; } = new DL.Rgb24(100, 100, 100);

    public void SetItems(IEnumerable<string> items)
    {
        _items.Clear();
        _items.AddRange(items);
        if (SelectedIndex >= _items.Count) SelectedIndex = _items.Count - 1;
    }
    public void SetSelectedIndex(int index) => SelectedIndex = Math.Clamp(index, -1, _items.Count - 1);

    public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        int x = (int)rect.X;
        int y = (int)rect.Y;
        int w = (int)rect.Width;
        int h = (int)rect.Height;
        builder.PushClip(new DL.ClipPush(x, y, w, h));
        builder.DrawRect(new DL.Rect(x, y, w, h, Bg));
        builder.DrawBorder(new DL.Border(x, y, w, h, "single", Border));
        int yy = y;
        for (int i = _firstVisibleIndex; i < _items.Count && yy < y + h; i++, yy++)
        {
            bool sel = i == SelectedIndex;
            var lineBg = sel ? SelectedBg : Bg;
            builder.DrawRect(new DL.Rect(x, yy, w, 1, lineBg));
            var attrs = sel ? DL.CellAttrFlags.Bold : DL.CellAttrFlags.None;
            builder.DrawText(new DL.TextRun(x + 1, yy, _items[i], Fg, lineBg, attrs));
        }
        builder.Pop();
    }

    // Simple hit testing and interactions
    public void OnMouseDown(int x, int y, in L.Rect rect)
    {
        if (y < rect.Y || y >= rect.Y + rect.Height) return;
        int localRow = y - (int)rect.Y;
        int index = _firstVisibleIndex + localRow;
        if (index >= 0 && index < _items.Count) SetSelectedIndex(index);
        EnsureSelectionVisible((int)rect.Height);
    }

    public void MoveSelection(int delta, int viewportRows)
    {
        if (_items.Count == 0) return;
        int next = SelectedIndex < 0 ? 0 : SelectedIndex + delta;
        SetSelectedIndex(next);
        EnsureSelectionVisible(viewportRows);
    }

    public void Page(int deltaPages, int viewportRows)
    {
        int delta = deltaPages * Math.Max(1, viewportRows - 1);
        MoveSelection(delta, viewportRows);
    }

    public void Home(int viewportRows)
    {
        SetSelectedIndex(0);
        _firstVisibleIndex = 0;
    }

    public void End(int viewportRows)
    {
        SetSelectedIndex(_items.Count - 1);
        _firstVisibleIndex = Math.Max(0, _items.Count - Math.Max(1, viewportRows));
    }

    private void EnsureSelectionVisible(int viewportRows)
    {
        viewportRows = Math.Max(1, viewportRows);
        if (SelectedIndex < _firstVisibleIndex) _firstVisibleIndex = SelectedIndex;
        else if (SelectedIndex >= _firstVisibleIndex + viewportRows)
            _firstVisibleIndex = SelectedIndex - viewportRows + 1;
        _firstVisibleIndex = Math.Max(0, Math.Min(_firstVisibleIndex, Math.Max(0, _items.Count - viewportRows)));
    }
}
