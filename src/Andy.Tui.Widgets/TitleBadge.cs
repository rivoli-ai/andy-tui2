using System;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class TitleBadge
    {
        private string _title = "Title";
        private string _badge = "NEW";
        private DL.Rgb24 _titleFg = new DL.Rgb24(220,220,220);
        private DL.Rgb24 _bg = new DL.Rgb24(20,20,20);
        private DL.Rgb24 _badgeFg = new DL.Rgb24(0,0,0);
        private DL.Rgb24 _badgeBg = new DL.Rgb24(200,200,80);

        public void SetTitle(string title) => _title = title ?? string.Empty;
        public void SetBadge(string badge) => _badge = badge ?? string.Empty;
        public void SetColors(DL.Rgb24 titleFg, DL.Rgb24 bg, DL.Rgb24 badgeFg, DL.Rgb24 badgeBg)
        { _titleFg = titleFg; _bg = bg; _badgeFg = badgeFg; _badgeBg = badgeBg; }

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x = (int)rect.X, y = (int)rect.Y, w = Math.Max(0,(int)rect.Width), h = Math.Max(1,(int)rect.Height);
            if (w <= 0 || h <= 0) return;
            b.PushClip(new DL.ClipPush(x,y,w,h));
            b.DrawRect(new DL.Rect(x,y,w,h,_bg));
            // title
            b.DrawText(new DL.TextRun(x+1, y, _title, _titleFg, null, DL.CellAttrFlags.Bold));
            // badge box
            int badgeW = Math.Max(3, _badge.Length + 2);
            int badgeX = x + w - badgeW - 1;
            if (badgeX > x)
            {
                b.DrawRect(new DL.Rect(badgeX, y, badgeW, 1, _badgeBg));
                b.DrawText(new DL.TextRun(badgeX + 1, y, _badge, _badgeFg, _badgeBg, DL.CellAttrFlags.Bold));
            }
            b.Pop();
        }
    }
}
