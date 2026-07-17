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
            // The URL is attached as structured metadata, never embedded in the text
            // content. The compositor keeps it out of the cell stream (so it never
            // consumes layout cells or gets clipped) and the encoder emits a properly
            // terminated OSC 8 sequence only when the terminal advertises hyperlink
            // support. Terminals without support fall back to plain, styled text.
            string? hyperlink = _enableOsc8 && !string.IsNullOrEmpty(_url) ? _url : null;
            b.DrawText(new DL.TextRun(x, y, content, _fg, null, DL.CellAttrFlags.Underline | DL.CellAttrFlags.Bold)
            {
                Hyperlink = hyperlink
            });
            b.Pop();
        }
    }
}
