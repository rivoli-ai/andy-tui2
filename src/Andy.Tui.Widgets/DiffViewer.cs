using System;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class DiffViewer
    {
        private string[] _a = Array.Empty<string>();
        private string[] _b = Array.Empty<string>();
        public DL.Rgb24 Border = new DL.Rgb24(80,80,80);
        private DL.Rgb24 _fg = new DL.Rgb24(220,220,220);
        private DL.Rgb24 _addBg = new DL.Rgb24(30,70,30);
        private DL.Rgb24 _delBg = new DL.Rgb24(80,30,30);

        public void SetLeft(string text) => _a = (text ?? string.Empty).Replace("\r\n","\n").Replace('\r','\n').Split('\n');
        public void SetRight(string text) => _b = (text ?? string.Empty).Replace("\r\n","\n").Replace('\r','\n').Split('\n');

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x=(int)rect.X, y=(int)rect.Y, w=(int)rect.Width, h=(int)rect.Height;
            if (w<=0||h<=0) return;
            b.PushClip(new DL.ClipPush(x,y,w,h));
            b.DrawBorder(new DL.Border(x,y,w,h,"single", Border));
            int contentX = x + 1; int contentY = y + 1; int contentW = Math.Max(0, w - 2); int contentH = Math.Max(0, h - 2);
            int mid = contentX + contentW/2;
            int rows = Math.Min(contentH, Math.Max(_a.Length, _b.Length));
            for (int i = 0; i < rows; i++)
            {
                string la = i < _a.Length ? _a[i] : string.Empty;
                string rb = i < _b.Length ? _b[i] : string.Empty;
                if (!string.Equals(la, rb, StringComparison.Ordinal))
                {
                    if (la.Length > 0) b.DrawRect(new DL.Rect(contentX, contentY + i, mid - contentX, 1, _delBg));
                    if (rb.Length > 0) b.DrawRect(new DL.Rect(mid, contentY + i, contentX + contentW - mid, 1, _addBg));
                }
                if (la.Length > 0)
                {
                    string left = la.Length > (mid - contentX) ? la.Substring(0, mid - contentX) : la;
                    b.DrawText(new DL.TextRun(contentX, contentY + i, left, _fg, null, DL.CellAttrFlags.None));
                }
                if (rb.Length > 0)
                {
                    string right = rb.Length > (contentX + contentW - mid) ? rb.Substring(0, contentX + contentW - mid) : rb;
                    b.DrawText(new DL.TextRun(mid, contentY + i, right, _fg, null, DL.CellAttrFlags.None));
                }
            }
            b.Pop();
        }
    }
}
