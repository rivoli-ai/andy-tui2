using System;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public enum SplitterOrientation { Vertical, Horizontal }

    public sealed class Splitter
    {
        private SplitterOrientation _orientation = SplitterOrientation.Vertical;
        private double _position = 0.5; // ratio 0..1
        private int _handleSize = 1;
        private int _minPane = 5;

        private Action<L.Rect, DL.DisplayList, DL.DisplayListBuilder>? _firstPane;
        private Action<L.Rect, DL.DisplayList, DL.DisplayListBuilder>? _secondPane;

        public void SetOrientation(SplitterOrientation orientation) => _orientation = orientation;
        public void SetPosition(double ratio) => _position = Math.Clamp(ratio, 0.1, 0.9);
        public void Adjust(double delta) => SetPosition(_position + delta);
        public void SetHandleSize(int size) => _handleSize = Math.Max(1, size);
        public void SetMinPane(int min) => _minPane = Math.Max(1, min);
        public void SetFirstPane(Action<L.Rect, DL.DisplayList, DL.DisplayListBuilder> render) => _firstPane = render;
        public void SetSecondPane(Action<L.Rect, DL.DisplayList, DL.DisplayListBuilder> render) => _secondPane = render;

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x = (int)rect.X; int y = (int)rect.Y; int w = (int)rect.Width; int h = (int)rect.Height;
            if (w <= 0 || h <= 0) return;
            b.PushClip(new DL.ClipPush(x, y, w, h));
            b.DrawRect(new DL.Rect(x, y, w, h, new DL.Rgb24(0, 0, 0)));

            if (_orientation == SplitterOrientation.Vertical)
            {
                int firstW = (int)Math.Round((w - _handleSize) * _position);
                firstW = Math.Clamp(firstW, _minPane, Math.Max(_minPane, w - _handleSize - _minPane));
                int handleX = x + firstW;
                int secondW = Math.Max(0, w - firstW - _handleSize);
                // render panes
                if (_firstPane != null) _firstPane(new L.Rect(x, y, firstW, h), baseDl, b);
                // handle background
                b.DrawRect(new DL.Rect(handleX, y, _handleSize, h, new DL.Rgb24(40, 40, 40)));
                // continuous center line
                int lineX = handleX + _handleSize / 2;
                for (int gy = y; gy < y + h; gy++)
                    b.DrawText(new DL.TextRun(lineX, gy, "│", new DL.Rgb24(140, 140, 140), null, DL.CellAttrFlags.None));
                if (_secondPane != null) _secondPane(new L.Rect(handleX + _handleSize, y, secondW, h), baseDl, b);
            }
            else
            {
                int firstH = (int)Math.Round((h - _handleSize) * _position);
                firstH = Math.Clamp(firstH, _minPane, Math.Max(_minPane, h - _handleSize - _minPane));
                int handleY = y + firstH;
                int secondH = Math.Max(0, h - firstH - _handleSize);
                if (_firstPane != null) _firstPane(new L.Rect(x, y, w, firstH), baseDl, b);
                b.DrawRect(new DL.Rect(x, handleY, w, _handleSize, new DL.Rgb24(40, 40, 40)));
                int lineY = handleY + _handleSize / 2;
                for (int gx = x; gx < x + w; gx++)
                    b.DrawText(new DL.TextRun(gx, lineY, "─", new DL.Rgb24(140, 140, 140), null, DL.CellAttrFlags.None));
                if (_secondPane != null) _secondPane(new L.Rect(x, handleY + _handleSize, w, secondH), baseDl, b);
            }

            b.Pop();
        }
    }
}
