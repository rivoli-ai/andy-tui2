using System;
using System.Collections.Generic;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class PreferencesPanel
    {
        private readonly List<(string Key,string Value)> _items = new();
        private DL.Rgb24 _bg = new DL.Rgb24(10,10,10);
        private DL.Rgb24 _fg = new DL.Rgb24(230,230,230);
        private DL.Rgb24 _accent = new DL.Rgb24(200,200,80);
        public void SetItems(IEnumerable<(string Key,string Value)> items){ _items.Clear(); if (items!=null) _items.AddRange(items); }
        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x=(int)rect.X,y=(int)rect.Y,w=(int)rect.Width,h=(int)rect.Height; if (w<=0||h<=0) return;
            b.PushClip(new DL.ClipPush(x,y,w,h));
            b.DrawRect(new DL.Rect(x,y,w,h,_bg));
            b.DrawBorder(new DL.Border(x,y,w,h,"single", _accent));
            int cy=y+1;
            foreach (var (k,v) in _items)
            {
                if (cy>=y+h-1) break;
                string line = $"{k}: {v}";
                if (line.Length> w-2) line = line.Substring(0,w-2);
                b.DrawText(new DL.TextRun(x+1, cy++, line, _fg, _bg, DL.CellAttrFlags.None));
            }
            b.Pop();
        }
    }
}
