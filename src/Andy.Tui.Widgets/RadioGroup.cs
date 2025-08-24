using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets;

public sealed class RadioGroup
{
    private readonly List<string> _items = new();
    public int SelectedIndex { get; private set; } = -1;
    public DL.Rgb24 Fg { get; private set; } = new DL.Rgb24(220, 220, 220);
    public DL.Rgb24 Bg { get; private set; } = new DL.Rgb24(40, 40, 40);
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
        for (int i = 0; i < _items.Count && yy < y + h; i++, yy++)
        {
            bool sel = i == SelectedIndex;
            var marker = sel ? "(o)" : "( )";
            var attrs = sel ? DL.CellAttrFlags.Bold : DL.CellAttrFlags.None;
            builder.DrawText(new DL.TextRun(x + 1, yy, $"{marker} {_items[i]}", Fg, Bg, attrs));
        }
        builder.Pop();
    }
}
