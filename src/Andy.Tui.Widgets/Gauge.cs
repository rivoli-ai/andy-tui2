using System;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class Gauge
    {
        private double _value;
        private double _min=0,_max=100;
        private DL.Rgb24 _bg = new DL.Rgb24(0,0,0);
        private DL.Rgb24 _track = new DL.Rgb24(60,60,60);
        private DL.Rgb24 _fill = new DL.Rgb24(80,160,240);
        public void SetRange(double min,double max){_min=min;_max=max;}
        public void SetValue(double v){_value=v;}
        public void SetColors(DL.Rgb24 track, DL.Rgb24 fill){_track=track;_fill=fill;}
        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x=(int)rect.X, y=(int)rect.Y, w=(int)rect.Width, h=(int)rect.Height;
            if (w<=0||h<=0) return;
            b.PushClip(new DL.ClipPush(x,y,w,h));
            b.DrawRect(new DL.Rect(x,y,w,h,_bg));
            int mid = y + h/2;
            b.DrawRect(new DL.Rect(x, mid, w, 1, _track));
            double span=_max-_min; if (span<=0) span=1;
            int fillW = (int)System.Math.Round((System.Math.Clamp(_value,_min,_max)-_min)/span * w);
            if (fillW>0) b.DrawRect(new DL.Rect(x, mid, fillW, 1, _fill));
            b.Pop();
        }
    }
}
