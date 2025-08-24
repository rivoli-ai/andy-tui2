using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;
using Andy.Tui.Virtualization;

namespace Andy.Tui.Widgets;

public sealed class VirtualizedList<T>
{
    private readonly IVirtualizedCollection<T> _items;
    private readonly IItemRenderer<T> _renderer;
    private OverscanPolicy _overscan;
    private int _firstRow;
    private int _rowCount;
    private int _recentDeltaRows;
    private Func<int, int>? _measureByIndex;

    public VirtualizedList(IVirtualizedCollection<T> items, IItemRenderer<T> renderer, OverscanPolicy? overscan = null)
    {
        _items = items;
        _renderer = renderer;
        _overscan = overscan ?? new OverscanPolicy(2, 4, Adaptive: false);
        _firstRow = 0; _rowCount = 10;
    }

    public void SetViewportRows(int firstRow, int rowCount)
    {
        _firstRow = Math.Max(0, firstRow);
        _rowCount = Math.Max(1, rowCount);
    }

    public void SetOverscan(OverscanPolicy policy) => _overscan = policy;
    public void UpdateScrollDelta(int deltaRows) => _recentDeltaRows = deltaRows;
    public void SetMeasureByIndex(Func<int, int> measureByIndex) => _measureByIndex = measureByIndex;

    public void Render(in L.Rect viewportRect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        var vp = new ViewportState(_firstRow, _rowCount, (int)viewportRect.Width, (int)viewportRect.Height, 0, 0);
        (int first, int last) window = _measureByIndex is not null
            ? ViewportComputer.ComputeWindowMeasuredByIndex(_items, vp, _overscan, _measureByIndex)
            : (_overscan.Adaptive
                ? ViewportComputer.ComputeWindowGenericAdaptive(_items, vp, _overscan, _recentDeltaRows, _items.GetKey, _ => 1)
                : ViewportComputer.ComputeWindowGeneric(_items, vp, _overscan, _items.GetKey, _ => 1));
        var (first, last) = window;

        int y = (int)viewportRect.Y;
        for (int i = first; i <= last; i++)
        {
            var slot = new L.Rect((int)viewportRect.X, y, (int)viewportRect.Width, 1);
            _renderer.Render(_items[i], i, slot, baseDl, builder);
            y += 1;
            if (y >= viewportRect.Y + viewportRect.Height) break;
        }
    }
}
