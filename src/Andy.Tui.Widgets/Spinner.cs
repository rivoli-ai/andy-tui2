using System;
using L = Andy.Tui.Layout;
using DL = Andy.Tui.DisplayList;

namespace Andy.Tui.Widgets
{
    public sealed class Spinner : WidgetBase
    {
        private static readonly char[] Frames = new[] { '|', '/', '-', '\\' };
        private int _index;
        private DL.Rgb24 _fg = new DL.Rgb24(200, 200, 200);
        private DL.Rgb24 _bg = new DL.Rgb24(20, 20, 20);

        public void Tick() { _index = (_index + 1) % Frames.Length; Invalidate(); }

        /// <summary>Backward-compatible intrinsic size accessor.</summary>
        public (int Width, int Height) Measure() => (1, 1);

        protected override L.Size MeasureCore(L.Size available) => new(1, 1);

        protected override void RenderCore(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x = (int)rect.X; int y = (int)rect.Y; int w = (int)rect.Width; int h = (int)rect.Height;
            b.PushClip(new DL.ClipPush(x, y, w, h));
            b.DrawRect(new DL.Rect(x, y, w, h, ResolveBackground(_bg)));
            b.DrawText(new DL.TextRun(x, y, Frames[_index].ToString(), ResolveForeground(_fg), null, DL.CellAttrFlags.Bold));
            b.Pop();
        }
    }
}
