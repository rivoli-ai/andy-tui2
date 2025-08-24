using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets;

public sealed class Label
{
    public string Text { get; private set; }
    public DL.Rgb24 Fg { get; private set; }
    public DL.Rgb24? Bg { get; private set; }
    public DL.CellAttrFlags Attrs { get; private set; }

    public Label(string text)
    {
        Text = text;
        Fg = new DL.Rgb24(200, 200, 200);
        Bg = null;
        Attrs = DL.CellAttrFlags.None;
    }

    public void SetText(string text) => Text = text;
    public void SetForeground(DL.Rgb24 fg) => Fg = fg;
    public void SetBackground(DL.Rgb24? bg) => Bg = bg;
    public void SetAttrs(DL.CellAttrFlags attrs) => Attrs = attrs;

    public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        var x = (int)rect.X;
        var y = (int)rect.Y;
        var width = (int)rect.Width;
        builder.PushClip(new DL.ClipPush(x, y, width, (int)rect.Height));
        builder.DrawText(new DL.TextRun(x, y, Text, Fg, Bg, Attrs));
        builder.Pop();
    }
}
