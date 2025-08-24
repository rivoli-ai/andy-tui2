using System;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class Badge
    {
        private string _text = string.Empty;
        private DL.Rgb24 _bg = new DL.Rgb24(60,60,60);
        private DL.Rgb24 _fg = new DL.Rgb24(240,240,240);
        private DL.Rgb24 _border = new DL.Rgb24(120,120,120);
        public void SetText(string text) => _text = text ?? string.Empty;
        public void SetColors(DL.Rgb24 fg, DL.Rgb24 bg, DL.Rgb24 border) { _fg = fg; _bg = bg; _border = border; }
        public (int w,int h) Measure() => (_text.Length + 4, 3);
        public void RenderAt(int x, int y, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            var (w,h) = Measure();
            b.PushClip(new DL.ClipPush(x, y, w, h));
            b.DrawRect(new DL.Rect(x, y, w, h, _bg));
            b.DrawBorder(new DL.Border(x, y, w, h, "single", _border));
            b.DrawText(new DL.TextRun(x + 2, y + 1, _text, _fg, _bg, DL.CellAttrFlags.Bold));
            b.Pop();
        }
    }
}
