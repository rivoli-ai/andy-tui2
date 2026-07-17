using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;
using ST = Andy.Tui.Style;

namespace Andy.Tui.Widgets;

public sealed class Panel : IThemeable, IStyleable
{
    public string? Title { get; private set; }
    public DL.Rgb24 Bg { get; private set; } = ST.ThemeContext.Current.GetRgb(ST.ThemeToken.Background, new DL.Rgb24(12, 12, 12));
    public DL.Rgb24 Border { get; private set; } = ST.ThemeContext.Current.GetRgb(ST.ThemeToken.Border, new DL.Rgb24(100, 100, 100));
    public DL.Rgb24 TitleColor { get; private set; } = ST.ThemeContext.Current.GetRgb(ST.ThemeToken.Foreground, new DL.Rgb24(200, 200, 200));

    /// <inheritdoc />
    public void ApplyTheme(ST.Theme theme)
    {
        Bg = theme.GetRgb(ST.ThemeToken.Background, new DL.Rgb24(12, 12, 12));
        Border = theme.GetRgb(ST.ThemeToken.Border, new DL.Rgb24(100, 100, 100));
        TitleColor = theme.GetRgb(ST.ThemeToken.Foreground, new DL.Rgb24(200, 200, 200));
    }

    /// <inheritdoc />
    public void ApplyStyle(in ST.ResolvedStyle style)
    {
        if (style.Color.ToRgb24() is { } fg) TitleColor = fg;
        if (style.BackgroundColor.ToRgb24() is { } bg) Bg = bg;
    }

    public void SetTitle(string? title) => Title = title;

    public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        int x = (int)rect.X;
        int y = (int)rect.Y;
        int w = (int)rect.Width;
        int h = (int)rect.Height;
        builder.PushClip(new DL.ClipPush(x, y, w, h));
        builder.DrawRect(new DL.Rect(x, y, w, h, Bg));
        builder.DrawBorder(new DL.Border(x, y, w, h, "single", Border));
        if (!string.IsNullOrEmpty(Title))
        {
            builder.DrawText(new DL.TextRun(x + 2, y, Title!, TitleColor, Bg, DL.CellAttrFlags.Bold));
        }
        builder.Pop();
    }
}
