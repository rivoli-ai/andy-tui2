using System;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class StatusBar
    {
        private string _left = string.Empty;
        private string _center = string.Empty;
        private string _right = string.Empty;
        private DL.Rgb24 _bg = new DL.Rgb24(30,30,30);
        private DL.Rgb24 _fg = new DL.Rgb24(220,220,220);

        public void SetText(string left, string? center = null, string? right = null)
        { _left = left ?? string.Empty; _center = center ?? string.Empty; _right = right ?? string.Empty; }
        public void SetColors(DL.Rgb24 fg, DL.Rgb24 bg) { _fg = fg; _bg = bg; }

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x = (int)rect.X; int y = (int)rect.Y; int w = (int)rect.Width; int h = (int)rect.Height;
            if (w <= 0 || h <= 0) return;
            b.PushClip(new DL.ClipPush(x, y, w, h));
            b.DrawRect(new DL.Rect(x, y, w, h, _bg));
            // Pack left and right; center in remaining space
            string left = _left;
            string right = _right;
            int leftW = Math.Min(left.Length, Math.Max(0, w));
            int rightW = Math.Min(right.Length, Math.Max(0, w - leftW));
            int centerAvail = Math.Max(0, w - leftW - rightW);
            string center = _center;
            if (center.Length > centerAvail)
                center = center.Substring(0, centerAvail);
            int centerX = x + leftW + Math.Max(0, (centerAvail - center.Length) / 2);
            if (leftW > 0)
                b.DrawText(new DL.TextRun(x, y, left.Substring(0,leftW), _fg, _bg, DL.CellAttrFlags.None));
            if (center.Length > 0)
                b.DrawText(new DL.TextRun(centerX, y, center, _fg, _bg, DL.CellAttrFlags.None));
            if (rightW > 0)
                b.DrawText(new DL.TextRun(x + Math.Max(0, w - rightW), y, right.Substring(0,rightW), _fg, _bg, DL.CellAttrFlags.None));
            b.Pop();
        }
    }
}
