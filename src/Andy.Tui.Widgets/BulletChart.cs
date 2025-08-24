using System;
using System.Collections.Generic;
using System.Linq;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class BulletChart
    {
        private double _value;
        private double _target;
        private double _min=0,_max=100;
        private DL.Rgb24 _bg = new DL.Rgb24(0,0,0);
        private DL.Rgb24 _range = new DL.Rgb24(60,60,60);
        private DL.Rgb24 _bar = new DL.Rgb24(200,200,80);
        private DL.Rgb24 _targetColor = new DL.Rgb24(220,80,80);
        public void SetRange(double min,double max){_min=min;_max=max;}
        public void SetValue(double v){_value=v;}
        public void SetTarget(double t){_target=t;}
        public void SetColors(DL.Rgb24 bar, DL.Rgb24 range, DL.Rgb24 target){_bar=bar;_range=range;_targetColor=target;}

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x=(int)rect.X, y=(int)rect.Y, w=(int)rect.Width, h=(int)rect.Height;
            if (w<=0||h<=0) return;
            b.PushClip(new DL.ClipPush(x,y,w,h));
            b.DrawRect(new DL.Rect(x,y,w,h,_bg));
            b.DrawRect(new DL.Rect(x,y,w,1,_range));
            double span=_max-_min; if (span<=0) span=1;
            int barW = (int)Math.Round((Math.Clamp(_value,_min,_max)-_min)/span * w);
            if (barW>0) b.DrawRect(new DL.Rect(x,y,Math.Min(barW,w),1,_bar));
            int tx = x + (int)Math.Round((Math.Clamp(_target,_min,_max)-_min)/span * w);
            b.DrawRect(new DL.Rect(Math.Min(tx,w-1), y, 1, 1, _targetColor));
            b.Pop();
        }
    }
}
