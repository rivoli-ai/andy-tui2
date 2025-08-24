using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets;

public sealed class MenuItem
{
    public string Text { get; }
    public char? Accelerator { get; }
    public Action? Action { get; }
    public Menu? Submenu { get; }
    public MenuItem(string text, char? accelerator = null, Action? action = null, Menu? submenu = null)
    { Text = text; Accelerator = accelerator; Action = action; Submenu = submenu; }
}

public sealed class Menu
{
    private readonly List<MenuItem> _items = new();
    public Menu Add(string text, char? accelerator = null, Action? action = null, Menu? submenu = null)
    { _items.Add(new MenuItem(text, accelerator, action, submenu)); return this; }
    public IReadOnlyList<MenuItem> Items => _items;

    public int IndexOfAccelerator(char key)
    {
        char k = char.ToUpperInvariant(key);
        for (int i = 0; i < _items.Count; i++)
        {
            var acc = _items[i].Accelerator;
            if (acc.HasValue && char.ToUpperInvariant(acc.Value) == k) return i;
        }
        return -1;
    }

    public int IndexOfFirstStartingWith(char key)
    {
        char k = char.ToUpperInvariant(key);
        for (int i = 0; i < _items.Count; i++)
        {
            var txt = _items[i].Text;
            if (!string.IsNullOrEmpty(txt) && char.ToUpperInvariant(txt[0]) == k) return i;
        }
        return -1;
    }
}

public sealed class MenuBar
{
    private readonly List<(string Title, Menu Menu)> _menus = new();
    public IReadOnlyList<(string Title, Menu Menu)> Menus => _menus;
    public DL.Rgb24 Fg { get; private set; } = new(220, 220, 220);
    public DL.Rgb24 Bg { get; private set; } = new(30, 30, 30);
    public DL.Rgb24 Accent { get; private set; } = new(200, 200, 80);

    public MenuBar Add(string title, Menu menu)
    { _menus.Add((title, menu)); return this; }

    public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
        => Render(rect, baseDl, builder, activeHeaderIndex: null);

    public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder, int? activeHeaderIndex)
    {
        int x = (int)rect.X; int y = (int)rect.Y; int w = (int)rect.Width;
        // Draw a full-width background and a border-like baseline
        builder.DrawRect(new DL.Rect(x, y, w, 1, Bg));
        int curX = x + 2;
        if (_menus.Count == 0)
        {
            // Fallback placeholder to ensure something visible
            builder.DrawText(new DL.TextRun(curX, y, "(Menu)", Accent, Bg, DL.CellAttrFlags.Bold));
            return;
        }
        for (int i = 0; i < _menus.Count; i++)
        {
            var title = _menus[i].Title;
            bool isActive = activeHeaderIndex.HasValue && activeHeaderIndex.Value == i;
            if (isActive)
            {
                // simple underline block behind the active header
                builder.DrawRect(new DL.Rect(curX - 1, y, title.Length + 2, 1, new DL.Rgb24(50, 50, 50)));
            }
            // Render title with optional underscore for accelerator marker (first letter underlined)
            if (!string.IsNullOrEmpty(title))
            {
                // Underline the first character as a conventional header accelerator
                builder.DrawText(new DL.TextRun(curX, y, title.Substring(0, 1), isActive ? Accent : Fg, isActive ? new DL.Rgb24(50, 50, 50) : Bg, DL.CellAttrFlags.Bold | DL.CellAttrFlags.Underline));
                if (title.Length > 1)
                {
                    builder.DrawText(new DL.TextRun(curX + 1, y, title.Substring(1), isActive ? Accent : Fg, isActive ? new DL.Rgb24(50, 50, 50) : Bg, DL.CellAttrFlags.Bold));
                }
            }
            curX += title.Length + 4;
            if (curX >= x + w) break;
        }
    }

    public IReadOnlyList<(string Title, int X)> ComputeHeaderPositions(int startX, int spacing, int maxWidth)
    {
        var result = new List<(string, int)>(_menus.Count);
        int curX = startX;
        for (int i = 0; i < _menus.Count; i++)
        {
            var title = _menus[i].Title;
            if (curX >= maxWidth) break;
            result.Add((title, curX));
            curX += title.Length + spacing;
        }
        return result;
    }
}
