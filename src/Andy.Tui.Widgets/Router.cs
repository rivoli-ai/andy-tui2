using System;
using System.Collections.Generic;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class Router
    {
        private readonly Dictionary<string, Action<L.Rect, DL.DisplayList, DL.DisplayListBuilder>> _routes = new();
        private readonly List<string> _history = new();
        private int _cursor = -1; // index into history
        private string? _current;
        private DL.Rgb24 _bg = new DL.Rgb24(0,0,0);

        public void SetBackground(DL.Rgb24 color) => _bg = color;
        public void SetRoute(string name, Action<L.Rect, DL.DisplayList, DL.DisplayListBuilder> render)
        { _routes[name] = render; }

        public string? GetCurrent() => _current;
        public IReadOnlyList<string> GetHistory() => _history;

        public void NavigateTo(string name)
        {
            if (!_routes.ContainsKey(name)) return;
            // Drop forward history
            if (_cursor >= 0 && _cursor < _history.Count - 1)
                _history.RemoveRange(_cursor + 1, _history.Count - (_cursor + 1));
            _history.Add(name);
            _cursor = _history.Count - 1;
            _current = name;
        }

        public bool CanBack() => _cursor > 0;
        public bool CanForward() => _cursor >= 0 && _cursor < _history.Count - 1;
        public void Back()
        {
            if (!CanBack()) return;
            _cursor--;
            _current = _history[_cursor];
        }
        public void Forward()
        {
            if (!CanForward()) return;
            _cursor++;
            _current = _history[_cursor];
        }

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x = (int)rect.X; int y = (int)rect.Y; int w = (int)rect.Width; int h = (int)rect.Height;
            if (w <= 0 || h <= 0) return;
            b.PushClip(new DL.ClipPush(x, y, w, h));
            b.DrawRect(new DL.Rect(x, y, w, h, _bg));
            if (_current != null && _routes.TryGetValue(_current, out var render))
            {
                render(new L.Rect(x, y, w, h), baseDl, b);
            }
            b.Pop();
        }
    }
}
