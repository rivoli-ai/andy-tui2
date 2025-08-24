using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets;

public sealed class Panel
{
    public string? Title { get; private set; }
    public DL.Rgb24 Bg { get; private set; } = new DL.Rgb24(12, 12, 12);
    public DL.Rgb24 Border { get; private set; } = new DL.Rgb24(100, 100, 100);
    public DL.Rgb24 TitleColor { get; private set; } = new DL.Rgb24(200, 200, 200);

    public void SetTitle(string? title) => Title = title;

    public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        int x = (int)rect.X;
        int y = (int)rect.Y;
        int w = (int)rect.Width;
        int h = (int)rect.Height;
        builder.PushClip(new DL.ClipPush(x, y, w, h));
        builder.DrawRect(new DL.Rect(x, y, w, h, Bg));
        builder.DrawBorder(new DL.Border(x, y, w, h, "single", Border));
        if (!string.IsNullOrEmpty(Title))
        {
            builder.DrawText(new DL.TextRun(x + 2, y, Title!, TitleColor, Bg, DL.CellAttrFlags.Bold));
        }
        builder.Pop();
    }
}
