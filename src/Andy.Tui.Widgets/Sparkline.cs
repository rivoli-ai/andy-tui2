using System;
using System.Collections.Generic;
using System.Linq;
using L = Andy.Tui.Layout;
using DL = Andy.Tui.DisplayList;

namespace Andy.Tui.Widgets
{
    public sealed class Sparkline
    {
        private IReadOnlyList<double> _values = Array.Empty<double>();
        private DL.Rgb24 _fg = new DL.Rgb24(120, 200, 255);
        private DL.Rgb24 _bg = new DL.Rgb24(20, 20, 20);
        private static readonly char[] Ramp = new[] { '▁', '▂', '▃', '▄', '▅', '▆', '▇', '█' };

        public void SetValues(IEnumerable<double> values) => _values = values?.ToArray() ?? Array.Empty<double>();
        public void SetColor(DL.Rgb24 fg) => _fg = fg;

        public (int Width, int Height) Measure() => (_values.Count, 1);

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x = (int)rect.X; int y = (int)rect.Y; int w = (int)rect.Width; int h = (int)rect.Height;
            if (w <= 0 || h <= 0) return;
            b.PushClip(new DL.ClipPush(x, y, w, h));
            b.DrawRect(new DL.Rect(x, y, w, h, _bg));
            if (_values.Count == 0) { b.Pop(); return; }

            double min = _values.Min();
            double max = _values.Max();
            if (Math.Abs(max - min) < 1e-12) { max = min + 1; }

            // Downsample or upsample to match available width
            for (int col = 0; col < w; col++)
            {
                double t0 = col / (double)w;
                double t1 = (col + 1) / (double)w;
                int i0 = (int)Math.Floor(t0 * _values.Count);
                int i1 = (int)Math.Ceiling(t1 * _values.Count);
                i0 = Math.Clamp(i0, 0, _values.Count - 1);
                i1 = Math.Clamp(i1, i0 + 1, _values.Count);
                double avg = 0;
                for (int i = i0; i < i1; i++) avg += _values[i];
                avg /= Math.Max(1, i1 - i0);
                int idx = (int)Math.Clamp(Math.Round((avg - min) / (max - min) * (Ramp.Length - 1)), 0, Ramp.Length - 1);
                b.DrawText(new DL.TextRun(x + col, y, Ramp[idx].ToString(), _fg, null, DL.CellAttrFlags.None));
            }

            b.Pop();
        }
    }
}
