using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;
using ST = Andy.Tui.Style;
using IN = Andy.Tui.Input;

namespace Andy.Tui.Widgets;

public sealed class Checkbox : WidgetBase
{
    public bool Checked { get; private set; }
    public string Text { get; private set; }
    public DL.Rgb24 Fg { get; private set; } = ST.ThemeContext.Current.GetRgb(ST.ThemeToken.Foreground, new DL.Rgb24(220, 220, 220));
    public DL.Rgb24 Bg { get; private set; } = ST.ThemeContext.Current.GetRgb(ST.ThemeToken.Surface, new DL.Rgb24(40, 40, 40));
    public DL.Rgb24 Border { get; private set; } = ST.ThemeContext.Current.GetRgb(ST.ThemeToken.Border, new DL.Rgb24(100, 100, 100));

    public override bool Focusable => true;

    public Checkbox(string text, bool initial = false)
    {
        Text = text;
        Checked = initial;
    }

    public void SetChecked(bool value) { Checked = value; Invalidate(); }
    public void ToggleChecked() { Checked = !Checked; Invalidate(); }
    public void SetText(string text) { Text = text; Invalidate(); }

    protected override L.Size MeasureCore(L.Size available) => new((Text?.Length ?? 0) + 4, 1);

    protected override bool HandleInputCore(IN.IInputEvent ev)
    {
        if (ev is IN.KeyEvent key && (key.Key == " " || key.Key == "Enter" || key.Key == "Return"))
        {
            ToggleChecked();
            return true;
        }
        return false;
    }

    protected override void RenderCore(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        int x = (int)rect.X;
        int y = (int)rect.Y;
        int h = (int)rect.Height;
        var bg = ResolveBackground(Bg);
        builder.PushClip(new DL.ClipPush(x, y, (int)rect.Width, h));
        builder.DrawRect(new DL.Rect(x, y, (int)rect.Width, h, bg));
        builder.DrawBorder(new DL.Border(x, y, (int)rect.Width, h, "single", Border));
        var mark = Checked ? "[x]" : "[ ]";
        builder.DrawText(new DL.TextRun(x + 1, y, $"{mark} {Text}", ResolveForeground(Fg), bg, DL.CellAttrFlags.None));
        builder.Pop();
    }
}
