using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets;

public sealed class Button
{
    public string Text { get; private set; }
    public bool Enabled { get; private set; } = true;
    public bool IsHovered { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsFocused { get; private set; }

    // Simple style palette
    public DL.Rgb24 Fg { get; private set; } = new DL.Rgb24(220, 220, 220);
    public DL.Rgb24 Bg { get; private set; } = new DL.Rgb24(40, 40, 40);
    public DL.Rgb24 BgHover { get; private set; } = new DL.Rgb24(55, 55, 55);
    public DL.Rgb24 BgActive { get; private set; } = new DL.Rgb24(80, 80, 120);
    public DL.Rgb24 BgDisabled { get; private set; } = new DL.Rgb24(30, 30, 30);
    public DL.Rgb24 Border { get; private set; } = new DL.Rgb24(100, 100, 100);

    public Button(string text) => Text = text;

    public void SetText(string text) => Text = text;
    public void SetEnabled(bool enabled) => Enabled = enabled;
    public void SetHovered(bool hovered) => IsHovered = hovered;
    public void SetActive(bool active) => IsActive = active;
    public void SetFocused(bool focused) => IsFocused = focused;

    public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        int x = (int)rect.X;
        int y = (int)rect.Y;
        int w = (int)rect.Width;
        int h = (int)rect.Height;
        var bg = Enabled ? (IsActive ? BgActive : (IsHovered ? BgHover : Bg)) : BgDisabled;

        builder.PushClip(new DL.ClipPush(x, y, w, h));
        builder.DrawRect(new DL.Rect(x, y, w, h, bg));
        builder.DrawBorder(new DL.Border(x, y, w, h, "single", Border));
        // naive text placement with small padding
        int tx = x + 2;
        int ty = y; // single-line button; top aligned
        var attrs = Enabled ? (IsFocused ? DL.CellAttrFlags.Bold : DL.CellAttrFlags.None) : DL.CellAttrFlags.Dim;
        builder.DrawText(new DL.TextRun(tx, ty, Text, Fg, bg, attrs));
        builder.Pop();
    }
}
