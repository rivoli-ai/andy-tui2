using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets;

public sealed class TextInput
{
    public bool ShowCaret { get; private set; } = true;
    public string Text { get; private set; } = string.Empty;
    public int Cursor { get; private set; }
    public bool Focused { get; private set; }
    public DL.Rgb24 Fg { get; private set; } = new DL.Rgb24(220, 220, 220);
    public DL.Rgb24 Bg { get; private set; } = new DL.Rgb24(20, 20, 20);
    public DL.Rgb24 Border { get; private set; } = new DL.Rgb24(100, 100, 100);

    public void SetText(string text) { Text = text; Cursor = Math.Min(Cursor, Text.Length); }
    public void SetCursor(int pos) => Cursor = Math.Clamp(pos, 0, Text.Length);
    public void SetFocused(bool f) => Focused = f;
    public void SetShowCaret(bool show) => ShowCaret = show;

    public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        int x = (int)rect.X;
        int y = (int)rect.Y;
        int w = (int)rect.Width;
        int h = (int)rect.Height;
        builder.PushClip(new DL.ClipPush(x, y, w, h));
        builder.DrawRect(new DL.Rect(x, y, w, h, Bg));
        builder.DrawBorder(new DL.Border(x, y, w, h, "single", Border));
        int maxChars = Math.Max(0, w - 2);
        var shown = (Text.Length <= maxChars) ? Text : Text.Substring(0, maxChars);
        var attrs = Focused ? DL.CellAttrFlags.Bold : DL.CellAttrFlags.None;
        builder.DrawText(new DL.TextRun(x + 1, y, shown, Fg, Bg, attrs));
        // caret option when focused
        if (Focused && ShowCaret)
        {
            int caretX = x + 1 + Math.Min(Cursor, maxChars);
            if (caretX < x + w - 1)
            {
                builder.DrawText(new DL.TextRun(caretX, y, "|", new DL.Rgb24(240, 240, 240), Bg, DL.CellAttrFlags.None));
            }
        }
        builder.Pop();
    }
}
