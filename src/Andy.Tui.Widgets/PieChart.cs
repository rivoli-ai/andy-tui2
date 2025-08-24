using System;
using System.Collections.Generic;
using System.Linq;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class PieChart
    {
        private readonly List<(string Label,double Value, DL.Rgb24 Color)> _slices = new();
        public void SetSlices(IEnumerable<(string Label,double Value, DL.Rgb24 Color)> slices)
        {
            _slices.Clear(); if (slices!=null) _slices.AddRange(slices);
        }

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            // Approximate pie with horizontal bands (not circular due to text-cell rendering); represent proportions along width.
            int x=(int)rect.X, y=(int)rect.Y, w=(int)rect.Width, h=(int)rect.Height;
            if (w<=0||h<=0||_slices.Count==0) return;
            b.PushClip(new DL.ClipPush(x,y,w,h));
            b.DrawRect(new DL.Rect(x,y,w,h,new DL.Rgb24(0,0,0)));
            double total = _slices.Sum(s=>Math.Max(0,s.Value)); if (total<=0) total=1;
            int cy = y;
            foreach (var (label,val,color) in _slices)
            {
                int ww = (int)Math.Round((val/total) * w);
                if (ww<=0) continue;
                b.DrawRect(new DL.Rect(x, cy, Math.Min(ww,w), 1, color));
                string cap = $" {label} ";
                if (cap.Length < ww)
                    b.DrawText(new DL.TextRun(x + 1, cy, cap, new DL.Rgb24(0,0,0), color, DL.CellAttrFlags.Bold));
                cy++;
                if (cy>=y+h) break;
            }
            b.Pop();
        }
    }
}
