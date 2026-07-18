using DL = Andy.Tui.DisplayList;
using ST = Andy.Tui.Style;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets;

public sealed class ContextMenu : WidgetBase, IThemeable, IStyleable
{
    private Menu _menu = new();
    private int _selectedIndex;

    public DL.Rgb24 Bg { get; private set; } = ST.ThemeContext.Current.GetRgb(ST.ThemeToken.SurfaceSunken, new(20, 20, 20));
    public DL.Rgb24 Fg { get; private set; } = ST.ThemeContext.Current.GetRgb(ST.ThemeToken.Foreground, new(220, 220, 220));
    public DL.Rgb24 SelBg { get; private set; } = ST.ThemeContext.Current.GetRgb(ST.ThemeToken.SurfaceSelected, new(60, 60, 90));
    public DL.Rgb24 Border { get; private set; } = ST.ThemeContext.Current.GetRgb(ST.ThemeToken.Border, new(100, 100, 100));

    /// <inheritdoc />
    public void ApplyTheme(ST.Theme theme)
    {
        Bg = theme.GetRgb(ST.ThemeToken.SurfaceSunken, new DL.Rgb24(20, 20, 20));
        Fg = theme.GetRgb(ST.ThemeToken.Foreground, new DL.Rgb24(220, 220, 220));
        SelBg = theme.GetRgb(ST.ThemeToken.SurfaceSelected, new DL.Rgb24(60, 60, 90));
        Border = theme.GetRgb(ST.ThemeToken.Border, new DL.Rgb24(100, 100, 100));
    }

    /// <inheritdoc />
    public void ApplyStyle(in ST.ResolvedStyle style)
    {
        if (style.Color.ToRgb24() is { } fg) Fg = fg;
        if (style.BackgroundColor.ToRgb24() is { } bg) Bg = bg;
    }

    public void SetMenu(Menu menu) => _menu = menu;
    public void SetSelectedIndex(int index) => _selectedIndex = Math.Max(0, Math.Min(_menu.Items.Count - 1, index));

    public (int Width, int Height) Measure()
    {
        int w = Math.Max(8, (_menu.Items.Count == 0 ? 0 : _menu.Items.Max(i => i.Text.Length)) + 4);
        int h = Math.Max(2, _menu.Items.Count + 2);
        return (w, h);
    }

    protected override L.Size MeasureCore(L.Size available)
    {
        var (w, h) = Measure();
        return new L.Size(w, h);
    }

    protected override void RenderCore(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        int x = (int)rect.X;
        int y = (int)rect.Y;
        int w = (int)rect.Width;
        int h = (int)rect.Height;
        builder.PushClip(new DL.ClipPush(x, y, w, h));
        builder.DrawRect(new DL.Rect(x, y, w, h, Bg));
        builder.DrawBorder(new DL.Border(x, y, w, h, "single", Border));
        int innerW = Math.Max(0, w - 2);
        for (int i = 0; i < _menu.Items.Count && i < h - 2; i++)
        {
            var item = _menu.Items[i];
            bool isSel = i == _selectedIndex;
            var rowBg = isSel ? SelBg : Bg;
            builder.DrawRect(new DL.Rect(x + 1, y + 1 + i, innerW, 1, rowBg));
            builder.DrawText(new DL.TextRun(x + 2, y + 1 + i, item.Text, Fg, rowBg, isSel ? DL.CellAttrFlags.Bold : DL.CellAttrFlags.None));
            if (item.Submenu is not null)
            {
                builder.DrawText(new DL.TextRun(x + w - 2, y + 1 + i, "▶", Fg, rowBg, DL.CellAttrFlags.None));
            }
        }
        builder.Pop();
    }
}
