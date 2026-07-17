namespace Andy.Tui.Style;

using System.Globalization;
using System.Text;

/// <summary>
/// Minimal CSS subset parser for Andy.Tui.
///
/// Supported grammar:
///   stylesheet   := ( rule | at-media )*
///   at-media     := '@media' feature+ '{' rule* '}'          (standard nested form)
///                 | '@media' feature+ selector '{' decls '}'  (legacy prefix form)
///   feature      := '(' ('min-width'|'max-width'|'min-height'|'max-height') ':' length ')'
///                 | '(' 'prefers-reduced-motion' [':' ('reduce'|'no-preference')] ')'
///                 | '(' 'is-terminal' ')'
///                 (multiple features may be joined with 'and')
///   rule         := selector '{' decls '}'
///   selector     := simple-sequence            (compound simple selectors only)
///   simple-seq   := ( type | '.' class | '#' id | ':' pseudo | '*' )+
///   decls        := ( property ':' value ';' )*
///
/// Descendant/child/sibling combinators and selector lists are NOT supported and are
/// rejected with a diagnostic rather than silently mis-applied. Lengths accept unitless
/// and 'px' values consistently; percentages are preserved. Unknown properties and
/// malformed values produce diagnostics on the returned <see cref="Stylesheet"/>.
/// </summary>
public static class CssParser
{
    private static readonly HashSet<string> KnownProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "display", "flex-direction", "flex-wrap", "justify-content", "align-items",
        "align-self", "align-content", "order", "flex-grow", "flex-shrink", "flex-basis",
        "width", "height", "min-width", "min-height", "max-width", "max-height",
        "row-gap", "column-gap", "gap",
        "padding", "padding-left", "padding-top", "padding-right", "padding-bottom",
        "margin", "margin-left", "margin-top", "margin-right", "margin-bottom",
        "overflow", "color", "background-color",
        "font-weight", "font-style", "text-decoration",
    };

    public static Stylesheet Parse(string css, int baseSourceOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(css)) return Stylesheet.Empty;
        var clean = StripComments(css);
        var rules = new List<Rule>();
        var diagnostics = new List<CssDiagnostic>();
        int order = baseSourceOrder;
        ParseBlock(clean, rules, diagnostics, ref order, null);
        return new Stylesheet(rules, diagnostics);
    }

    /// <summary>Remove <c>/* ... */</c> comments, replacing each with a single space.</summary>
    private static string StripComments(string s)
    {
        var sb = new StringBuilder(s.Length);
        bool inComment = false;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (!inComment && c == '/' && i + 1 < s.Length && s[i + 1] == '*')
            {
                inComment = true; i++; continue;
            }
            if (inComment && c == '*' && i + 1 < s.Length && s[i + 1] == '/')
            {
                inComment = false; i++; sb.Append(' '); continue;
            }
            if (inComment) continue;
            sb.Append(c);
        }
        return sb.ToString();
    }

    private static void ParseBlock(string s, List<Rule> rules, List<CssDiagnostic> diagnostics, ref int order, Func<EnvironmentContext, bool>? media)
    {
        int i = 0;
        while (i < s.Length)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
            if (i >= s.Length) break;

            int braceOpen = s.IndexOf('{', i);
            if (braceOpen < 0)
            {
                var stray = s[i..].Trim();
                if (stray.Length > 0)
                    diagnostics.Add(CssDiagnostic.Error($"Ignored content with no declaration block: '{Truncate(stray)}'."));
                break;
            }

            string prelude = s[i..braceOpen].Trim();
            int braceClose = FindMatchingBrace(s, braceOpen);
            if (braceClose < 0)
            {
                diagnostics.Add(CssDiagnostic.Error($"Unterminated '{{' block for '{Truncate(prelude)}'."));
                break;
            }
            string body = s[(braceOpen + 1)..braceClose];
            i = braceClose + 1;

            if (prelude.StartsWith("@media", StringComparison.OrdinalIgnoreCase))
            {
                HandleMedia(prelude, body, rules, diagnostics, ref order, media);
            }
            else if (prelude.StartsWith("@", StringComparison.Ordinal))
            {
                diagnostics.Add(CssDiagnostic.Warning($"Unsupported at-rule '{Truncate(prelude)}' ignored."));
            }
            else
            {
                AddRule(prelude, body, rules, diagnostics, ref order, media);
            }
        }
    }

    private static void HandleMedia(string prelude, string body, List<Rule> rules, List<CssDiagnostic> diagnostics, ref int order, Func<EnvironmentContext, bool>? outer)
    {
        var cond = ParseMediaCondition(prelude, diagnostics);
        if (cond is null) return; // invalid media: diagnostic already emitted, drop the block

        var combined = outer is null ? cond : (Func<EnvironmentContext, bool>)(env => outer(env) && cond(env));

        string trailingSelector = SelectorAfterMedia(prelude);
        if (trailingSelector.Length > 0)
        {
            // Legacy prefix form: @media(feature) selector { decls }
            AddRule(trailingSelector, body, rules, diagnostics, ref order, combined);
        }
        else
        {
            // Standard nested form: @media (feature) { rule* }
            ParseBlock(body, rules, diagnostics, ref order, combined);
        }
    }

    private static void AddRule(string selectorText, string body, List<Rule> rules, List<CssDiagnostic> diagnostics, ref int order, Func<EnvironmentContext, bool>? media)
    {
        var selector = ParseSelector(selectorText, diagnostics);
        if (selector is null) return; // rejected selector: diagnostic already emitted
        var decls = ParseDeclarations(body, diagnostics);
        rules.Add(new Rule(selector, decls, order++, media));
    }

    private static int FindMatchingBrace(string s, int openIndex)
    {
        int depth = 0;
        for (int i = openIndex; i < s.Length; i++)
        {
            if (s[i] == '{') depth++;
            else if (s[i] == '}')
            {
                depth--;
                if (depth == 0) return i;
            }
        }
        return -1;
    }

    private static int FindMatchingParen(string s, int openIndex)
    {
        int depth = 0;
        for (int i = openIndex; i < s.Length; i++)
        {
            if (s[i] == '(') depth++;
            else if (s[i] == ')')
            {
                depth--;
                if (depth == 0) return i;
            }
        }
        return -1;
    }

    /// <summary>The selector portion (if any) trailing a legacy prefix <c>@media(...) selector</c>.</summary>
    private static string SelectorAfterMedia(string prelude)
    {
        int close = prelude.LastIndexOf(')');
        if (close < 0 || close + 1 >= prelude.Length) return string.Empty;
        return prelude[(close + 1)..].Trim();
    }

    private static Selector? ParseSelector(string text, List<CssDiagnostic> diagnostics)
    {
        text = text.Trim();
        if (text.Length == 0) return new UniversalSelector();

        if (text.Contains(','))
        {
            diagnostics.Add(CssDiagnostic.Warning($"Selector lists are not supported: '{Truncate(text)}'. Split into separate rules."));
            return null;
        }
        foreach (var comb in new[] { '>', '+', '~' })
        {
            if (text.Contains(comb))
            {
                diagnostics.Add(CssDiagnostic.Warning($"The '{comb}' combinator is not supported in selector '{Truncate(text)}'."));
                return null;
            }
        }

        var tokens = text.Split(new[] { ' ', '\n', '\r', '\t', '\f' }, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length > 1)
        {
            diagnostics.Add(CssDiagnostic.Warning(
                $"Descendant selectors are not supported: '{Truncate(text)}'. Only compound simple selectors (e.g. 'button.primary:hover') are matched."));
            return null;
        }

        Selector? current = null;
        foreach (var part in ParseSimpleSequence(tokens[0]))
        {
            current = current is null ? part : new AndSelector(current, part);
        }
        return current ?? new UniversalSelector();
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
            // Unknown char (e.g. '*'): advance to avoid infinite loop
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

    private static Dictionary<string, object> ParseDeclarations(string text, List<CssDiagnostic> diagnostics)
    {
        var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        var pairs = text.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in pairs)
        {
            var idx = p.IndexOf(':');
            if (idx < 0)
            {
                var junk = p.Trim();
                if (junk.Length > 0)
                    diagnostics.Add(CssDiagnostic.Warning($"Declaration missing ':' ignored: '{Truncate(junk)}'."));
                continue;
            }
            var name = p[..idx].Trim();
            var valueRaw = p[(idx + 1)..].Trim();
            if (name.Length == 0)
            {
                diagnostics.Add(CssDiagnostic.Warning($"Declaration with empty property name ignored: '{Truncate(p.Trim())}'."));
                continue;
            }
            if (valueRaw.Length == 0)
            {
                diagnostics.Add(CssDiagnostic.Warning($"Declaration '{name}' has an empty value and was ignored."));
                continue;
            }

            bool isCustomProperty = name.StartsWith("--", StringComparison.Ordinal);
            if (!isCustomProperty && !KnownProperties.Contains(name))
            {
                diagnostics.Add(CssDiagnostic.Warning($"Unsupported property '{name}' ignored."));
                continue;
            }

            // Validate var() syntax up front so malformed expressions produce a diagnostic
            // instead of silently resolving to a fallback later.
            if (valueRaw.StartsWith("var(", StringComparison.Ordinal))
            {
                if (!ValidateVar(valueRaw, out var varError))
                {
                    diagnostics.Add(CssDiagnostic.Warning($"Malformed var() in '{name}: {Truncate(valueRaw)}': {varError}"));
                    // Still store it; the resolver handles malformed var() defensively.
                }
            }

            // Map padding/margin shorthands to a Thickness sentinel now (1-4 length values).
            if (string.Equals(name, "padding", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "margin", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseThicknessShorthand(valueRaw, out var thickness))
                {
                    dict[name] = thickness;
                }
                else
                {
                    diagnostics.Add(CssDiagnostic.Warning($"Could not parse '{name}: {Truncate(valueRaw)}' as 1-4 lengths; ignored."));
                }
                continue;
            }

            dict[name] = valueRaw;
        }
        return dict;
    }

    private static bool TryParseThicknessShorthand(string valueRaw, out Thickness thickness)
    {
        thickness = default;
        var parts = valueRaw.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        switch (parts.Length)
        {
            case 1:
                if (TryNum(parts[0], out var v))
                {
                    thickness = new Thickness(new Length(v), new Length(v), new Length(v), new Length(v));
                    return true;
                }
                return false;
            case 2:
                if (TryNum(parts[0], out var v1) && TryNum(parts[1], out var v2))
                {
                    // top/bottom = parts[0], left/right = parts[1]
                    thickness = new Thickness(new Length(v2), new Length(v1), new Length(v2), new Length(v1));
                    return true;
                }
                return false;
            case 3:
                if (TryNum(parts[0], out var t) && TryNum(parts[1], out var lr) && TryNum(parts[2], out var b))
                {
                    thickness = new Thickness(new Length(lr), new Length(t), new Length(lr), new Length(b));
                    return true;
                }
                return false;
            default: // 4 or more; use the first 4 as top right bottom left
                if (TryNum(parts[0], out var top) && TryNum(parts[1], out var right) &&
                    TryNum(parts[2], out var bottom) && TryNum(parts[3], out var left))
                {
                    thickness = new Thickness(new Length(left), new Length(top), new Length(right), new Length(bottom));
                    return true;
                }
                return false;
        }
    }

    /// <summary>Validate a well-formed <c>var(--name[, fallback])</c> expression with no trailing content.</summary>
    private static bool ValidateVar(string s, out string error)
    {
        error = string.Empty;
        int open = s.IndexOf('(');
        int close = FindMatchingParen(s, open);
        if (close < 0)
        {
            error = "missing closing ')'";
            return false;
        }
        var trailing = s[(close + 1)..].Trim();
        if (trailing.Length > 0)
        {
            error = $"unexpected trailing content '{Truncate(trailing)}'";
            return false;
        }
        var inner = s[(open + 1)..close].Trim();
        int comma = inner.IndexOf(',');
        var nameStr = (comma >= 0 ? inner[..comma] : inner).Trim();
        if (!nameStr.StartsWith("--", StringComparison.Ordinal) || nameStr.Length <= 2)
        {
            error = "custom-property name must start with '--'";
            return false;
        }
        return true;
    }

    /// <summary>
    /// Parse a unitless or px length token. Percentages are rejected here so callers keep
    /// the raw string for later percentage resolution.
    /// </summary>
    private static bool TryNum(string token, out double value)
    {
        value = 0;
        token = token.Trim();
        if (token.Length == 0) return false;
        if (token.EndsWith("%", StringComparison.Ordinal)) return false; // preserved elsewhere
        if (token.EndsWith("px", StringComparison.OrdinalIgnoreCase))
            token = token[..^2].Trim();
        return double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static Func<EnvironmentContext, bool>? ParseMediaCondition(string prelude, List<CssDiagnostic> diagnostics)
    {
        // Strip the leading @media keyword; the remainder holds feature groups (and possibly
        // a trailing selector in the legacy form, which contains no parentheses).
        string rest = prelude[("@media".Length)..];
        var features = new List<Func<EnvironmentContext, bool>>();
        int i = 0;
        while (true)
        {
            int open = rest.IndexOf('(', i);
            if (open < 0) break;
            int close = FindMatchingParen(rest, open);
            if (close < 0)
            {
                diagnostics.Add(CssDiagnostic.Error($"Unterminated media feature in '{Truncate(prelude)}'."));
                return null;
            }
            var inner = rest[(open + 1)..close].Trim();
            var feature = ParseMediaFeature(inner, prelude, diagnostics);
            if (feature is null) return null; // diagnostic already emitted
            features.Add(feature);
            i = close + 1;
        }

        if (features.Count == 0)
        {
            diagnostics.Add(CssDiagnostic.Warning($"@media query has no recognised feature: '{Truncate(prelude)}'."));
            return null;
        }
        if (features.Count == 1) return features[0];
        return env => features.TrueForAll(f => f(env));
    }

    private static Func<EnvironmentContext, bool>? ParseMediaFeature(string inner, string prelude, List<CssDiagnostic> diagnostics)
    {
        int colon = inner.IndexOf(':');
        string feature = (colon >= 0 ? inner[..colon] : inner).Trim();
        string arg = colon >= 0 ? inner[(colon + 1)..].Trim() : string.Empty;

        switch (feature.ToLowerInvariant())
        {
            case "min-width":
                if (TryNum(arg, out var minW)) return env => env.ViewportWidth >= minW;
                break;
            case "max-width":
                if (TryNum(arg, out var maxW)) return env => env.ViewportWidth <= maxW;
                break;
            case "min-height":
                if (TryNum(arg, out var minH)) return env => env.ViewportHeight >= minH;
                break;
            case "max-height":
                if (TryNum(arg, out var maxH)) return env => env.ViewportHeight <= maxH;
                break;
            case "prefers-reduced-motion":
                if (arg.Length == 0 || arg.Equals("reduce", StringComparison.OrdinalIgnoreCase))
                    return env => env.PrefersReducedMotion;
                if (arg.Equals("no-preference", StringComparison.OrdinalIgnoreCase))
                    return env => !env.PrefersReducedMotion;
                break;
            case "is-terminal":
                return env => env.IsTerminal;
        }

        diagnostics.Add(CssDiagnostic.Warning($"Unsupported media feature '({inner})' in '{Truncate(prelude)}'."));
        return null;
    }

    private static string Truncate(string s, int max = 60)
    {
        s = s.Replace('\n', ' ').Replace('\r', ' ').Trim();
        return s.Length <= max ? s : s[..max] + "…";
    }
}
