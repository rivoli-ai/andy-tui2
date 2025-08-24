using System;
using System.Collections.Generic;
using System.Linq;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class GanttChart
    {
        public readonly struct TaskItem
        {
            public readonly string Label; public readonly int Start; public readonly int Duration; public readonly DL.Rgb24 Color;
            public TaskItem(string label, int start, int duration, DL.Rgb24? color = null)
            { Label = label; Start = start; Duration = duration; Color = color ?? new DL.Rgb24(80,160,240); }
        }
        private readonly List<TaskItem> _tasks = new();
        private int _horizon = 30;
        private DL.Rgb24 _bg = new DL.Rgb24(0,0,0);
        private DL.Rgb24 _axis = new DL.Rgb24(80,80,80);
        private DL.Rgb24 _label = new DL.Rgb24(200,200,200);
        public void SetHorizon(int days) { _horizon = Math.Max(1, days); }
        public void SetTasks(IEnumerable<TaskItem> tasks) { _tasks.Clear(); if (tasks!=null) _tasks.AddRange(tasks); }

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x=(int)rect.X, y=(int)rect.Y, w=(int)rect.Width, h=(int)rect.Height;
            if (w<=0||h<=0) return;
            b.PushClip(new DL.ClipPush(x,y,w,h));
            b.DrawRect(new DL.Rect(x,y,w,h,_bg));
            int labelW = Math.Min(12, Math.Max(6, w/4));
            int chartX = x + labelW + 1;
            int chartW = Math.Max(1, w - labelW - 1);
            int cy = y;
            // axis
            b.DrawRect(new DL.Rect(chartX, cy, chartW, 1, _axis));
            cy++;
            foreach (var t in _tasks)
            {
                if (cy >= y + h) break;
                string lab = t.Label.Length > labelW-1 ? t.Label.Substring(0, labelW-1) : t.Label;
                b.DrawText(new DL.TextRun(x, cy, lab.PadRight(labelW), _label, _bg, DL.CellAttrFlags.None));
                int sx = chartX + (int)Math.Round((t.Start/(double)_horizon) * (chartW-1));
                int ex = chartX + Math.Min(chartW-1, sx + Math.Max(1, t.Duration));
                b.DrawRect(new DL.Rect(sx, cy, Math.Max(1, ex - sx), 1, t.Color));
                cy++;
            }
            b.Pop();
        }
    }
}
