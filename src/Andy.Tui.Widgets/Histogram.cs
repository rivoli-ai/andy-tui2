using System;
using System.Collections.Generic;
using System.Linq;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class Histogram
    {
        private readonly List<double> _values = new();
        private int _bins = 10;
        private DL.Rgb24 _bar = new DL.Rgb24(200,200,80);
        public void SetValues(IEnumerable<double> v) { _values.Clear(); if (v!=null) _values.AddRange(v); }
        public void SetBins(int bins) { _bins = Math.Max(1, bins); }
        public void SetColor(DL.Rgb24 c) { _bar = c; }
        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x=(int)rect.X, y=(int)rect.Y, w=(int)rect.Width, h=(int)rect.Height;
            if (w<=0||h<=0||_values.Count==0) return;
            b.PushClip(new DL.ClipPush(x,y,w,h));
            b.DrawRect(new DL.Rect(x,y,w,h,new DL.Rgb24(0,0,0)));
            double min = _values.Min();
            double max = _values.Max();
            if (Math.Abs(max-min)<1e-9) { max=min+1; }
            int bins = Math.Min(_bins, w);
            var counts = new int[bins];
            foreach (var v in _values)
            {
                int bi = (int)Math.Floor(((v-min)/(max-min)) * (bins-1));
                counts[bi]++;
            }
            int maxCount = Math.Max(1, counts.Max());
            for (int i=0;i<bins;i++)
            {
                int barH = (int)Math.Round((counts[i]/(double)maxCount) * (h-1));
                if (barH>0) b.DrawRect(new DL.Rect(x+i, y + h - barH - 1, 1, barH, _bar));
            }
            b.Pop();
        }
    }
}
