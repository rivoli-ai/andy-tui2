using System;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public enum DockRegion { Left, Right, Top, Bottom }

    public sealed class DockLayout
    {
        private (DockRegion region, int size, Action<L.Rect, DL.DisplayList, DL.DisplayListBuilder> render)[] _areas
            = Array.Empty<(DockRegion,int,Action<L.Rect, DL.DisplayList, DL.DisplayListBuilder>)>();
        private Action<L.Rect, DL.DisplayList, DL.DisplayListBuilder>? _center;
        private DL.Rgb24 _bg = new DL.Rgb24(0,0,0);

        public void SetBackground(DL.Rgb24 c) => _bg = c;
        public void SetRegions(params (DockRegion region, int size, Action<L.Rect, DL.DisplayList, DL.DisplayListBuilder> render)[] areas)
            => _areas = areas ?? Array.Empty<(DockRegion,int,Action<L.Rect, DL.DisplayList, DL.DisplayListBuilder>)>();
        public void SetCenter(Action<L.Rect, DL.DisplayList, DL.DisplayListBuilder> render) => _center = render;

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x = (int)rect.X; int y = (int)rect.Y; int w = (int)rect.Width; int h = (int)rect.Height;
            if (w <= 0 || h <= 0) return;
            b.PushClip(new DL.ClipPush(x, y, w, h));
            b.DrawRect(new DL.Rect(x, y, w, h, _bg));

            int cx = x, cy = y, cw = w, ch = h;
            // Apply top/bottom first, then left/right
            foreach (var (region, size, render) in _areas)
            {
                switch (region)
                {
                    case DockRegion.Top:
                        render(new L.Rect(cx, cy, cw, Math.Min(size, ch)), baseDl, b);
                        cy += size; ch = Math.Max(0, ch - size);
                        break;
                    case DockRegion.Bottom:
                        render(new L.Rect(cx, cy + Math.Max(0, ch - size), cw, Math.Min(size, ch)), baseDl, b);
                        ch = Math.Max(0, ch - size);
                        break;
                }
            }
            foreach (var (region, size, render) in _areas)
            {
                switch (region)
                {
                    case DockRegion.Left:
                        render(new L.Rect(cx, cy, Math.Min(size, cw), ch), baseDl, b);
                        cx += size; cw = Math.Max(0, cw - size);
                        break;
                    case DockRegion.Right:
                        render(new L.Rect(cx + Math.Max(0, cw - size), cy, Math.Min(size, cw), ch), baseDl, b);
                        cw = Math.Max(0, cw - size);
                        break;
                }
            }

            if (_center != null && cw > 0 && ch > 0)
                _center(new L.Rect(cx, cy, cw, ch), baseDl, b);

            b.Pop();
        }
    }
}
