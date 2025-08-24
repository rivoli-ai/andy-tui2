using System;
using System.Collections.Generic;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class FocusRing
    {
        private readonly List<(string id, L.Rect rect)> _order = new();
        private int _index;
        private DL.Rgb24 _highlight = new DL.Rgb24(200,200,80);
        private DL.Rgb24 _bg = new DL.Rgb24(0,0,0);

        public void Clear() { _order.Clear(); _index = 0; }
        public void Add(string id, in L.Rect rect) { _order.Add((id, rect)); }
        public string? GetFocusedId() => _order.Count == 0 ? null : _order[_index].id;
        public int GetFocusedIndex() => _index;
        public void Next() { if (_order.Count == 0) return; _index = (_index + 1) % _order.Count; }
        public void Prev() { if (_order.Count == 0) return; _index = (_index - 1 + _order.Count) % _order.Count; }

        public void Render(DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            for (int i = 0; i < _order.Count; i++)
            {
                var r = _order[i].rect;
                int x=(int)r.X, y=(int)r.Y, w=(int)r.Width, h=(int)r.Height;
                if (i == _index)
                {
                    // Draw a highlight border rectangle (1-pixel outline)
                    b.DrawRect(new DL.Rect(x, y, w, 1, _highlight));
                    b.DrawRect(new DL.Rect(x, y + h - 1, w, 1, _highlight));
                    b.DrawRect(new DL.Rect(x, y, 1, h, _highlight));
                    b.DrawRect(new DL.Rect(x + w - 1, y, 1, h, _highlight));
                }
            }
        }
    }
}
