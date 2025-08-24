using System;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    // Simple help/hint panel overlay with keybindings
    public sealed class HintPanel
    {
        private string _title = "Help";
        private string[] _lines = Array.Empty<string>();
        private DL.Rgb24 _bg = new DL.Rgb24(20,20,20);
        private DL.Rgb24 _fg = new DL.Rgb24(220,220,220);
        private DL.Rgb24 _accent = new DL.Rgb24(200,200,80);
        public void SetTitle(string title) => _title = title ?? "";
        public void SetLines(params string[] lines) => _lines = lines ?? Array.Empty<string>();
        public void RenderCentered((int w,int h) viewport, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int w = Math.Min(viewport.w - 4, 60);
            int h = Math.Min(viewport.h - 4, Math.Max(5, _lines.Length + 4));
            int x = (viewport.w - w) / 2;
            int y = (viewport.h - h) / 2;
            b.PushClip(new DL.ClipPush(x, y, w, h));
            b.DrawRect(new DL.Rect(x, y, w, h, _bg));
            b.DrawBorder(new DL.Border(x, y, w, h, "single", _accent));
            b.DrawText(new DL.TextRun(x + 2, y + 1, _title, _accent, _bg, DL.CellAttrFlags.Bold));
            for (int i = 0; i < _lines.Length && i + 3 < h; i++)
            {
                b.DrawText(new DL.TextRun(x + 2, y + 2 + i, _lines[i], _fg, _bg, DL.CellAttrFlags.None));
            }
            b.Pop();
        }
    }
}
