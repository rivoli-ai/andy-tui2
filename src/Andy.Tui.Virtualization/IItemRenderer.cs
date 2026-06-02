using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Virtualization;

public interface IItemRenderer<T>
{
    void Render(in T item, int index, in L.Rect slot, DL.DisplayList baseDl, DL.DisplayListBuilder builder);
}
