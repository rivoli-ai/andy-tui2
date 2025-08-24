using System;
using System.Collections.Generic;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class MapView
    {
        private int _cols = 16, _rows = 8;
        private Func<int,int, DL.Rgb24> _tileColor = (cx,cy) => new DL.Rgb24(20,60,20);
        public void SetGrid(int cols, int rows){ _cols=Math.Max(1,cols); _rows=Math.Max(1,rows);}        
        public void SetTileColorProvider(Func<int,int, DL.Rgb24> provider){ _tileColor = provider ?? _tileColor; }
        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x=(int)rect.X, y=(int)rect.Y, w=(int)rect.Width, h=(int)rect.Height;
            if (w<=0||h<=0) return;
            b.PushClip(new DL.ClipPush(x,y,w,h));
            b.DrawRect(new DL.Rect(x,y,w,h,new DL.Rgb24(0,0,0)));
            int cellW = Math.Max(1, w/_cols);
            int cellH = Math.Max(1, h/_rows);
            for (int ry=0; ry<_rows; ry++)
            {
                for (int cx=0; cx<_cols; cx++)
                {
                    int px = x + cx * cellW;
                    int py = y + ry * cellH;
                    var c = _tileColor(cx, ry);
                    b.DrawRect(new DL.Rect(px, py, Math.Min(cellW, x+w-px), Math.Min(cellH, y+h-py), c));
                }
            }
            b.Pop();
        }
    }
}
