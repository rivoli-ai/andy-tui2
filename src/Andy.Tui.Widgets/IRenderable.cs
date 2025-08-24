using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets;

public interface IRenderable
{
    void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder);
}

public delegate void RenderFn(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder);
