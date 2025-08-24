using System;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class ResizeHandle
    {
        private bool _horizontal = true; // true: horizontal bar, false: vertical bar
        private DL.Rgb24 _fg = new DL.Rgb24(160,160,160);
        private DL.Rgb24 _bg = new DL.Rgb24(30,30,30);

        public void SetOrientation(bool horizontal) => _horizontal = horizontal;

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x=(int)rect.X, y=(int)rect.Y, w=(int)rect.Width, h=(int)rect.Height;
            if (w<=0||h<=0) return;
            b.PushClip(new DL.ClipPush(x,y,w,h));
            b.DrawRect(new DL.Rect(x,y,w,h,_bg));
            if (_horizontal)
            {
                int mid = y + h/2;
                b.DrawText(new DL.TextRun(x+1, mid, "━━╋━━", _fg, _bg, DL.CellAttrFlags.None));
            }
            else
            {
                for (int i=0;i<h;i++) b.DrawText(new DL.TextRun(x + w/2, y+i, "┃", _fg, _bg, DL.CellAttrFlags.None));
            }
            b.Pop();
        }
    }
}
