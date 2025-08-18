using Andy.Tui.DisplayList;
using Andy.Tui.Layout;

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
    void Render(in T item, int index, in Rect slot, DisplayList baseDl, DisplayListBuilder builder);
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
}
