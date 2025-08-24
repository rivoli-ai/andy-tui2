using System;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public enum HorizontalAlign { Left, Center, Right, Stretch }
    public enum VerticalAlign { Top, Middle, Bottom, Stretch }

    public sealed class Align
    {
        private HorizontalAlign _h = HorizontalAlign.Left;
        private VerticalAlign _v = VerticalAlign.Top;
        private (int w,int h)? _childFixedSize;
        private Action<L.Rect, DL.DisplayList, DL.DisplayListBuilder>? _child;
        private DL.Rgb24 _bg = new DL.Rgb24(0,0,0);

        public void SetAlignment(HorizontalAlign h, VerticalAlign v) { _h = h; _v = v; }
        public void SetChild(Action<L.Rect, DL.DisplayList, DL.DisplayListBuilder> child) { _child = child; }
        public void SetChildFixedSize(int w, int h) { _childFixedSize = (Math.Max(0,w), Math.Max(0,h)); }
        public void ClearChildFixedSize() { _childFixedSize = null; }
        public void SetBackground(DL.Rgb24 color) { _bg = color; }

        public (int x,int y,int w,int h) ComputeChildRect(int x,int y,int w,int h)
        {
            int cw = _childFixedSize?.w ?? w;
            int ch = _childFixedSize?.h ?? h;
            if (_h == HorizontalAlign.Stretch) cw = w;
            if (_v == VerticalAlign.Stretch) ch = h;
            int cx = _h switch
            {
                HorizontalAlign.Left => x,
                HorizontalAlign.Center => x + Math.Max(0,(w - cw) / 2),
                HorizontalAlign.Right => x + Math.Max(0,w - cw),
                _ => x,
            };
            int cy = _v switch
            {
                VerticalAlign.Top => y,
                VerticalAlign.Middle => y + Math.Max(0,(h - ch) / 2),
                VerticalAlign.Bottom => y + Math.Max(0,h - ch),
                _ => y,
            };
            return (cx, cy, Math.Max(0,cw), Math.Max(0,ch));
        }

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x = (int)rect.X; int y = (int)rect.Y; int w = (int)rect.Width; int h = (int)rect.Height;
            if (w <= 0 || h <= 0) return;
            b.PushClip(new DL.ClipPush(x, y, w, h));
            b.DrawRect(new DL.Rect(x, y, w, h, _bg));
            if (_child != null)
            {
                var (cx, cy, cw, ch) = ComputeChildRect(x, y, w, h);
                _child(new L.Rect(cx, cy, cw, ch), baseDl, b);
            }
            b.Pop();
        }
    }
}
