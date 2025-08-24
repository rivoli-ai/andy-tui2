using System;
using System.Collections.Generic;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class Carousel
    {
        private readonly List<string> _items = new();
        private int _index;
        private DL.Rgb24 _fg = new DL.Rgb24(220,220,220);
        private DL.Rgb24 _bg = new DL.Rgb24(0,0,0);
        private DL.Rgb24 _accent = new DL.Rgb24(200,200,80);

        public void SetItems(IEnumerable<string> items) { _items.Clear(); if (items != null) _items.AddRange(items); _index = 0; }
        public void Next() { if (_items.Count == 0) return; _index = (_index + 1) % _items.Count; }
        public void Prev() { if (_items.Count == 0) return; _index = (_index - 1 + _items.Count) % _items.Count; }
        public int GetIndex() => _index;
        public void SetIndex(int i) { if (_items.Count == 0) { _index = 0; return; } _index = Math.Clamp(i, 0, _items.Count - 1); }

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x=(int)rect.X, y=(int)rect.Y, w=(int)rect.Width, h=(int)rect.Height;
            if (w<=0||h<=0) return;
            b.PushClip(new DL.ClipPush(x,y,w,h));
            b.DrawRect(new DL.Rect(x, y, w, h, _bg));
            if (_items.Count > 0)
            {
                string text = _items[_index];
                int cx = x + Math.Max(0, (w - text.Length) / 2);
                int cy = y + h/2;
                if (text.Length > w) text = text.Substring(0, w);
                b.DrawText(new DL.TextRun(cx, cy, text, _fg, _bg, DL.CellAttrFlags.Bold));
                // Dots
                int dotsW = _items.Count * 2 - 1;
                int dx = x + Math.Max(0, (w - dotsW) / 2);
                int dy = y + h - 1;
                for (int i = 0; i < _items.Count; i++)
                {
                    var color = i == _index ? _accent : new DL.Rgb24(120,120,120);
                    b.DrawText(new DL.TextRun(dx + i*2, dy, "•", color, _bg, DL.CellAttrFlags.None));
                }
            }
            b.Pop();
        }
    }
}
