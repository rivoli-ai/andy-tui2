using System;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class Tooltip
    {
        private string _text = string.Empty;
        private DL.Rgb24 _bg = new DL.Rgb24(40,40,40);
        private DL.Rgb24 _fg = new DL.Rgb24(220,220,220);

        public void SetText(string text) => _text = text ?? string.Empty;
        public void SetColors(DL.Rgb24 fg, DL.Rgb24 bg) { _fg = fg; _bg = bg; }

        public (int w,int h) Measure() => (_text.Length + 2, 3);

        public void RenderAt(int x, int y, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            var (w,h) = Measure();
            b.PushClip(new DL.ClipPush(x, y, w, h));
            b.DrawRect(new DL.Rect(x, y, w, h, _bg));
            b.DrawBorder(new DL.Border(x, y, w, h, "single", new DL.Rgb24(120,120,120)));
            b.DrawText(new DL.TextRun(x + 1, y + 1, _text, _fg, _bg, DL.CellAttrFlags.None));
            b.Pop();
        }
    }
}
