using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets;

public sealed class Checkbox
{
    public bool Checked { get; private set; }
    public string Text { get; private set; }
    public DL.Rgb24 Fg { get; private set; } = new DL.Rgb24(220, 220, 220);
    public DL.Rgb24 Bg { get; private set; } = new DL.Rgb24(40, 40, 40);
    public DL.Rgb24 Border { get; private set; } = new DL.Rgb24(100, 100, 100);

    public Checkbox(string text, bool initial = false)
    {
        Text = text;
        Checked = initial;
    }

    public void SetChecked(bool value) => Checked = value;
    public void ToggleChecked() => Checked = !Checked;
    public void SetText(string text) => Text = text;

    public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        int x = (int)rect.X;
        int y = (int)rect.Y;
        int h = (int)rect.Height;
        builder.PushClip(new DL.ClipPush(x, y, (int)rect.Width, h));
        builder.DrawRect(new DL.Rect(x, y, (int)rect.Width, h, Bg));
        builder.DrawBorder(new DL.Border(x, y, (int)rect.Width, h, "single", Border));
        var mark = Checked ? "[x]" : "[ ]";
        builder.DrawText(new DL.TextRun(x + 1, y, $"{mark} {Text}", Fg, Bg, DL.CellAttrFlags.None));
        builder.Pop();
    }
}
