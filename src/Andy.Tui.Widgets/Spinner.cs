using System;
using L = Andy.Tui.Layout;
using DL = Andy.Tui.DisplayList;

namespace Andy.Tui.Widgets
{
    public sealed class Spinner
    {
        private static readonly char[] Frames = new[] { '|', '/', '-', '\\' };
        private int _index;
        private DL.Rgb24 _fg = new DL.Rgb24(200, 200, 200);
        private DL.Rgb24 _bg = new DL.Rgb24(20, 20, 20);

        public void Tick() { _index = (_index + 1) % Frames.Length; }
        public (int Width, int Height) Measure() => (1, 1);

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x = (int)rect.X; int y = (int)rect.Y; int w = (int)rect.Width; int h = (int)rect.Height;
            if (w <= 0 || h <= 0) return;
            b.PushClip(new DL.ClipPush(x, y, w, h));
            b.DrawRect(new DL.Rect(x, y, w, h, _bg));
            b.DrawText(new DL.TextRun(x, y, Frames[_index].ToString(), _fg, null, DL.CellAttrFlags.Bold));
            b.Pop();
        }
    }
}
