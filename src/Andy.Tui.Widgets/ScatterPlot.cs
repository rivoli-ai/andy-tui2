using System;
using System.Collections.Generic;
using System.Linq;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class ScatterPlot
    {
        private readonly List<(double X,double Y)> _points = new();
        private DL.Rgb24 _point = new DL.Rgb24(200,200,80);
        private DL.Rgb24 _bg = new DL.Rgb24(0,0,0);
        public void SetPoints(IEnumerable<(double X,double Y)> pts) { _points.Clear(); if (pts!=null) _points.AddRange(pts); }
        public void SetColors(DL.Rgb24 point, DL.Rgb24 bg) { _point = point; _bg = bg; }

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x=(int)rect.X, y=(int)rect.Y, w=(int)rect.Width, h=(int)rect.Height;
            if (w<=0||h<=0||_points.Count==0) return;
            b.PushClip(new DL.ClipPush(x,y,w,h));
            b.DrawRect(new DL.Rect(x,y,w,h,_bg));
            double minX=_points.Min(p=>p.X), maxX=_points.Max(p=>p.X);
            double minY=_points.Min(p=>p.Y), maxY=_points.Max(p=>p.Y);
            if (Math.Abs(maxX-minX)<1e-9) { maxX=minX+1; }
            if (Math.Abs(maxY-minY)<1e-9) { maxY=minY+1; }
            foreach (var p in _points)
            {
                int px = x + (int)Math.Round(((p.X-minX)/(maxX-minX)) * (w-1));
                int py = y + (int)Math.Round((1.0 - (p.Y-minY)/(maxY-minY)) * (h-1));
                b.DrawRect(new DL.Rect(px, py, 1, 1, _point));
            }
            b.Pop();
        }
    }
}
