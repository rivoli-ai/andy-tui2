using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    // Very small markup: [b] [/b], [i] [/i], [u] [/u], [color=#RRGGBB][/color]
    public sealed class RichText
    {
        private string _text = string.Empty;
        private DL.Rgb24 _bg = new DL.Rgb24(0,0,0);
        private DL.Rgb24 _fg = new DL.Rgb24(220,220,220);

        public void SetText(string text) => _text = text ?? string.Empty;
        public void SetColors(DL.Rgb24 fg, DL.Rgb24 bg) { _fg = fg; _bg = bg; }

        private struct Style { public bool Bold; public bool Italic; public bool Underline; public DL.Rgb24? Fg; }

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x=(int)rect.X, y=(int)rect.Y, w=(int)rect.Width, h=(int)rect.Height;
            if (w<=0||h<=0) return;
            b.PushClip(new DL.ClipPush(x,y,w,h));
            b.DrawRect(new DL.Rect(x,y,w,h,_bg));

            int cx = x; int cy = y; int maxX = x + w; int maxY = y + h;
            var stack = new Stack<Style>();
            stack.Push(new Style{Bold=false,Italic=false,Underline=false,Fg=null});

            int i=0;
            while (i < _text.Length && cy < maxY)
            {
                if (_text[i] == '[')
                {
                    int close = _text.IndexOf(']', i+1);
                    if (close > i)
                    {
                        string tag = _text.Substring(i+1, close - (i+1));
                        bool handled = false;
                        if (tag.Equals("b", StringComparison.OrdinalIgnoreCase)) { var s = stack.Peek(); s.Bold = true; stack.Push(s); handled = true; }
                        else if (tag.Equals("/b", StringComparison.OrdinalIgnoreCase)) { if (stack.Count > 1) stack.Pop(); handled = true; }
                        else if (tag.Equals("i", StringComparison.OrdinalIgnoreCase)) { var s = stack.Peek(); s.Italic = true; stack.Push(s); handled = true; }
                        else if (tag.Equals("/i", StringComparison.OrdinalIgnoreCase)) { if (stack.Count > 1) stack.Pop(); handled = true; }
                        else if (tag.Equals("u", StringComparison.OrdinalIgnoreCase)) { var s = stack.Peek(); s.Underline = true; stack.Push(s); handled = true; }
                        else if (tag.Equals("/u", StringComparison.OrdinalIgnoreCase)) { if (stack.Count > 1) stack.Pop(); handled = true; }
                        else if (tag.StartsWith("color=", StringComparison.OrdinalIgnoreCase))
                        {
                            var s = stack.Peek();
                            var val = tag.Substring(6);
                            if (val.StartsWith("#") && (val.Length==7))
                            {
                                if (int.TryParse(val.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb))
                                {
                                    s.Fg = new DL.Rgb24((byte)((rgb>>16)&0xFF),(byte)((rgb>>8)&0xFF),(byte)(rgb&0xFF));
                                }
                            }
                            stack.Push(s); handled=true;
                        }
                        else if (tag.Equals("/color", StringComparison.OrdinalIgnoreCase)) { if (stack.Count>1) stack.Pop(); handled=true; }

                        if (handled) { i = close + 1; continue; }
                    }
                }
                // Text run until next tag or newline
                int next = _text.IndexOf('[', i);
                int nl = _text.IndexOf('\n', i);
                int end = next == -1 ? _text.Length : next;
                if (nl != -1 && nl < end) end = nl;
                string chunk = _text.Substring(i, end - i);
                // Wrap if needed
                int remaining = maxX - cx;
                if (remaining <= 0) { cx = x; cy++; continue; }
                if (chunk.Length > remaining) chunk = chunk.Substring(0, remaining);
                var st = stack.Peek();
                var fg = st.Fg ?? _fg;
                var attrs = DL.CellAttrFlags.None;
                if (st.Bold) attrs |= DL.CellAttrFlags.Bold;
                if (st.Underline) attrs |= DL.CellAttrFlags.Underline;
                b.DrawText(new DL.TextRun(cx, cy, chunk, fg, _bg, attrs));
                cx += chunk.Length;
                i = end;
                if (nl != -1 && i == nl)
                {
                    cx = x; cy++; i++;
                }
            }

            b.Pop();
        }
    }
}
