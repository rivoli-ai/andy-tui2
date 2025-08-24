using System;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class Card
    {
        private string _title = string.Empty;
        private string _body = string.Empty;
        private string _footer = string.Empty;
        private DL.Rgb24 _bg = new DL.Rgb24(10,10,10);
        private DL.Rgb24 _fg = new DL.Rgb24(230,230,230);
        private DL.Rgb24 _accent = new DL.Rgb24(200,200,80);
        public void SetTitle(string t) => _title = t ?? string.Empty;
        public void SetBody(string b) => _body = b ?? string.Empty;
        public void SetFooter(string f) => _footer = f ?? string.Empty;
        public void SetColors(DL.Rgb24 fg, DL.Rgb24 bg, DL.Rgb24 accent) { _fg = fg; _bg = bg; _accent = accent; }

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x=(int)rect.X, y=(int)rect.Y, w=(int)rect.Width, h=(int)rect.Height;
            if (w<=0||h<=0) return;
            b.PushClip(new DL.ClipPush(x,y,w,h));
            b.DrawRect(new DL.Rect(x,y,w,h,_bg));
            b.DrawBorder(new DL.Border(x,y,w,h,"single", _accent));
            int cy = y + 1;
            if (!string.IsNullOrEmpty(_title))
            {
                string t = _title.Length > w-4 ? _title.Substring(0,w-4) : _title;
                b.DrawText(new DL.TextRun(x+2, cy, t, _accent, _bg, DL.CellAttrFlags.Bold));
                cy++;
            }
            foreach (var line in (_body ?? string.Empty).Replace("\r\n","\n").Replace('\r','\n').Split('\n'))
            {
                if (cy >= y + h - (string.IsNullOrEmpty(_footer) ? 1 : 2)) break;
                string l = line.Length > w-4 ? line.Substring(0,w-4) : line;
                b.DrawText(new DL.TextRun(x+2, cy++, l, _fg, _bg, DL.CellAttrFlags.None));
            }
            if (!string.IsNullOrEmpty(_footer) && cy < y + h - 1)
            {
                string f = _footer.Length > w-4 ? _footer.Substring(0,w-4) : _footer;
                b.DrawText(new DL.TextRun(x+2, y + h - 2, f, _fg, _bg, DL.CellAttrFlags.None));
            }
            b.Pop();
        }
    }
}
