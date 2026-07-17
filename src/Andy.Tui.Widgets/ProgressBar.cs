using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;
using ST = Andy.Tui.Style;

namespace Andy.Tui.Widgets;

public sealed class ProgressBar : WidgetBase
{
    private double _value; // 0..1
    public double Value { get => _value; set { _value = Math.Clamp(value, 0.0, 1.0); Invalidate(); } }
    public DL.Rgb24 Bg { get; private set; } = ST.ThemeContext.Current.GetRgb(ST.ThemeToken.Surface, new DL.Rgb24(40, 40, 40));
    public DL.Rgb24 Fill { get; private set; } = ST.ThemeContext.Current.GetRgb(ST.ThemeToken.Info, new DL.Rgb24(60, 140, 220));
    public DL.Rgb24 Border { get; private set; } = ST.ThemeContext.Current.GetRgb(ST.ThemeToken.Border, new DL.Rgb24(100, 100, 100));

    protected override L.Size MeasureCore(L.Size available) => new(available.Width > 0 ? available.Width : 10, 1);

    protected override void RenderCore(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        int x = (int)rect.X;
        int y = (int)rect.Y;
        int w = (int)rect.Width;
        int h = (int)rect.Height;
        var bg = ResolveBackground(Bg);
        builder.PushClip(new DL.ClipPush(x, y, w, h));
        builder.DrawRect(new DL.Rect(x, y, w, h, bg));
        int fillW = (int)Math.Round(w * Value);
        if (fillW > 0)
        {
            builder.DrawRect(new DL.Rect(x, y, Math.Min(fillW, w), h, Fill));
        }
        builder.DrawBorder(new DL.Border(x, y, w, h, "single", Border));
        var pct = (int)Math.Round(Value * 100);
        builder.DrawText(new DL.TextRun(x + 2, y, $"{pct}%", new DL.Rgb24(230, 230, 230), null, DL.CellAttrFlags.None));
        builder.Pop();
    }
}
