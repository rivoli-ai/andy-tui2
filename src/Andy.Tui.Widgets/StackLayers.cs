using System;
using System.Collections.Generic;
using L = Andy.Tui.Layout;
using DL = Andy.Tui.DisplayList;

namespace Andy.Tui.Widgets
{
    public sealed class StackLayers
    {
        private readonly List<Action<DL.DisplayList, DL.DisplayListBuilder>> _layers = new();
        private DL.Rgb24 _bg = new DL.Rgb24(0, 0, 0);

        public void Clear() => _layers.Clear();
        public void AddLayer(Action<DL.DisplayList, DL.DisplayListBuilder> draw) { if (draw != null) _layers.Add(draw); }
        public (int Width, int Height) Measure() => (0, 0);

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            // Base background fill
            b.DrawRect(new DL.Rect((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height, _bg));
            foreach (var layer in _layers)
            {
                layer(baseDl, b);
            }
        }
    }
}
