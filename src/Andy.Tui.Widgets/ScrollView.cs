using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets;

public sealed class ScrollView
{
    public int ScrollY { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DL.Rgb24 Fg { get; private set; } = new DL.Rgb24(220, 220, 220);
    public DL.Rgb24 Bg { get; private set; } = new DL.Rgb24(10, 10, 10);
    public DL.Rgb24 Border { get; private set; } = new DL.Rgb24(100, 100, 100);

    public void SetContent(string content) => Content = content;
    public void SetScrollY(int y) => ScrollY = Math.Max(0, y);
    public void AdjustScroll(int delta, int viewportHeight)
    {
        var lines = Content.Split('\n');
        int maxStart = Math.Max(0, lines.Length - viewportHeight);
        ScrollY = Math.Clamp(ScrollY + delta, 0, maxStart);
    }
    public void OnMouseWheel(int wheelDelta, in L.Rect rect)
    {
        // wheelDelta: +1 up, -1 down (as decoded)
        AdjustScroll(-wheelDelta, (int)rect.Height);
    }

    public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        int x = (int)rect.X;
        int y = (int)rect.Y;
        int w = (int)rect.Width;
        int h = (int)rect.Height;
        builder.PushClip(new DL.ClipPush(x, y, w, h));
        builder.DrawRect(new DL.Rect(x, y, w, h, Bg));
        builder.DrawBorder(new DL.Border(x, y, w, h, "single", Border));
        var lines = Content.Split('\n');
        int start = Math.Min(ScrollY, Math.Max(0, lines.Length - 1));
        int yy = y;
        for (int i = start; i < lines.Length && yy < y + h; i++, yy++)
        {
            var s = lines[i];
            if (s.Length > w - 2) s = s.Substring(0, Math.Max(0, w - 2));
            builder.DrawText(new DL.TextRun(x + 1, yy, s, Fg, Bg, DL.CellAttrFlags.None));
        }
        builder.Pop();
    }
}
