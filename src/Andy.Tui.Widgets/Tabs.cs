using System;
using System.Collections.Generic;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class Tabs
    {
        private readonly List<string> _tabs = new();
        private int _activeIndex;
        private Action<int, L.Rect, DL.DisplayList, DL.DisplayListBuilder>? _contentRenderer;
        private DL.Rgb24 _bg = new DL.Rgb24(0, 0, 0);
        private DL.Rgb24 _fg = new DL.Rgb24(220, 220, 220);
        private DL.Rgb24 _accent = new DL.Rgb24(200, 200, 80);

        public void SetTabs(IEnumerable<string> titles)
        {
            _tabs.Clear();
            _tabs.AddRange(titles ?? Array.Empty<string>());
            _activeIndex = Math.Clamp(_activeIndex, 0, Math.Max(0, _tabs.Count - 1));
        }

        public void SetActive(int index) => _activeIndex = Math.Clamp(index, 0, Math.Max(0, _tabs.Count - 1));
        public int GetActive() => _activeIndex;
        public void Move(int delta) => SetActive(_activeIndex + delta);
        public void SetContentRenderer(Action<int, L.Rect, DL.DisplayList, DL.DisplayListBuilder> render) => _contentRenderer = render;

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x = (int)rect.X; int y = (int)rect.Y; int w = (int)rect.Width; int h = (int)rect.Height;
            if (w <= 0 || h <= 0) return;
            b.PushClip(new DL.ClipPush(x, y, w, h));
            b.DrawRect(new DL.Rect(x, y, w, h, _bg));

            // Header row with tabs
            int curX = x + 1; int headerY = y;
            for (int i = 0; i < _tabs.Count; i++)
            {
                string t = _tabs[i];
                var isActive = i == _activeIndex;
                var bg = isActive ? _accent : new DL.Rgb24(30, 30, 30);
                var fg = isActive ? new DL.Rgb24(0, 0, 0) : _fg;
                int tw = t.Length + 2;
                b.DrawRect(new DL.Rect(curX, headerY, tw, 1, bg));
                b.DrawText(new DL.TextRun(curX + 1, headerY, t, fg, bg, isActive ? DL.CellAttrFlags.Bold : DL.CellAttrFlags.None));
                curX += tw + 1;
            }
            // Separator under tabs
            b.DrawRect(new DL.Rect(x, y + 1, w, 1, new DL.Rgb24(40, 40, 40)));

            // Content area
            if (_contentRenderer != null)
            {
                var contentRect = new L.Rect(x + 1, y + 2, Math.Max(0, w - 2), Math.Max(0, h - 3));
                _contentRenderer(_activeIndex, contentRect, baseDl, b);
            }

            b.Pop();
        }
    }
}
