using System;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class AboutDialog
    {
        private string _title = "About";
        private string _body = "Andy.Tui — A fast TUI library";
        private DL.Rgb24 _bg = new DL.Rgb24(10,10,10);
        private DL.Rgb24 _fg = new DL.Rgb24(230,230,230);
        private DL.Rgb24 _accent = new DL.Rgb24(200,200,80);
        public void SetContent(string title, string body){ _title = title ?? "About"; _body = body ?? string.Empty; }
        public void RenderCentered((int w,int h) viewport, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int w = Math.Min(viewport.w - 4, 50);
            int h = Math.Min(viewport.h - 4, 6);
            int x = (viewport.w - w)/2; int y = (viewport.h - h)/2;
            b.PushClip(new DL.ClipPush(x,y,w,h));
            b.DrawRect(new DL.Rect(x,y,w,h,_bg));
            b.DrawBorder(new DL.Border(x,y,w,h,"single", _accent));
            b.DrawText(new DL.TextRun(x+2, y+1, _title, _accent, _bg, DL.CellAttrFlags.Bold));
            b.DrawText(new DL.TextRun(x+2, y+3, _body, _fg, _bg, DL.CellAttrFlags.None));
            b.Pop();
        }
    }
}
