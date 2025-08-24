using System;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class ColorChooser
    {
        private int _hueSteps = 12;
        private int _sel = 0;
        public void SetHueSteps(int steps) { _hueSteps = System.Math.Max(1, steps); }
        public int GetSelectedIndex() => _sel;
        public void Move(int delta) { _sel = (_sel + delta + _hueSteps) % _hueSteps; }

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x=(int)rect.X, y=(int)rect.Y, w=(int)rect.Width, h=(int)rect.Height; if (w<=0||h<=0) return;
            b.PushClip(new DL.ClipPush(x,y,w,h));
            b.DrawRect(new DL.Rect(x,y,w,h,new DL.Rgb24(0,0,0)));
            int segW = System.Math.Max(1, w/_hueSteps);
            for (int i=0;i<_hueSteps;i++)
            {
                var c = HsvToRgb(i/(double)_hueSteps, 1, 1);
                int px = x + i*segW;
                b.DrawRect(new DL.Rect(px, y, System.Math.Min(segW, x+w-px), h, c));
                if (i==_sel)
                {
                    b.DrawRect(new DL.Rect(px, y, System.Math.Min(segW, x+w-px), 1, new DL.Rgb24(255,255,255)));
                    b.DrawRect(new DL.Rect(px, y+h-1, System.Math.Min(segW, x+w-px), 1, new DL.Rgb24(255,255,255)));
                }
            }
            b.Pop();
        }

        private static DL.Rgb24 HsvToRgb(double h, double s, double v)
        {
            double r=0,g=0,b=0; int i = (int)System.Math.Floor(h*6); double f=h*6 - i; double p=v*(1-s), q=v*(1-f*s), t=v*(1-(1-f)*s);
            switch (i%6)
            {
                case 0: r=v; g=t; b=p; break;
                case 1: r=q; g=v; b=p; break;
                case 2: r=p; g=v; b=t; break;
                case 3: r=p; g=q; b=v; break;
                case 4: r=t; g=p; b=v; break;
                case 5: r=v; g=p; b=q; break;
            }
            return new DL.Rgb24((byte)(r*255),(byte)(g*255),(byte)(b*255));
        }
    }
}
