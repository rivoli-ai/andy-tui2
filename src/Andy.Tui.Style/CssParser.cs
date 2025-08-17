namespace Andy.Tui.Style;

using System.Text;

/// <summary>
/// Minimal CSS subset parser for Andy.Tui.
/// Supports: type, .class, #id, :pseudo, AND combinator (no descendant),
/// declarations of known properties as strings, and a limited @media (min-width/max-width, prefers-reduced-motion, is-terminal).
/// Values are kept mostly as strings; StyleResolver handles strong typing.
/// </summary>
public static class CssParser
{
    public static Stylesheet Parse(string css, int baseSourceOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(css)) return Stylesheet.Empty;
        var rules = new List<Rule>();
        int i = 0;
        int order = baseSourceOrder;
        while (TryReadUntil(ref i, css, '{', out var selectorText))
        {
            var selector = ParseSelector(selectorText);
            if (!TryReadUntil(ref i, css, '}', out var declsText)) break;

            var decls = ParseDeclarations(declsText);
            Func<EnvironmentContext, bool>? media = null;

            // Support simple @media preceding the selector block: e.g., @media(min-width:40)
            // We backtrack around the selector to see if it started with @media(
            var trimmedSel = selectorText.TrimStart();
            if (trimmedSel.StartsWith("@media", StringComparison.OrdinalIgnoreCase))
            {
                media = ParseMediaCondition(trimmedSel);
                // The real selector part after ")" if any
                int close = trimmedSel.IndexOf(')');
                if (close >= 0 && close + 1 < trimmedSel.Length)
                {
                    selector = ParseSelector(trimmedSel[(close + 1)..]);
                }
            }

            rules.Add(new Rule(selector, decls, order++, media));
        }
        return new Stylesheet(rules);
    }

    private static bool TryReadUntil(ref int i, string s, char end, out string chunk)
    {
        var sb = new StringBuilder();
        bool inComment = false;
        for (; i < s.Length; i++)
        {
            char c = s[i];
            if (!inComment && c == '/' && i + 1 < s.Length && s[i + 1] == '*')
            {
                inComment = true; i++; continue;
            }
            if (inComment && c == '*' && i + 1 < s.Length && s[i + 1] == '/')
            {
                inComment = false; i++; continue;
            }
            if (inComment) continue;
            if (c == end)
            {
                i++; // consume end
                break;
            }
            sb.Append(c);
        }
        chunk = sb.ToString();
        return chunk.Length > 0;
    }

    private static Selector ParseSelector(string text)
    {
        // Support compound simple selectors without descendant combinators, e.g.:
        // "button:hover", ".btn.primary:hover", "#id.cell".
        var tokens = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        Selector? current = null;
        foreach (var raw in tokens)
        {
            foreach (var part in ParseSimpleSequence(raw.Trim()))
            {
                current = current is null ? part : new AndSelector(current, part);
            }
        }
        return current ?? new TypeSelector("*");
    }

    private static IEnumerable<Selector> ParseSimpleSequence(string token)
    {
        int i = 0;
        bool seenType = false;
        while (i < token.Length)
        {
            char c = token[i];
            if (c == '#')
            {
                i++;
                var id = ReadIdent(token, ref i);
                if (id.Length > 0) yield return new IdSelector(id);
                continue;
            }
            if (c == '.')
            {
                i++;
                var cls = ReadIdent(token, ref i);
                if (cls.Length > 0) yield return new ClassSelector(cls);
                continue;
            }
            if (c == ':')
            {
                i++;
                var name = ReadIdent(token, ref i);
                if (name.Length > 0) yield return new PseudoClassSelector(":" + name);
                continue;
            }
            // type selector, only once at the beginning of a sequence until a special char
            if (!seenType)
            {
                var type = ReadIdent(token, ref i);
                if (type.Length > 0)
                {
                    seenType = true;
                    yield return new TypeSelector(type);
                    continue;
                }
            }
            // Unknown char: advance to avoid infinite loop
            i++;
        }
    }

    private static string ReadIdent(string s, ref int i)
    {
        int start = i;
        while (i < s.Length)
        {
            char ch = s[i];
            if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_') { i++; continue; }
            break;
        }
        return s[start..i];
    }

    private static Dictionary<string, object> ParseDeclarations(string text)
    {
        var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        var pairs = text.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in pairs)
        {
            var idx = p.IndexOf(':');
            if (idx <= 0) continue;
            var name = p[..idx].Trim();
            var valueRaw = p[(idx + 1)..].Trim();
            if (name.Length == 0 || valueRaw.Length == 0) continue;

            // Keep as string; StyleResolver will parse known ones. Map padding/margin shorthands to Thickness string sentinel.
            if (string.Equals(name, "padding", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "margin", StringComparison.OrdinalIgnoreCase))
            {
                // Very light shorthand splitter: 1-4 parts numeric assumed px unit-less
                var parts = valueRaw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1)
                {
                    if (TryNum(parts[0], out var v)) dict[name] = new Thickness(new Length(v), new Length(v), new Length(v), new Length(v));
                }
                else if (parts.Length == 2)
                {
                    if (TryNum(parts[0], out var v1) && TryNum(parts[1], out var v2))
                        dict[name] = new Thickness(new Length(v2), new Length(v1), new Length(v2), new Length(v1)); // left/right=v2, top/bottom=v1
                }
                else if (parts.Length == 3)
                {
                    if (TryNum(parts[0], out var v1) && TryNum(parts[1], out var v2) && TryNum(parts[2], out var v3))
                        dict[name] = new Thickness(new Length(v2), new Length(v1), new Length(v2), new Length(v3));
                }
                else if (parts.Length >= 4)
                {
                    if (TryNum(parts[0], out var v1) && TryNum(parts[1], out var v2) && TryNum(parts[2], out var v3) && TryNum(parts[3], out var v4))
                        dict[name] = new Thickness(new Length(v4), new Length(v1), new Length(v2), new Length(v3)); // LTRB order expected separately; we map left,top,right,bottom
                }
                continue;
            }

            dict[name] = valueRaw;
        }
        return dict;
    }

    private static bool TryNum(string token, out double value)
    {
        token = token.Trim();
        // Preserve percentages for later resolution; do not coerce to px
        if (token.EndsWith("%", StringComparison.Ordinal)) { value = 0; return false; }
        // Allow simple px suffix like 10px
        return double.TryParse(token.TrimEnd('p', 'x'), out value);
    }

    private static Func<EnvironmentContext, bool>? ParseMediaCondition(string mediaText)
    {
        // Supports: @media(min-width: N), @media(max-width: N), @media(prefers-reduced-motion), @media(is-terminal)
        int open = mediaText.IndexOf('(');
        int close = mediaText.LastIndexOf(')');
        if (open < 0 || close <= open) return null;
        var inner = mediaText.Substring(open + 1, close - open - 1).Trim();
        if (inner.StartsWith("min-width", StringComparison.OrdinalIgnoreCase))
        {
            if (TryNum(inner.Split(':', 2)[1], out var n))
                return env => env.ViewportWidth >= n;
        }
        if (inner.StartsWith("max-width", StringComparison.OrdinalIgnoreCase))
        {
            if (TryNum(inner.Split(':', 2)[1], out var n))
                return env => env.ViewportWidth <= n;
        }
        if (inner.Equals("prefers-reduced-motion", StringComparison.OrdinalIgnoreCase))
        {
            return env => env.PrefersReducedMotion;
        }
        if (inner.Equals("is-terminal", StringComparison.OrdinalIgnoreCase))
        {
            return env => env.IsTerminal;
        }
        return null;
    }
}
