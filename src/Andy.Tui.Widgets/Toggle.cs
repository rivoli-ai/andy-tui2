using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;
using ST = Andy.Tui.Style;

namespace Andy.Tui.Widgets;

public sealed class Toggle : WidgetBase, IThemeable, IStyleable
{
    public bool Checked { get; private set; }
    public string? Label { get; private set; }

    public override bool Focusable => true;
    public DL.Rgb24 Fg { get; private set; } = ST.ThemeContext.Current.GetRgb(ST.ThemeToken.Foreground, new DL.Rgb24(220, 220, 220));
    public DL.Rgb24 BgOn { get; private set; } = ST.ThemeContext.Current.GetRgb(ST.ThemeToken.Success, new DL.Rgb24(60, 120, 70));
    public DL.Rgb24 BgOff { get; private set; } = ST.ThemeContext.Current.GetRgb(ST.ThemeToken.ForegroundDisabled, new DL.Rgb24(80, 80, 80));
    public DL.Rgb24 Border { get; private set; } = ST.ThemeContext.Current.GetRgb(ST.ThemeToken.Border, new DL.Rgb24(100, 100, 100));

    /// <inheritdoc />
    public void ApplyTheme(ST.Theme theme)
    {
        Fg = theme.GetRgb(ST.ThemeToken.Foreground, new DL.Rgb24(220, 220, 220));
        BgOn = theme.GetRgb(ST.ThemeToken.Success, new DL.Rgb24(60, 120, 70));
        BgOff = theme.GetRgb(ST.ThemeToken.ForegroundDisabled, new DL.Rgb24(80, 80, 80));
        Border = theme.GetRgb(ST.ThemeToken.Border, new DL.Rgb24(100, 100, 100));
    }

    /// <inheritdoc />
    public void ApplyStyle(in ST.ResolvedStyle style)
    {
        if (style.Color.ToRgb24() is { } fg) Fg = fg;
    }

    public Toggle(bool initial = false, string? label = null)
    {
        Checked = initial;
        Label = label;
    }

    public void SetChecked(bool value) => Checked = value;
    public void ToggleChecked() => Checked = !Checked;
    public void SetLabel(string? text) => Label = text;

    protected override void RenderCore(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        int x = (int)rect.X;
        int y = (int)rect.Y;
        int w = (int)rect.Width;
        int h = (int)rect.Height;
        var bg = Checked ? BgOn : BgOff;
        builder.PushClip(new DL.ClipPush(x, y, w, h));
        builder.DrawRect(new DL.Rect(x, y, w, h, bg));
        builder.DrawBorder(new DL.Border(x, y, w, h, "single", Border));
        var text = Checked ? " ON " : " OFF";
        if (!string.IsNullOrEmpty(Label)) text = ($"{Label}:{text}");
        var attrs = IsFocused ? DL.CellAttrFlags.Bold : DL.CellAttrFlags.None;
        builder.DrawText(new DL.TextRun(x + 1, y, text, Fg, bg, attrs));
        builder.Pop();
    }
}
