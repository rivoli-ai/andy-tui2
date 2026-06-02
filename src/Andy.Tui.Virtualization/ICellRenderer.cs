using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Virtualization;

public interface ICellRenderer<T>
{
    void Render(in T item, int row, int col, in L.Rect slot, DL.DisplayList baseDl, DL.DisplayListBuilder builder);
}
