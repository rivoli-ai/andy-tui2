using System;
using L = Andy.Tui.Layout;
using DL = Andy.Tui.DisplayList;

namespace Andy.Tui.Widgets
{
    public sealed class Toast
    {
        private string _text = string.Empty;
        private DL.Rgb24 _bg = new DL.Rgb24(30, 30, 30);
        private DL.Rgb24 _fg = new DL.Rgb24(230, 230, 230);
        private DL.Rgb24 _accent = new DL.Rgb24(255, 200, 0);
        private DateTime _showUntil = DateTime.MinValue;

        public void Show(string text, TimeSpan duration)
        {
            _text = text ?? string.Empty;
            _showUntil = DateTime.UtcNow + duration;
        }

        public bool IsVisible() => DateTime.UtcNow < _showUntil;

        public (int Width, int Height) Measure()
        {
            int w = _text.Length + 4; // padding
            return (w, 1);
        }

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            if (!IsVisible()) return;
            int x = (int)rect.X; int y = (int)rect.Y; int w = (int)rect.Width; int h = (int)rect.Height;
            if (w <= 0 || h <= 0) return;
            b.PushClip(new DL.ClipPush(x, y, w, h));
            b.DrawRect(new DL.Rect(x, y, w, h, _bg));
            b.DrawBorder(new DL.Border(x, y, w, h, "single", _accent));
            b.DrawText(new DL.TextRun(x + 2, y, _text, _fg, null, DL.CellAttrFlags.Bold));
            b.Pop();
        }
    }
}
