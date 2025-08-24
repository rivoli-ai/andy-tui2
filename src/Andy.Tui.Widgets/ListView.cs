using System;
using System.Collections.Generic;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class ListView
    {
        private readonly List<string> _items = new();
        private readonly HashSet<int> _selected = new();
        private int _scroll;
        private int _cursor;
        public DL.Rgb24 Border = new DL.Rgb24(80,80,80);
        public DL.Rgb24 ItemFg = new DL.Rgb24(220,220,220);
        public DL.Rgb24 SelFg = new DL.Rgb24(0,0,0);
        public DL.Rgb24 SelBg = new DL.Rgb24(200,200,80);

        public void SetItems(IEnumerable<string> items)
        { _items.Clear(); if (items != null) _items.AddRange(items); _scroll = 0; _cursor = 0; _selected.Clear(); }
        public IReadOnlyCollection<int> GetSelectedIndices() => _selected;
        public int GetCursor() => _cursor;
        public void MoveCursor(int delta)
        {
            _cursor = Math.Clamp(_cursor + delta, 0, Math.Max(0, _items.Count - 1));
            EnsureCursorVisible(0, _viewportH);
        }
        private int _viewportH;
        public void ToggleSelect(int? index = null)
        {
            int i = index ?? _cursor;
            if (i < 0 || i >= _items.Count) return;
            if (_selected.Contains(i)) _selected.Remove(i); else _selected.Add(i);
        }
        public void ClearSelection() => _selected.Clear();
        public void SelectRange(int start, int end)
        {
            int a = Math.Clamp(Math.Min(start, end), 0, _items.Count - 1);
            int b = Math.Clamp(Math.Max(start, end), 0, _items.Count - 1);
            for (int i = a; i <= b; i++) _selected.Add(i);
        }
        public void Page(int delta)
        { MoveCursor(delta * Math.Max(1, _viewportH - 1)); }
        public void Home() { _cursor = 0; _scroll = 0; }
        public void End() { _cursor = Math.Max(0, _items.Count - 1); _scroll = Math.Max(0, _items.Count - _viewportH); }
        private void EnsureCursorVisible(int x, int viewportH)
        {
            if (_cursor < _scroll) _scroll = _cursor;
            if (_cursor >= _scroll + viewportH) _scroll = Math.Max(0, _cursor - viewportH + 1);
        }

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
        {
            int x = (int)rect.X; int y = (int)rect.Y; int w = (int)rect.Width; int h = (int)rect.Height;
            if (w <= 0 || h <= 0) return;
            _viewportH = h - 2;
            builder.PushClip(new DL.ClipPush(x, y, w, h));
            builder.DrawBorder(new DL.Border(x, y, w, h, "single", Border));
            int contentX = x + 1, contentY = y + 1, contentW = Math.Max(0, w - 2), contentH = Math.Max(0, h - 2);
            int maxIndex = Math.Min(_items.Count, _scroll + contentH);
            for (int i = _scroll, row = 0; i < maxIndex; i++, row++)
            {
                bool isSel = _selected.Contains(i) || i == _cursor;
                var fg = isSel ? SelFg : ItemFg;
                var bg = isSel ? SelBg : (DL.Rgb24?)null;
                string text = _items[i];
                if (text.Length > contentW) text = text.Substring(0, contentW);
                builder.DrawRect(new DL.Rect(contentX, contentY + row, contentW, 1, bg ?? new DL.Rgb24(0,0,0)));
                builder.DrawText(new DL.TextRun(contentX, contentY + row, text, fg, bg, isSel ? DL.CellAttrFlags.Bold : DL.CellAttrFlags.None));
            }
            builder.Pop();
        }
    }
}
