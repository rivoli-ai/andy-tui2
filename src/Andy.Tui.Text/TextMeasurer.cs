namespace Andy.Tui.Text;

/// <summary>
/// Simple text measurer for terminal cells.
/// Width is computed per grapheme, using East Asian wide approximation (2 cells for wide/emoji).
/// Combining marks within a grapheme do not add width.
/// </summary>
public sealed class TextMeasurer
{
    private readonly Dictionary<string, int> _cache = new();

    public int MeasureWidth(string text)
    {
        text ??= string.Empty;
        if (_cache.TryGetValue(text, out var cached)) return cached;
        int width = 0;
        foreach (var g in new GraphemeEnumerator(text))
        {
            width += MeasureGraphemeWidth(g);
        }
        _cache[text] = width;
        return width;
    }

    private static int MeasureGraphemeWidth(string grapheme)
    {
        // If any code point in the grapheme is wide, width=2; else if all non-spacing, width=0; else width=1
        bool hasBase = false;
        bool isWide = false;
        foreach (var cp in EnumerateCodePoints(grapheme))
        {
            int w = WcWidth.GetCharWidth(cp);
            if (w == 2) isWide = true;
            if (w == 1) hasBase = true;
        }
        if (isWide) return 2;
        if (hasBase) return 1;
        return 0;
    }

    private static IEnumerable<int> EnumerateCodePoints(string s)
    {
        for (int i = 0; i < s.Length; i++)
        {
            int cp = char.IsSurrogatePair(s, i) ? char.ConvertToUtf32(s, i++) : s[i];
            yield return cp;
        }
    }
}
