using System.Collections;
using System.Globalization;

namespace Andy.Tui.Text;

/// <summary>
/// Iterates Unicode grapheme clusters (user-perceived characters) using StringInfo as a baseline.
/// </summary>
public sealed class GraphemeEnumerator : IEnumerable<string>
{
    private readonly string _text;

    public GraphemeEnumerator(string text)
    {
        _text = text ?? string.Empty;
    }

    public IEnumerator<string> GetEnumerator()
    {
        if (_text.Length == 0)
        {
            yield break;
        }
        var si = new StringInfo(_text);
        int index = 0;
        while (index < _text.Length)
        {
            int len = StringInfo.GetNextTextElementLength(_text, index);
            yield return _text.Substring(index, len);
            index += len;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
