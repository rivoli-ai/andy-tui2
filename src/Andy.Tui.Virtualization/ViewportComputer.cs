namespace Andy.Tui.Virtualization;

public static class ViewportComputer
{
    public static (int FirstIndex, int LastIndex) ComputeWindow(IVirtualizedCollection<object> collection, ViewportState vp, OverscanPolicy overscan, Func<int, string> keyAt, Func<string, int> measure)
    {
        // MVP: assume fixed row height of 1 cell; later integrate measure
        int first = Math.Max(0, vp.FirstRow - overscan.Before);
        int last = Math.Min(collection.Count - 1, vp.FirstRow + vp.RowCount + overscan.After - 1);
        return (first, last);
    }

    public static (int FirstIndex, int LastIndex) ComputeWindowGeneric<T>(IVirtualizedCollection<T> collection, ViewportState vp, OverscanPolicy overscan, Func<int, string> keyAt, Func<string, int> measure)
    {
        int first = Math.Max(0, vp.FirstRow - overscan.Before);
        int last = Math.Min(collection.Count - 1, vp.FirstRow + vp.RowCount + overscan.After - 1);
        return (first, last);
    }

    public static (int FirstIndex, int LastIndex) ComputeWindowGenericAdaptive<T>(IVirtualizedCollection<T> collection, ViewportState vp, OverscanPolicy overscan, int recentDeltaRows, Func<int, string> keyAt, Func<string, int> measure)
    {
        int before = overscan.Before;
        int after = overscan.After;
        if (overscan.Adaptive)
        {
            int add = Math.Min(vp.RowCount, Math.Abs(recentDeltaRows));
            if (recentDeltaRows > 0) after += add; // scrolling down
            else if (recentDeltaRows < 0) before += add; // scrolling up
            else { before += add / 2; after += add / 2; }
        }
        int first = Math.Max(0, vp.FirstRow - before);
        int last = Math.Min(collection.Count - 1, vp.FirstRow + vp.RowCount + after - 1);
        return (first, last);
    }

    public static (int FirstIndex, int LastIndex) ComputeWindowMeasuredByIndex<T>(IVirtualizedCollection<T> collection, ViewportState vp, OverscanPolicy overscan, Func<int, int> measureByIndex)
    {
        // Guard: an empty collection has no renderable index.
        if (collection.Count <= 0) return (0, -1);
        // Treat OverscanPolicy.Before/After and vp.RowCount as row-height units (terminal rows)
        int first = vp.FirstRow;
        int needBefore = overscan.Before;
        while (needBefore > 0 && first > 0)
        {
            first--;
            needBefore -= Math.Max(1, measureByIndex(first));
        }

        int idx = vp.FirstRow;
        int needMainAndAfter = vp.RowCount + overscan.After;
        while (needMainAndAfter > 0 && idx < collection.Count)
        {
            needMainAndAfter -= Math.Max(1, measureByIndex(idx));
            idx++;
        }
        int last = Math.Max(first, Math.Min(collection.Count - 1, idx - 1));
        return (first, last);
    }

    /// <summary>
    /// Resolve the items intersecting a viewport and place each one in terminal rows.
    /// </summary>
    /// <param name="collection">The virtualized item source.</param>
    /// <param name="scrollTopRow">
    /// The scroll offset of the viewport top, measured in terminal rows from the top of the
    /// content — independent of item indices. For fixed row heights this equals the index of the
    /// first item; for variable heights it is mapped onto items via their measured heights.
    /// </param>
    /// <param name="viewportRows">Viewport height in terminal rows.</param>
    /// <param name="overscan">Overscan policy controlling extra rows rendered above/below the viewport.</param>
    /// <param name="measureByIndex">
    /// Per-index row height; <c>null</c> means every item is exactly one row. Values are clamped
    /// to a minimum of 1 so a mis-measured 0 never stalls advancement.
    /// </param>
    /// <param name="recentDeltaRows">
    /// Recent scroll delta in rows, used only when <see cref="OverscanPolicy.Adaptive"/> is set to
    /// grow overscan in the scroll direction.
    /// </param>
    /// <remarks>
    /// Slots are positioned relative to the viewport top row. Before-overscan items and a partially
    /// scrolled first item get negative <see cref="ItemSlot.Top"/>; after-overscan items may extend
    /// past the viewport bottom. The returned layout never contains an out-of-range index, so empty
    /// and rapidly shrinking collections are safe.
    /// </remarks>
    public static VirtualLayout ComputeLayout<T>(
        IVirtualizedCollection<T> collection,
        int scrollTopRow,
        int viewportRows,
        OverscanPolicy overscan,
        Func<int, int>? measureByIndex,
        int recentDeltaRows = 0)
        => ComputeLayout(collection, scrollTopRow, viewportRows, overscan, measureByIndex, heightIndex: null, recentDeltaRows);

