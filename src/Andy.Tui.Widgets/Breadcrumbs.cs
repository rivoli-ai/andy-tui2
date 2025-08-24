using System;
using System.Collections.Generic;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class Breadcrumbs
    {
        private readonly List<string> _parts = new();
        private string _separator = "›";
        private DL.Rgb24 _fg = new DL.Rgb24(220,220,220);
        private DL.Rgb24 _dim = new DL.Rgb24(150,150,150);
        private DL.Rgb24 _bg = new DL.Rgb24(0,0,0);
        public void SetItems(IEnumerable<string> items) { _parts.Clear(); _parts.AddRange(items ?? Array.Empty<string>()); }
        public void SetSeparator(string sep) { _separator = sep; }
        public void SetColors(DL.Rgb24 fg, DL.Rgb24 dim, DL.Rgb24 bg) { _fg = fg; _dim = dim; _bg = bg; }

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x = (int)rect.X; int y = (int)rect.Y; int w = (int)rect.Width; int h = (int)rect.Height;
            if (w <= 0 || h <= 0) return;
            b.PushClip(new DL.ClipPush(x, y, w, h));
            b.DrawRect(new DL.Rect(x, y, w, h, _bg));
            int cx = x;
            for (int i = 0; i < _parts.Count; i++)
            {
                var color = i == _parts.Count - 1 ? _fg : _dim;
                var text = _parts[i];
                if (cx >= x + w) break;
                int avail = Math.Max(0, x + w - cx);
                if (text.Length > avail) text = text.Substring(0, avail);
                b.DrawText(new DL.TextRun(cx, y, text, color, _bg, DL.CellAttrFlags.None));
                cx += text.Length;
                if (i != _parts.Count - 1 && cx < x + w)
                {
                    string sep = $" {_separator} ";
                    if (sep.Length > x + w - cx) sep = sep.Substring(0, x + w - cx);
                    b.DrawText(new DL.TextRun(cx, y, sep, _dim, _bg, DL.CellAttrFlags.None));
                    cx += sep.Length;
                }
            }
            b.Pop();
        }
    }
}
