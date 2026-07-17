namespace Andy.Tui.Virtualization;

public sealed class MeasureCache
{
    private readonly Dictionary<string, int> _rowHeights = new();
    public void Set(string key, int rowHeight) => _rowHeights[key] = rowHeight;
    public bool TryGet(string key, out int height) => _rowHeights.TryGetValue(key, out height);

    /// <summary>Drop all cached heights — e.g. when the available width (and thus wrapping) changes.</summary>
    public void Clear() => _rowHeights.Clear();

    /// <summary>Number of cached measurements. Exposed for invalidation tests/diagnostics.</summary>
    public int Count => _rowHeights.Count;
}
