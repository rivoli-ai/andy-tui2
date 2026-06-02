namespace Andy.Tui.Virtualization;

public sealed class MeasureCache
{
    private readonly Dictionary<string, int> _rowHeights = new();
    public void Set(string key, int rowHeight) => _rowHeights[key] = rowHeight;
    public bool TryGet(string key, out int height) => _rowHeights.TryGetValue(key, out height);
}
