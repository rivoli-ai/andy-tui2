using System;
using System.Collections.Generic;
using System.Linq;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class Candlestick
    {
        public readonly struct Candle
        {
            public readonly double Open, High, Low, Close;
            public Candle(double o,double h,double l,double c){Open=o;High=h;Low=l;Close=c;}
        }
        private readonly List<Candle> _data = new();
        private DL.Rgb24 _up = new DL.Rgb24(80,200,120);
        private DL.Rgb24 _down = new DL.Rgb24(220,80,80);
        public void SetSeries(IEnumerable<Candle> series){ _data.Clear(); if (series!=null) _data.AddRange(series); }
        public void SetColors(DL.Rgb24 up, DL.Rgb24 down){_up=up;_down=down;}

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x=(int)rect.X, y=(int)rect.Y, w=(int)rect.Width, h=(int)rect.Height;
            if (w<=0||h<=0||_data.Count==0) return;
            b.PushClip(new DL.ClipPush(x,y,w,h));
            b.DrawRect(new DL.Rect(x,y,w,h,new DL.Rgb24(0,0,0)));
            double vmin = _data.Min(c=>c.Low), vmax = _data.Max(c=>c.High);
            if (System.Math.Abs(vmax-vmin)<1e-9){ vmax=vmin+1; }
            int count = System.Math.Min(w, _data.Count);
            for (int i=0;i<count;i++)
            {
                var c = _data[_data.Count - count + i];
                int px = x + i;
                int yLow = Map(c.Low), yHigh = Map(c.High), yOpen = Map(c.Open), yClose = Map(c.Close);
                var color = c.Close >= c.Open ? _up : _down;
                // wick
                b.DrawRect(new DL.Rect(px, System.Math.Min(yLow,yHigh), 1, System.Math.Abs(yHigh-yLow)+1, color));
                // body
                int bodyY = System.Math.Min(yOpen,yClose);
                int bodyH = System.Math.Max(1, System.Math.Abs(yClose - yOpen));
                b.DrawRect(new DL.Rect(px, bodyY, 1, bodyH, color));
            }
            b.Pop();

            int Map(double v) => y + (int)System.Math.Round((1.0 - (v - vmin)/(vmax - vmin)) * (h-1));
        }
    }
}
