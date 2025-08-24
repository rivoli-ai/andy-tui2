using System;
using L = Andy.Tui.Layout;
using DL = Andy.Tui.DisplayList;

namespace Andy.Tui.Widgets
{
    public sealed class Link
    {
        private string _text = string.Empty;
        private string _url = string.Empty;
        private DL.Rgb24 _fg = new DL.Rgb24(80, 160, 255);
        private DL.Rgb24 _bg = new DL.Rgb24(20, 20, 20);
        private bool _enableOsc8 = true;

        public void SetText(string text) => _text = text ?? string.Empty;
        public void SetUrl(string url) => _url = url ?? string.Empty;
        public void SetColor(DL.Rgb24 color) => _fg = color;
        public void EnableOsc8(bool enabled) => _enableOsc8 = enabled;

        public (int Width, int Height) Measure() => (Math.Max(_text.Length, _url.Length), 1);

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x = (int)rect.X; int y = (int)rect.Y; int w = (int)rect.Width; int h = (int)rect.Height;
            if (w <= 0 || h <= 0) return;
            b.PushClip(new DL.ClipPush(x, y, w, h));
            b.DrawRect(new DL.Rect(x, y, w, h, _bg));
            string content = _text.Length > 0 ? _text : _url;
            if (content.Length > w) content = content.Substring(0, w);
            string rendered = content;
            if (_enableOsc8 && !string.IsNullOrEmpty(_url))
            {
                // OSC 8 hyperlink: ESC ] 8 ; params ; URI ST  ...  ESC ] 8 ; ; ST
                const string esc = "\u001b";
                const string st = "\u001b\\"; // String Terminator
                string start = $"{esc}]8;;{_url}{st}";
                string end = $"{esc}]8;;{st}";
                rendered = start + content + end;
            }
            b.DrawText(new DL.TextRun(x, y, rendered, _fg, null, DL.CellAttrFlags.Underline | DL.CellAttrFlags.Bold));
            b.Pop();
        }
    }
}
