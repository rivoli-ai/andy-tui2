using System;
using System.Collections.Generic;
using System.Linq;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class KeyValueList
    {
        private readonly List<(string Key, string Value)> _items = new();
        private DL.Rgb24 _keyFg = new DL.Rgb24(180,180,180);
        private DL.Rgb24 _valFg = new DL.Rgb24(235,235,235);
        private DL.Rgb24 _bg = new DL.Rgb24(0,0,0);
        public void SetItems(IEnumerable<(string key, string value)> items)
        {
            _items.Clear();
            if (items != null) _items.AddRange(items);
        }
        public void SetColors(DL.Rgb24 key, DL.Rgb24 val, DL.Rgb24 bg) { _keyFg = key; _valFg = val; _bg = bg; }

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x=(int)rect.X, y=(int)rect.Y, w=(int)rect.Width, h=(int)rect.Height;
            if (w<=0||h<=0) return;
            b.PushClip(new DL.ClipPush(x,y,w,h));
            b.DrawRect(new DL.Rect(x,y,w,h,_bg));
            int labelW = Math.Min(Math.Max(0, _items.Select(i => i.Key.Length).DefaultIfEmpty(0).Max()+1), Math.Max(0,w-3));
            int cy = y;
            foreach (var (key, value) in _items)
            {
                if (cy >= y + h) break;
                string lk = key.Length > labelW ? key.Substring(0,labelW) : key;
                b.DrawText(new DL.TextRun(x, cy, lk.PadRight(labelW), _keyFg, _bg, DL.CellAttrFlags.Bold));
                string sep = ": ";
                b.DrawText(new DL.TextRun(x + labelW, cy, sep, _keyFg, _bg, DL.CellAttrFlags.None));
                int avail = Math.Max(0, w - (labelW + sep.Length));
                string vv = value;
                if (vv.Length > avail) vv = vv.Substring(0, avail);
                b.DrawText(new DL.TextRun(x + labelW + sep.Length, cy, vv, _valFg, _bg, DL.CellAttrFlags.None));
                cy++;
            }
            b.Pop();
        }
    }
}
