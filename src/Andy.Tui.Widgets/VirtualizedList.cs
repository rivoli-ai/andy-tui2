using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;
using Andy.Tui.Virtualization;

namespace Andy.Tui.Widgets;

public sealed class VirtualizedList<T>
{
    private readonly IVirtualizedCollection<T> _items;
    private readonly IItemRenderer<T> _renderer;
    private OverscanPolicy _overscan;
    // Scroll offset of the viewport top, measured in terminal rows from the top of the content —
    // a row offset, NOT an item index. For fixed heights the two coincide; for variable heights the
    // offset is mapped onto items by ComputeLayout.
    private int _scrollTopRow;
    private int _rowCount;
    private int _recentDeltaRows;
    private Func<int, int>? _measureByIndex;
    private readonly MeasureCache _measureCache = new();
    private readonly CumulativeHeightIndex _heightIndex = new();
    private int _lastWidth = -1;

    public VirtualizedList(IVirtualizedCollection<T> items, IItemRenderer<T> renderer, OverscanPolicy? overscan = null)
    {
        _items = items;
        _renderer = renderer;
        _overscan = overscan ?? new OverscanPolicy(2, 4, Adaptive: false);
        _scrollTopRow = 0; _rowCount = 10;
    }

    /// <summary>
    /// Set the viewport scroll position and height. <paramref name="firstRow"/> is a scroll offset
    /// in terminal rows (0 == top of content), independent of item indices.
    /// </summary>
    public void SetViewportRows(int firstRow, int rowCount)
    {
        _scrollTopRow = Math.Max(0, firstRow);
        _rowCount = Math.Max(1, rowCount);
    }

    public void SetOverscan(OverscanPolicy policy) => _overscan = policy;
    public void UpdateScrollDelta(int deltaRows) => _recentDeltaRows = deltaRows;

    public void SetMeasureByIndex(Func<int, int> measureByIndex)
    {
        _measureByIndex = measureByIndex;
        _measureCache.Clear();
        _heightIndex.Invalidate();
    }

    /// <summary>Drop cached item heights so they are re-measured on the next render (e.g. after a content change).</summary>
    public void InvalidateMeasurements()
    {
        _measureCache.Clear();
        _heightIndex.Invalidate();
    }

    public void Render(in L.Rect viewportRect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        int vpX = (int)viewportRect.X;
        int vpY = (int)viewportRect.Y;
        int vpW = (int)viewportRect.Width;
        int vpRows = (int)viewportRect.Height;
        if (vpRows <= 0) vpRows = _rowCount;

        // A width change invalidates any width-dependent measurements (e.g. wrapping) so heights
        // are recomputed rather than reused across a resize.
        if (vpW != _lastWidth)
        {
            _lastWidth = vpW;
            _measureCache.Clear();
            _heightIndex.Invalidate();
        }

        // Measure through a per-item-key cache so heights are computed once per width and reused,
        // using the collection's stable keys to survive reordering.
        Func<int, int>? measure = _measureByIndex is null ? null : MeasureCached;

        // For variable-height content a persistent prefix-sum index locates the first visible item
        // in O(log n) so deep scrolls stay O(window + log n) instead of re-measuring from index 0.
        var layout = ViewportComputer.ComputeLayout(_items, _scrollTopRow, vpRows, _overscan, measure, _heightIndex, _recentDeltaRows);
        if (layout.IsEmpty) return;

        // Clip to the viewport so before/after-overscan and partially scrolled items perform their
        // render work without painting visible content outside the viewport rows.
        builder.PushClip(new DL.ClipPush(vpX, vpY, vpW, vpRows));
        foreach (var s in layout.Slots)
        {
            var slot = new L.Rect(vpX, vpY + s.Top, vpW, s.Height);
            _renderer.Render(_items[s.Index], s.Index, slot, baseDl, builder);
        }
        builder.Pop();
    }

    private int MeasureCached(int index)
    {
        string key = _items.GetKey(index);
        if (_measureCache.TryGet(key, out int cached)) return cached;
        int h = Math.Max(1, _measureByIndex!(index));
        _measureCache.Set(key, h);
        return h;
    }
}
