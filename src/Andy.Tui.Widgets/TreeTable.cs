using System;
using System.Collections.Generic;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class TreeTable
    {
        public sealed class Node
        {
            public string Label { get; }
            public List<Node> Children { get; } = new List<Node>();
            public bool Expanded { get; set; }
            public Node(string label) { Label = label; }
        }

        private readonly List<Node> _roots = new();
        private DL.Rgb24 _fg = new DL.Rgb24(220,220,220);
        private DL.Rgb24 _accent = new DL.Rgb24(200,200,80);
        private int _scroll;
        private int _cursor;
        private readonly List<(Node node, int depth)> _flat = new();

        public void SetRoots(IEnumerable<Node> nodes)
        {
            _roots.Clear(); if (nodes != null) _roots.AddRange(nodes);
            _scroll = 0; _cursor = 0;
        }
        public void MoveCursor(int delta, int viewportRows)
        {
            RebuildFlat();
            _cursor = Math.Clamp(_cursor + delta, 0, Math.Max(0, _flat.Count - 1));
            EnsureVisible(viewportRows);
        }
        public void ToggleExpanded()
        {
            RebuildFlat();
            if (_cursor < 0 || _cursor >= _flat.Count) return;
            _flat[_cursor].node.Expanded = !_flat[_cursor].node.Expanded;
        }
        private void EnsureVisible(int rows)
        {
            if (_cursor < _scroll) _scroll = _cursor;
            if (_cursor >= _scroll + rows) _scroll = Math.Max(0, _cursor - rows + 1);
        }
        private void RebuildFlat()
        {
            _flat.Clear();
            foreach (var r in _roots) Add(r, 0);
            void Add(Node n, int d)
            {
                _flat.Add((n, d));
                if (n.Expanded)
                {
                    foreach (var c in n.Children) Add(c, d + 1);
                }
            }
        }

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x=(int)rect.X, y=(int)rect.Y, w=(int)rect.Width, h=(int)rect.Height;
            if (w<=0||h<=0) return;
            b.PushClip(new DL.ClipPush(x,y,w,h));
            b.DrawRect(new DL.Rect(x,y,w,h,new DL.Rgb24(0,0,0)));
            RebuildFlat();
            int visible = Math.Max(0, h);
            int end = Math.Min(_flat.Count, _scroll + visible);
            for (int i = _scroll, row = 0; i < end; i++, row++)
            {
                var (node, depth) = _flat[i];
                bool cursor = i == _cursor;
                var fg = cursor ? new DL.Rgb24(0,0,0) : _fg;
                var bg = cursor ? (DL.Rgb24?)_accent : null;
                string prefix = node.Children.Count > 0 ? (node.Expanded ? "▼ " : "▶ ") : "  ";
                string text = new string(' ', depth * 2) + prefix + node.Label;
                if (text.Length > w) text = text.Substring(0, w);
                b.DrawRect(new DL.Rect(x, y + row, w, 1, bg ?? new DL.Rgb24(0,0,0)));
                b.DrawText(new DL.TextRun(x, y + row, text, fg, bg, cursor ? DL.CellAttrFlags.Bold : DL.CellAttrFlags.None));
            }
            b.Pop();
        }
    }
}
