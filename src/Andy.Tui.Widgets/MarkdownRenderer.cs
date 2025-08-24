using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    // Minimal markdown-ish: #, ##, ### headings; *italic*, **bold**, `code`; lists: - item
    public sealed class MarkdownRenderer
    {
        private string _md = string.Empty;
        private DL.Rgb24 _bg = new DL.Rgb24(0,0,0);
        private DL.Rgb24 _fg = new DL.Rgb24(220,220,220);
        private DL.Rgb24 _accent = new DL.Rgb24(200,200,80);

        public void SetText(string md) => _md = md ?? string.Empty;
        public void SetColors(DL.Rgb24 fg, DL.Rgb24 bg, DL.Rgb24 accent) { _fg = fg; _bg = bg; _accent = accent; }

        private static readonly Regex Bold = new Regex(@"\*\*(.+?)\*\*", RegexOptions.Compiled);
        private static readonly Regex Italic = new Regex(@"\*(.+?)\*", RegexOptions.Compiled);
        private static readonly Regex Code = new Regex(@"`(.+?)`", RegexOptions.Compiled);

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x=(int)rect.X, y=(int)rect.Y, w=(int)rect.Width, h=(int)rect.Height;
            if (w<=0||h<=0) return;
            b.PushClip(new DL.ClipPush(x,y,w,h));
            b.DrawRect(new DL.Rect(x,y,w,h,_bg));
            int cy = y;
            foreach (var line in _md.Replace("\r\n","\n").Replace('\r','\n').Split('\n'))
            {
                if (cy >= y + h) break;
                string text = line;
                DL.CellAttrFlags attrs = DL.CellAttrFlags.None;
                var color = _fg;
                int indent = 0;
                if (text.StartsWith("### ")) { text = text.Substring(4); attrs |= DL.CellAttrFlags.Bold; color = _accent; }
                else if (text.StartsWith("## ")) { text = text.Substring(3); attrs |= DL.CellAttrFlags.Bold; color = _accent; }
                else if (text.StartsWith("# ")) { text = text.Substring(2); attrs |= DL.CellAttrFlags.Bold; color = _accent; }
                else if (text.StartsWith("- ")) { text = "• " + text.Substring(2); indent = 2; }
                // inline transforms: bold/italic/code
                string rendered = Code.Replace(Italic.Replace(Bold.Replace(text, "$1"), "$1"), "$1");
                int cx = x + indent;
                foreach (char ch in rendered)
                {
                    if (cx >= x + w) break;
                    if (ch == '\u0001') { attrs ^= DL.CellAttrFlags.Bold; continue; }
                    if (ch == '\u0002') { attrs ^= DL.CellAttrFlags.Underline; continue; }
                    if (ch == '\u0003') { /* code */ attrs ^= DL.CellAttrFlags.Underline; continue; }
                    b.DrawText(new DL.TextRun(cx++, cy, ch.ToString(), color, _bg, attrs));
                }
                cy++;
            }
            b.Pop();
        }
    }
}
