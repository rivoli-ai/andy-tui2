using System;
using System.Collections.Generic;
using System.Linq;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class BoxPlot
    {
        private readonly List<double> _series = new();
        private DL.Rgb24 _fg = new DL.Rgb24(200,200,80);
        private DL.Rgb24 _bg = new DL.Rgb24(0,0,0);
        public void SetSeries(IEnumerable<double> v) { _series.Clear(); if (v!=null) _series.AddRange(v); }
        public void SetColors(DL.Rgb24 fg, DL.Rgb24 bg) { _fg = fg; _bg = bg; }
        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x=(int)rect.X, y=(int)rect.Y, w=(int)rect.Width, h=(int)rect.Height;
            if (w<=2||h<=2||_series.Count==0) return;
            b.PushClip(new DL.ClipPush(x,y,w,h));
            b.DrawRect(new DL.Rect(x,y,w,h,_bg));
            var data = _series.OrderBy(v=>v).ToList();
            double q1 = Quantile(data, 0.25);
            double q2 = Quantile(data, 0.50);
            double q3 = Quantile(data, 0.75);
            double min = data.First();
            double max = data.Last();
            double loWhisker = data.Where(v => v>= q1 - 1.5*(q3-q1)).DefaultIfEmpty(min).Min();
            double hiWhisker = data.Where(v => v<= q3 + 1.5*(q3-q1)).DefaultIfEmpty(max).Max();
            // map value to y
            double vmin = data.Min(); double vmax = data.Max(); if (Math.Abs(vmax-vmin)<1e-9){ vmax=vmin+1; }
            int Map(double v) => y + (int)Math.Round((1.0 - (v - vmin)/(vmax - vmin)) * (h-1));
            int xmid = x + w/2;
            int yq1 = Map(q1), yq2 = Map(q2), yq3 = Map(q3), yLo = Map(loWhisker), yHi = Map(hiWhisker);
            // box
            int boxH = Math.Abs(yq3 - yq1) + 1;
            int boxY = Math.Min(yq1, yq3);
            int boxW = Math.Max(3, w/3);
            int boxX = xmid - boxW/2;
            b.DrawRect(new DL.Rect(boxX, boxY, boxW, boxH, new DL.Rgb24(30,30,10)));
            b.DrawRect(new DL.Rect(boxX, boxY, boxW, 1, _fg));
            b.DrawRect(new DL.Rect(boxX, boxY + boxH - 1, boxW, 1, _fg));
            b.DrawRect(new DL.Rect(boxX, boxY, 1, boxH, _fg));
            b.DrawRect(new DL.Rect(boxX + boxW - 1, boxY, 1, boxH, _fg));
            // median
            b.DrawRect(new DL.Rect(boxX, yq2, boxW, 1, _fg));
            // whiskers
            b.DrawRect(new DL.Rect(xmid, yHi, 1, Math.Abs(yHi - boxY), _fg));
            b.DrawRect(new DL.Rect(xmid, boxY + boxH - 1, 1, Math.Abs((boxY + boxH - 1) - yLo), _fg));
            // whisker caps
            b.DrawRect(new DL.Rect(xmid - boxW/4, yHi, boxW/2, 1, _fg));
            b.DrawRect(new DL.Rect(xmid - boxW/4, yLo, boxW/2, 1, _fg));
            b.Pop();
        }
        private static double Quantile(IReadOnlyList<double> sorted, double p)
        {
            if (sorted.Count==0) return 0; if (p<=0) return sorted.First(); if (p>=1) return sorted.Last();
            double pos = (sorted.Count - 1) * p;
            int lo = (int)Math.Floor(pos); int hi = (int)Math.Ceiling(pos);
            if (lo==hi) return sorted[lo];
            double frac = pos - lo;
            return sorted[lo] * (1 - frac) + sorted[hi] * frac;
        }
    }
}
