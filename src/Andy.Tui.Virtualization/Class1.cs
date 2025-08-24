using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Virtualization;

public interface IVirtualizedCollection<T>
{
    int Count { get; }
    T this[int index] { get; }
    string GetKey(int index);
}

public readonly record struct ViewportState(int FirstRow, int RowCount, int Cols, int Rows, int PixelWidth, int PixelHeight);
public readonly record struct OverscanPolicy(int Before, int After, bool Adaptive);

public interface IItemRenderer<T>
{
    void Render(in T item, int index, in L.Rect slot, DL.DisplayList baseDl, DL.DisplayListBuilder builder);
}

public interface IGridProvider<T>
{
    int RowCount { get; }
    int ColCount { get; }
    T GetItem(int row, int col);
    string GetKey(int row, int col);
}

public interface ICellRenderer<T>
{
    void Render(in T item, int row, int col, in L.Rect slot, DL.DisplayList baseDl, DL.DisplayListBuilder builder);
}

public sealed class MeasureCache
{
    private readonly Dictionary<string, int> _rowHeights = new();
    public void Set(string key, int rowHeight) => _rowHeights[key] = rowHeight;
    public bool TryGet(string key, out int height) => _rowHeights.TryGetValue(key, out height);
}

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

public readonly record struct GridViewportState(int FirstRow, int RowCount, int FirstCol, int ColCount);

public static class GridViewportComputer
{
    public static (int RowStart, int RowEnd, int ColStart, int ColEnd) ComputeWindow<T>(IGridProvider<T> provider, GridViewportState vp, OverscanPolicy rowOverscan, OverscanPolicy colOverscan)
    {
        int r0 = Math.Max(0, vp.FirstRow - rowOverscan.Before);
        int r1 = Math.Min(provider.RowCount - 1, vp.FirstRow + vp.RowCount + rowOverscan.After - 1);
        int c0 = Math.Max(0, vp.FirstCol - colOverscan.Before);
        int c1 = Math.Min(provider.ColCount - 1, vp.FirstCol + vp.ColCount + colOverscan.After - 1);
        return (r0, r1, c0, c1);
    }
}
