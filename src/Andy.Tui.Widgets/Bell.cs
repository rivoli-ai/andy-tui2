using System;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class Bell
    {
        private string _text = "Notification";
        private int _ttlFrames = 60; // ~2 seconds at 30 fps
        private DL.Rgb24 _fg = new DL.Rgb24(255,255,255);
        private DL.Rgb24 _bg = new DL.Rgb24(80,30,30);

        public void Show(string text, int ttlFrames = 60)
        { _text = text ?? string.Empty; _ttlFrames = ttlFrames; }

        public void Tick() { if (_ttlFrames > 0) _ttlFrames--; }
        public bool IsVisible => _ttlFrames > 0;

        public void RenderAt(int x, int y, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            if (!IsVisible) return;
            int w = Math.Max(8, _text.Length + 4);
            b.PushClip(new DL.ClipPush(x, y, w, 1));
            b.DrawRect(new DL.Rect(x, y, w, 1, _bg));
            b.DrawText(new DL.TextRun(x+2, y, _text, _fg, _bg, DL.CellAttrFlags.Bold));
            b.Pop();
        }
    }
}
