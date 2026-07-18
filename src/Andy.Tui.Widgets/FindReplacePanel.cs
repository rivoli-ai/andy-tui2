using System;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class FindReplacePanel : WidgetBase
    {
        private string _find = string.Empty;
        private string _replace = string.Empty;
        private DL.Rgb24 _bg = new DL.Rgb24(10,10,10);
        private DL.Rgb24 _fg = new DL.Rgb24(230,230,230);
        private DL.Rgb24 _accent = new DL.Rgb24(200,200,80);

        // Hidden by default; visibility is now backed by WidgetBase.IsVisible and
        // the base Render short-circuits when not visible (matching the prior
        // _visible early-return behaviour).
        public FindReplacePanel() => SetVisible(false);

        public void SetText(string find, string replace) { _find = find ?? string.Empty; _replace = replace ?? string.Empty; }
        public (string Find, string Replace) GetText() => (_find, _replace);
        protected override void RenderCore(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x=(int)rect.X, y=(int)rect.Y, w=(int)rect.Width, h=(int)rect.Height;
            int panelH = 3;
            b.PushClip(new DL.ClipPush(x,y,w,h));
            b.DrawRect(new DL.Rect(x, y, w, panelH, _bg));
            b.DrawBorder(new DL.Border(x, y, w, panelH, "single", _accent));
            b.DrawText(new DL.TextRun(x+2, y+1, $"Find: {_find}  Replace: {_replace}", _fg, _bg, DL.CellAttrFlags.None));
            b.Pop();
        }
    }
}
