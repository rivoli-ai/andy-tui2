using System;
using System.Collections.Generic;
using System.Linq;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class LineChart
    {
        private readonly List<double> _values = new();
        private DL.Rgb24 _line = new DL.Rgb24(200,200,80);
        private DL.Rgb24 _area = new DL.Rgb24(50,50,20);
        private bool _fillArea;
        public void SetValues(IEnumerable<double> values) { _values.Clear(); if (values!=null) _values.AddRange(values); }
        public void SetColors(DL.Rgb24 line, DL.Rgb24 area) { _line = line; _area = area; }
        public void SetFillArea(bool fill) { _fillArea = fill; }

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x=(int)rect.X, y=(int)rect.Y, w=(int)rect.Width, h=(int)rect.Height;
            if (w<=1||h<=1||_values.Count==0) return;
            b.PushClip(new DL.ClipPush(x,y,w,h));
            b.DrawRect(new DL.Rect(x,y,w,h,new DL.Rgb24(0,0,0)));
            double min = _values.Min();
            double max = _values.Max();
            if (Math.Abs(max-min)<1e-9) { max=min+1; }
            int points = Math.Min(w, _values.Count);
            for (int i=0;i<points;i++)
            {
                double v = _values[_values.Count - points + i];
                int px = x + i;
                int py = y + (int)Math.Round((1.0 - (v-min)/(max-min)) * (h-1));
                if (_fillArea)
                {
                    int ay0 = Math.Min(py, y + h - 1);
                    int ay1 = y + h - 1;
                    b.DrawRect(new DL.Rect(px, ay0, 1, ay1 - ay0 + 1, _area));
                }
                b.DrawRect(new DL.Rect(px, py, 1, 1, _line));
            }
            b.Pop();
        }
    }
}
