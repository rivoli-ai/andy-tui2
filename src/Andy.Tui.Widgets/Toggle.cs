using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets;

public sealed class Toggle
{
    public bool Checked { get; private set; }
    public string? Label { get; private set; }
    public bool Focused { get; private set; }
    public DL.Rgb24 Fg { get; private set; } = new DL.Rgb24(220, 220, 220);
    public DL.Rgb24 BgOn { get; private set; } = new DL.Rgb24(60, 120, 70);
    public DL.Rgb24 BgOff { get; private set; } = new DL.Rgb24(80, 80, 80);
    public DL.Rgb24 Border { get; private set; } = new DL.Rgb24(100, 100, 100);

    public Toggle(bool initial = false, string? label = null)
    {
        Checked = initial;
        Label = label;
    }

    public void SetChecked(bool value) => Checked = value;
    public void ToggleChecked() => Checked = !Checked;
    public void SetLabel(string? text) => Label = text;
    public void SetFocused(bool f) => Focused = f;

    public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
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
        var attrs = Focused ? DL.CellAttrFlags.Bold : DL.CellAttrFlags.None;
        builder.DrawText(new DL.TextRun(x + 1, y, text, Fg, bg, attrs));
        builder.Pop();
    }
}
