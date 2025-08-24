using System;
using System.Collections.Generic;
using System.Linq;
using L = Andy.Tui.Layout;
using DL = Andy.Tui.DisplayList;

namespace Andy.Tui.Widgets
{
    public sealed class BarChart
    {
        private readonly List<(string Label, double Value)> _items = new();
        private DL.Rgb24 _fg = new DL.Rgb24(220, 220, 220);
        private DL.Rgb24 _bar = new DL.Rgb24(100, 200, 120);
        private DL.Rgb24 _bg = new DL.Rgb24(20, 20, 20);

        public void SetItems(IEnumerable<(string Label, double Value)> items)
        {
            _items.Clear();
            if (items != null) _items.AddRange(items);
        }
        public void SetBarColor(DL.Rgb24 color) => _bar = color;

        public (int Width, int Height) Measure()
        {
            int labelWidth = _items.Count == 0 ? 0 : _items.Max(i => i.Label.Length);
            return (labelWidth + 1 + 10, Math.Max(1, _items.Count));
        }

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x = (int)rect.X; int y = (int)rect.Y; int w = (int)rect.Width; int h = (int)rect.Height;
            if (w <= 0 || h <= 0) return;
            b.PushClip(new DL.ClipPush(x, y, w, h));
            b.DrawRect(new DL.Rect(x, y, w, h, _bg));
            if (_items.Count == 0) { b.Pop(); return; }

            int labelWidth = Math.Min(w / 2, Math.Max(0, _items.Max(i => i.Label.Length)));
            int barWidth = Math.Max(1, w - labelWidth - 2);
            double max = Math.Max(1e-6, _items.Max(i => Math.Max(0, i.Value)));
            for (int i = 0; i < Math.Min(h, _items.Count); i++)
            {
                var (label, value) = _items[i];
                string lbl = label.Length > labelWidth ? label.Substring(0, labelWidth) : label.PadRight(labelWidth);
                b.DrawText(new DL.TextRun(x, y + i, lbl, _fg, null, DL.CellAttrFlags.None));
                int fill = (int)Math.Round(Math.Clamp(value, 0, max) / max * barWidth);
                for (int j = 0; j < fill; j++)
                {
                    b.DrawText(new DL.TextRun(x + labelWidth + 1 + j, y + i, "█", _bar, null, DL.CellAttrFlags.None));
                }
            }
            b.Pop();
        }
    }
}
