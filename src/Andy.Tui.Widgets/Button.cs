using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;
using ST = Andy.Tui.Style;

namespace Andy.Tui.Widgets;

public sealed class Button : WidgetBase
{
    public string Text { get; private set; }

    /// <summary>Alias of <see cref="WidgetBase.IsEnabled"/> kept for backward compatibility.</summary>
    public bool Enabled => IsEnabled;
    public bool IsHovered { get; private set; }
    public bool IsActive { get; private set; }

    public override bool Focusable => true;

    // Style palette, seeded from the ambient theme (ThemeContext.Current) at construction.
    public DL.Rgb24 Fg { get; private set; } = ST.ThemeContext.Current.GetRgb(ST.ThemeToken.Foreground, new DL.Rgb24(220, 220, 220));
    public DL.Rgb24 Bg { get; private set; } = ST.ThemeContext.Current.GetRgb(ST.ThemeToken.Surface, new DL.Rgb24(40, 40, 40));
    public DL.Rgb24 BgHover { get; private set; } = ST.ThemeContext.Current.GetRgb(ST.ThemeToken.SurfaceHover, new DL.Rgb24(55, 55, 55));
    public DL.Rgb24 BgActive { get; private set; } = ST.ThemeContext.Current.GetRgb(ST.ThemeToken.SurfaceActive, new DL.Rgb24(80, 80, 120));
    public DL.Rgb24 BgDisabled { get; private set; } = ST.ThemeContext.Current.GetRgb(ST.ThemeToken.SurfaceDisabled, new DL.Rgb24(30, 30, 30));
    public DL.Rgb24 Border { get; private set; } = ST.ThemeContext.Current.GetRgb(ST.ThemeToken.Border, new DL.Rgb24(100, 100, 100));

    public Button(string text) => Text = text;

    public void SetText(string text) { Text = text; Invalidate(); }
    public void SetHovered(bool hovered) { IsHovered = hovered; Invalidate(); }
    public void SetActive(bool active) { IsActive = active; Invalidate(); }

    protected override L.Size MeasureCore(L.Size available) => new((Text?.Length ?? 0) + 4, 1);

    protected override void RenderCore(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        int x = (int)rect.X;
        int y = (int)rect.Y;
        int w = (int)rect.Width;
        int h = (int)rect.Height;
        var baseBg = ResolveBackground(Bg);
        var bg = IsEnabled ? (IsActive ? BgActive : (IsHovered ? BgHover : baseBg)) : BgDisabled;

        builder.PushClip(new DL.ClipPush(x, y, w, h));
        builder.DrawRect(new DL.Rect(x, y, w, h, bg));
        builder.DrawBorder(new DL.Border(x, y, w, h, "single", Border));
        // naive text placement with small padding
        int tx = x + 2;
        int ty = y; // single-line button; top aligned
        var attrs = IsEnabled ? (IsFocused ? DL.CellAttrFlags.Bold : DL.CellAttrFlags.None) : DL.CellAttrFlags.Dim;
        builder.DrawText(new DL.TextRun(tx, ty, Text, ResolveForeground(Fg), bg, attrs));
        builder.Pop();
    }
}
