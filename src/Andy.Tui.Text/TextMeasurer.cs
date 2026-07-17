namespace Andy.Tui.Text;

/// <summary>
/// Caching text measurer for terminal cells. Width is computed per grapheme
/// cluster via the shared <see cref="TerminalText"/> service so measurement,
/// layout, and rendering all agree on the same column count.
/// </summary>
public sealed class TextMeasurer
{
    private readonly Dictionary<string, int> _cache = new();

    public int MeasureWidth(string text)
    {
        text ??= string.Empty;
        if (_cache.TryGetValue(text, out var cached)) return cached;
        int width = TerminalText.MeasureWidth(text);
        _cache[text] = width;
        return width;
    }
}
