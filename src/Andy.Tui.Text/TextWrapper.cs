using System.Text;

namespace Andy.Tui.Text;

/// <summary>
/// Wraps text into lines based on grapheme clusters and simple word boundaries.
/// </summary>
public sealed class TextWrapper
{
    private readonly TextMeasurer _measurer = new();

    public IReadOnlyList<string> Wrap(string text, WrapOptions options)
    {
        text ??= string.Empty;
        if (options.MaxWidth <= 0 || options.Strategy == WrapStrategy.NoWrap)
        {
            return new[] { text };
        }

        return options.Strategy switch
        {
            WrapStrategy.WordWrap => WordWrap(text, options.MaxWidth),
            WrapStrategy.CharacterWrap => CharacterWrap(text, options.MaxWidth),
            _ => new[] { text }
        };
    }

    private IReadOnlyList<string> CharacterWrap(string text, int maxWidth)
    {
        var lines = new List<string>();
        var sb = new StringBuilder();
        int current = 0;
        foreach (var g in new GraphemeEnumerator(text))
        {
            if (current + 1 > maxWidth)
            {
                lines.Add(sb.ToString());
                sb.Clear();
                current = 0;
            }
            sb.Append(g);
            current++;
        }
        lines.Add(sb.ToString());
        return lines;
    }

    private IReadOnlyList<string> WordWrap(string text, int maxWidth)
    {
        var words = SplitWords(text);
        var lines = new List<string>();
        var sb = new StringBuilder();
        int current = 0;
        foreach (var word in words)
        {
            int w = _measurer.MeasureWidth(word);
            if (current == 0)
            {
                sb.Append(word);
                current += w;
                continue;
            }
            if (current + 1 + w <= maxWidth)
            {
                sb.Append(' ').Append(word);
                current += 1 + w;
            }
            else
            {
                lines.Add(sb.ToString());
                sb.Clear();
                sb.Append(word);
                current = w;
            }
        }
        lines.Add(sb.ToString());
        return lines;
    }

    private static IEnumerable<string> SplitWords(string text)
    {
        int i = 0;
        while (i < text.Length)
        {
            while (i < text.Length && char.IsWhiteSpace(text, i)) i++;
            if (i >= text.Length) yield break;
            int start = i;
            while (i < text.Length && !char.IsWhiteSpace(text, i)) i++;
            yield return text.Substring(start, i - start);
        }
    }
}
