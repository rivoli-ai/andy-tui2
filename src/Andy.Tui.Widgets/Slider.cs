using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets;

public sealed class Slider
{
    private double _value; // 0..1
    public double Value { get => _value; set => _value = Math.Clamp(value, 0.0, 1.0); }
    public DL.Rgb24 Bg { get; private set; } = new DL.Rgb24(20, 20, 20);
    public DL.Rgb24 Track { get; private set; } = new DL.Rgb24(60, 60, 60);
    public DL.Rgb24 Thumb { get; private set; } = new DL.Rgb24(200, 200, 200);
    public DL.Rgb24 Border { get; private set; } = new DL.Rgb24(100, 100, 100);

    public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        int x = (int)rect.X;
        int y = (int)rect.Y;
        int w = (int)rect.Width;
        int h = (int)rect.Height;
        builder.PushClip(new DL.ClipPush(x, y, w, h));
        builder.DrawRect(new DL.Rect(x, y, w, h, Bg));
        // track
        builder.DrawRect(new DL.Rect(x + 1, y, w - 2, 1, Track));
        // thumb position
        // Use MidpointAwayFromZero to match test expectation at 50% -> center bias
        int thumbX = x + 1 + (int)Math.Round((w - 3) * Value, MidpointRounding.AwayFromZero);
        builder.DrawText(new DL.TextRun(thumbX, y, "|", Thumb, Bg, DL.CellAttrFlags.Bold));
        builder.DrawBorder(new DL.Border(x, y, w, h, "single", Border));
        builder.Pop();
    }
}
