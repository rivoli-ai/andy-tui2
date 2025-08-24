using System;
using System.Collections.Generic;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class Accordion
    {
        public sealed class Item
        {
            public string Header { get; }
            public Action<L.Rect, DL.DisplayList, DL.DisplayListBuilder> RenderContent { get; }
            public Item(string header, Action<L.Rect, DL.DisplayList, DL.DisplayListBuilder> render)
            { Header = header; RenderContent = render; }
        }

        private readonly List<Item> _items = new();
        private int _activeHeaderIndex;
        private HashSet<int> _expanded = new();
        private DL.Rgb24 _bg = new DL.Rgb24(0, 0, 0);
        private DL.Rgb24 _fg = new DL.Rgb24(220, 220, 220);
        private DL.Rgb24 _accent = new DL.Rgb24(200, 200, 80);

        public void SetItems(IEnumerable<Item> items)
        {
            _items.Clear(); _items.AddRange(items ?? Array.Empty<Item>());
            _activeHeaderIndex = Math.Clamp(_activeHeaderIndex, 0, Math.Max(0, _items.Count - 1));
        }

        public void SetActive(int index) => _activeHeaderIndex = Math.Clamp(index, 0, Math.Max(0, _items.Count - 1));
        public int GetActive() => _activeHeaderIndex;
        public void MoveActive(int delta) => SetActive(_activeHeaderIndex + delta);
        public void ToggleExpanded(int? index = null)
        {
            int i = index ?? _activeHeaderIndex;
            if (_expanded.Contains(i)) _expanded.Remove(i); else _expanded.Add(i);
        }

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x = (int)rect.X; int y = (int)rect.Y; int w = (int)rect.Width; int h = (int)rect.Height;
            if (w <= 0 || h <= 0) return;
            b.PushClip(new DL.ClipPush(x, y, w, h));
            b.DrawRect(new DL.Rect(x, y, w, h, _bg));

            int cy = y;
            for (int i = 0; i < _items.Count && cy < y + h; i++)
            {
                bool isActive = i == _activeHeaderIndex;
                var headBg = isActive ? _accent : new DL.Rgb24(30, 30, 30);
                var headFg = isActive ? new DL.Rgb24(0, 0, 0) : _fg;
                string arrow = _expanded.Contains(i) ? "▼" : "▶";
                string title = $" {arrow} {_items[i].Header} ";
                b.DrawRect(new DL.Rect(x, cy, w, 1, headBg));
                b.DrawText(new DL.TextRun(x + 1, cy, title.Length > w ? title.Substring(0, w) : title, headFg, headBg, isActive ? DL.CellAttrFlags.Bold : DL.CellAttrFlags.None));
                cy += 1;
                if (_expanded.Contains(i) && cy < y + h)
                {
                    int contentH = Math.Max(0, h - (cy - y));
                    var contentRect = new L.Rect(x + 2, cy, Math.Max(0, w - 4), contentH);
                    _items[i].RenderContent(contentRect, baseDl, b);
                    // Estimate content height as at least 3 lines for spacing in demo
                    cy += Math.Min(contentH, 3);
                }
            }

            b.Pop();
        }
    }
}