    /// <summary>
    /// Overload that accepts a persistent <see cref="CumulativeHeightIndex"/>. For variable-height
    /// content the index lets the first visible item be located in O(log n) via binary search over
    /// cached prefix sums, so a deep-scrolled layout pass is O(window + log n) instead of walking
    /// (and measuring) every item from index 0. The produced layout is identical to the non-indexed
    /// path; only the work to reach the window differs. Pass <c>null</c> to use the linear walk.
    /// </summary>
    public static VirtualLayout ComputeLayout<T>(
        IVirtualizedCollection<T> collection,
        int scrollTopRow,
        int viewportRows,
        OverscanPolicy overscan,
        Func<int, int>? measureByIndex,
        CumulativeHeightIndex? heightIndex,
        int recentDeltaRows = 0)
    {
        int n = collection.Count;
        if (n <= 0 || viewportRows <= 0) return VirtualLayout.Empty;

        scrollTopRow = Math.Max(0, scrollTopRow);

        // Adaptive overscan grows in the direction of travel; symmetric when idle.
        int before = overscan.Before;
        int after = overscan.After;
        if (overscan.Adaptive)
        {
            int add = Math.Min(viewportRows, Math.Abs(recentDeltaRows));
            if (recentDeltaRows > 0) after += add;
            else if (recentDeltaRows < 0) before += add;
            else { before += add / 2; after += add / 2; }
        }
        before = Math.Max(0, before);
        after = Math.Max(0, after);

        // The content-row window we must cover, including overscan (windowBottom exclusive).
        int windowTop = scrollTopRow - before;              // may be negative
        int windowBottom = scrollTopRow + viewportRows + after;

        var slots = new List<ItemSlot>();

        if (measureByIndex is null)
        {
            // Fixed height 1: item i occupies content row i, so map arithmetically without walking.
            int firstIndex = Math.Max(0, windowTop);
            int lastIndex = Math.Min(n - 1, windowBottom - 1);
            for (int i = firstIndex; i <= lastIndex; i++)
                slots.Add(new ItemSlot(i, i - scrollTopRow, 1));
        }
        else if (heightIndex is not null)
        {
            // Prefix-sum fast path: measure/cache heights once, then binary-search to the first
            // item intersecting the window instead of walking from index 0 every frame.
            heightIndex.EnsureBuilt(n, measureByIndex);
            int start = heightIndex.FirstIntersecting(windowTop);
            for (int i = start; i < n; i++)
            {
                long top = heightIndex.TopOf(i);
                if (top >= windowBottom) break;             // past the window; nothing further intersects
                int h = heightIndex.HeightOf(i);
                slots.Add(new ItemSlot(i, (int)(top - scrollTopRow), h));
            }
        }
        else
        {
            int top = 0;
            for (int i = 0; i < n; i++)
            {
                int h = Math.Max(1, measureByIndex(i));
                int bottom = top + h;
                if (top >= windowBottom) break;             // past the window; nothing further intersects
                if (bottom > windowTop)                     // intersects the window
                    slots.Add(new ItemSlot(i, top - scrollTopRow, h));
                top = bottom;
            }
        }

        if (slots.Count == 0) return VirtualLayout.Empty;
        return new VirtualLayout(slots, slots[0].Index, slots[^1].Index);
    }
}
