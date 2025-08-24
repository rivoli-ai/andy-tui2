using System;
using L = Andy.Tui.Layout;
using DL = Andy.Tui.DisplayList;

namespace Andy.Tui.Widgets
{
    public sealed class Pager
    {
        private int _totalItems = 0;
        private int _pageSize = 10;
        private int _currentPage = 1; // 1-based
        private DL.Rgb24 _fg = new DL.Rgb24(220, 220, 220);
        private DL.Rgb24 _accent = new DL.Rgb24(90, 170, 255);
        private DL.Rgb24 _bg = new DL.Rgb24(20, 20, 20);

        public void SetTotalItems(int total) => _totalItems = Math.Max(0, total);
        public void SetPageSize(int size) => _pageSize = Math.Max(1, size);
        public void SetCurrentPage(int page) => _currentPage = Math.Max(1, Math.Min(page, GetTotalPages()));
        public int GetCurrentPage() => _currentPage;
        public int GetTotalPages() => _pageSize <= 0 ? 0 : Math.Max(1, (int)Math.Ceiling(_totalItems / (double)_pageSize));
        public void Next() => SetCurrentPage(_currentPage + 1);
        public void Prev() => SetCurrentPage(_currentPage - 1);

        public (int Width, int Height) Measure()
        {
            // Rough width: "Prev 123/456 Next"
            int totalPages = GetTotalPages();
            int digitsCur = Math.Max(1, _currentPage.ToString().Length);
            int digitsTot = Math.Max(1, totalPages.ToString().Length);
            int width = 5 + 1 + digitsCur + 1 + digitsTot + 1 + 4;
            return (width, 1);
        }

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x = (int)rect.X; int y = (int)rect.Y; int w = (int)rect.Width; int h = (int)rect.Height;
            if (w <= 0 || h <= 0) return;
            b.PushClip(new DL.ClipPush(x, y, w, h));
            b.DrawRect(new DL.Rect(x, y, w, h, _bg));

            int totalPages = GetTotalPages();
            string prev = "Prev";
            string next = "Next";
            string middle = $" {_currentPage}/{totalPages} ";
            int cursor = x + 1;
            b.DrawText(new DL.TextRun(cursor, y, prev, _fg, null, DL.CellAttrFlags.None));
            cursor += prev.Length;
            b.DrawText(new DL.TextRun(cursor, y, middle, _fg, null, DL.CellAttrFlags.None));
            cursor += middle.Length;
            b.DrawText(new DL.TextRun(cursor, y, next, _fg, null, DL.CellAttrFlags.None));

            // Highlight current page number
            int slash = middle.IndexOf('/');
            if (slash > 1)
            {
                int curStart = x + 1 + prev.Length + 1; // space before cur
                b.DrawText(new DL.TextRun(curStart, y, _currentPage.ToString(), _accent, null, DL.CellAttrFlags.Bold));
            }

            b.Pop();
        }
    }
}
