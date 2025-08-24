using System;
using System.Collections.Generic;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class Timeline
    {
        public readonly struct Item
        {
            public readonly string Time;
            public readonly string Text;
            public Item(string time, string text) { Time = time; Text = text; }
        }

        private readonly List<Item> _items = new();
        private DL.Rgb24 _timeFg = new DL.Rgb24(200,200,80);
        private DL.Rgb24 _textFg = new DL.Rgb24(230,230,230);
        private DL.Rgb24 _bg = new DL.Rgb24(0,0,0);
        public void SetItems(IEnumerable<Item> items)
        {
            _items.Clear(); if (items != null) _items.AddRange(items);
        }

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x=(int)rect.X, y=(int)rect.Y, w=(int)rect.Width, h=(int)rect.Height;
            if (w<=0||h<=0) return;
            b.PushClip(new DL.ClipPush(x,y,w,h));
            b.DrawRect(new DL.Rect(x,y,w,h,_bg));
            int cy = y;
            int timeW = 8;
            foreach (var it in _items)
            {
                if (cy >= y + h) break;
                string t = it.Time.Length > timeW ? it.Time.Substring(0,timeW) : it.Time.PadRight(timeW);
                b.DrawText(new DL.TextRun(x, cy, t, _timeFg, _bg, DL.CellAttrFlags.Bold));
                b.DrawText(new DL.TextRun(x + timeW, cy, " • ", _timeFg, _bg, DL.CellAttrFlags.None));
                int avail = Math.Max(0, w - (timeW + 3));
                string msg = it.Text.Length > avail ? it.Text.Substring(0,avail) : it.Text;
                b.DrawText(new DL.TextRun(x + timeW + 3, cy, msg, _textFg, _bg, DL.CellAttrFlags.None));
                cy++;
            }
            b.Pop();
        }
    }
}
