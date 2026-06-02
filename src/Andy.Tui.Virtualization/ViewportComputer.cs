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
}
