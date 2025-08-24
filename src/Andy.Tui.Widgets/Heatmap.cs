using System;
using System.Collections.Generic;
using System.Linq;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class Heatmap
    {
        private readonly List<double> _values = new();
        private int _cols = 10;
        private DL.Rgb24 _low = new DL.Rgb24(30,30,80);
        private DL.Rgb24 _high = new DL.Rgb24(200,80,80);
        public void SetGrid(int cols) { _cols = Math.Max(1, cols); }
        public void SetValues(IEnumerable<double> v) { _values.Clear(); if (v!=null) _values.AddRange(v); }
        public void SetColors(DL.Rgb24 low, DL.Rgb24 high) { _low = low; _high = high; }

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x=(int)rect.X, y=(int)rect.Y, w=(int)rect.Width, h=(int)rect.Height;
            if (w<=0||h<=0||_values.Count==0) return;
            b.PushClip(new DL.ClipPush(x,y,w,h));
            b.DrawRect(new DL.Rect(x,y,w,h,new DL.Rgb24(0,0,0)));
            double min=_values.Min(), max=_values.Max(); if (Math.Abs(max-min)<1e-9){ max=min+1; }
            int rows = Math.Max(1, (int)Math.Ceiling(_values.Count/(double)_cols));
            int cellW = Math.Max(1, w/_cols);
            int cellH = Math.Max(1, h/rows);
            for (int i=0;i<_values.Count;i++)
            {
                int cx = i % _cols; int cy = i / _cols;
                int px = x + cx * cellW; int py = y + cy * cellH;
                double t = (_values[i]-min)/(max-min);
                var color = Lerp(_low,_high,t);
                b.DrawRect(new DL.Rect(px, py, Math.Min(cellW, x+w-px), Math.Min(cellH, y+h-py), color));
            }
            b.Pop();
        }

        private static DL.Rgb24 Lerp(DL.Rgb24 a, DL.Rgb24 b, double t)
        {
            byte r = (byte)(a.R + (b.R - a.R) * t);
            byte g = (byte)(a.G + (b.G - a.G) * t);
            byte bl = (byte)(a.B + (b.B - a.B) * t);
            return new DL.Rgb24(r,g,bl);
        }
    }
}
