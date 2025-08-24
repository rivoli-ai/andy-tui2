using System;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class GroupBox
    {
        private string _title = string.Empty;
        private DL.Rgb24 _border = new DL.Rgb24(150, 150, 150);
        private DL.Rgb24 _titleFg = new DL.Rgb24(220, 220, 220);
        private DL.Rgb24 _bg = new DL.Rgb24(0, 0, 0);
        private Action<L.Rect, DL.DisplayList, DL.DisplayListBuilder>? _content;

        // Padding inside the border for content
        private int _padLeft = 1, _padTop = 1, _padRight = 1, _padBottom = 1;

        public void SetTitle(string title) => _title = title ?? string.Empty;
        public void SetBorderColor(DL.Rgb24 color) => _border = color;
        public void SetTitleColor(DL.Rgb24 color) => _titleFg = color;
        public void SetBackground(DL.Rgb24 color) => _bg = color;
        public void SetPadding(int left, int top, int right, int bottom)
        { _padLeft = Math.Max(0, left); _padTop = Math.Max(0, top); _padRight = Math.Max(0, right); _padBottom = Math.Max(0, bottom); }
        public void SetContent(Action<L.Rect, DL.DisplayList, DL.DisplayListBuilder>? render) => _content = render;

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x = (int)rect.X; int y = (int)rect.Y; int w = (int)rect.Width; int h = (int)rect.Height;
            if (w <= 0 || h <= 0) return;
            b.PushClip(new DL.ClipPush(x, y, w, h));
            b.DrawRect(new DL.Rect(x, y, w, h, _bg));

            // Border
            b.DrawBorder(new DL.Border(x, y, w, h, "single", _border));

            // Title text rendered on the top border with a small background wipe
            if (!string.IsNullOrEmpty(_title) && w >= 4)
            {
                string decorated = $" {_title} ";
                int tx = x + 2; // after the top-left corner + one horizontal line
                int maxTitle = Math.Max(0, w - 4);
                if (decorated.Length > maxTitle) decorated = decorated.Substring(0, maxTitle);
                // Wipe a background on the top border where the title sits
                b.DrawRect(new DL.Rect(tx, y, decorated.Length, 1, _bg));
                // Draw title text
                b.DrawText(new DL.TextRun(tx, y, decorated, _titleFg, _bg, DL.CellAttrFlags.Bold));
            }

            // Content area inside padding
            int cx = x + 1 + _padLeft;
            int cy = y + 1 + _padTop;
            int cw = Math.Max(0, w - 2 - _padLeft - _padRight);
            int ch = Math.Max(0, h - 2 - _padTop - _padBottom);
            if (cw > 0 && ch > 0 && _content != null)
            {
                var contentRect = new L.Rect(cx, cy, cw, ch);
                _content(contentRect, baseDl, b);
            }

            b.Pop();
        }
    }
}
