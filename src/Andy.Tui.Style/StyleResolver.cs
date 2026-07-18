namespace Andy.Tui.Style;

/// <summary>
/// Resolves styles for a given node by applying the cascade.
/// Ordering is: specificity, then the index of the stylesheet in the supplied
/// sequence (later sheets win), then source order within a sheet. No magic
/// numeric offsets are used, so a later stylesheet always wins an
/// equal-specificity tie regardless of how many rules an earlier sheet holds.
/// </summary>
public sealed class StyleResolver
{
    private sealed record DeclWinner(Specificity Specificity, int SheetIndex, int SourceOrder, object Value);

    public ResolvedStyle Compute(Node node, IEnumerable<Stylesheet> stylesheets, EnvironmentContext? env = null, ResolvedStyle? parent = null)
        => Compute(node, stylesheets, env, parent, diagnostics: null);

    /// <summary>
    /// Computes the resolved style and, when <paramref name="diagnostics"/> is supplied,
    /// records a <see cref="StyleDiagnostic"/> for every recognized property whose value
    /// could not be interpreted.
    /// </summary>
    public ResolvedStyle Compute(Node node, IEnumerable<Stylesheet> stylesheets, EnvironmentContext? env, ResolvedStyle? parent, ICollection<StyleDiagnostic>? diagnostics)
    {
        var winners = new Dictionary<string, DeclWinner>(StringComparer.OrdinalIgnoreCase);

        int sheetIndex = 0;
        foreach (var sheet in stylesheets)
        {
            foreach (var rule in sheet.Rules)
            {
                if (rule.MediaCondition is not null)
                {
                    // A media-gated rule can only be evaluated against an environment.
                    // When no environment is supplied the query is unknowable, so the
                    // rule does not apply. This is the explicit, documented behavior.
                    if (env is null || !rule.MediaCondition(env))
                    {
                        continue;
                    }
                }
                if (!rule.Selector.Matches(node)) continue;
                foreach (var kvp in rule.Declarations)
                {
                    var key = kvp.Key;
                    var candidate = new DeclWinner(rule.Selector.Specificity, sheetIndex, rule.SourceOrder, kvp.Value);
                    if (!winners.TryGetValue(key, out var existing) || Compare(candidate, existing) > 0)
                    {
                        winners[key] = candidate;
                    }
                }
            }
            sheetIndex++;
        }

        var style = ResolvedStyle.Default;

        // Map known properties
        style = style with
        {
            Display = GetEnum(winners, "display", style.Display, diagnostics),
            FlexDirection = GetEnum(winners, "flex-direction", style.FlexDirection, diagnostics),
            FlexWrap = GetEnum(winners, "flex-wrap", style.FlexWrap, diagnostics),
            JustifyContent = GetEnum(winners, "justify-content", style.JustifyContent, diagnostics),
            AlignItems = GetEnum(winners, "align-items", style.AlignItems, diagnostics),
            AlignSelf = GetEnum(winners, "align-self", style.AlignSelf, diagnostics),
            AlignContent = GetEnum(winners, "align-content", style.AlignContent, diagnostics),
            Order = GetInt(winners, "order", style.Order, diagnostics),
            FlexGrow = GetDouble(winners, "flex-grow", style.FlexGrow, diagnostics),
            FlexShrink = GetDouble(winners, "flex-shrink", style.FlexShrink, diagnostics),
            FlexBasis = GetLengthOrAuto(winners, "flex-basis", style.FlexBasis, diagnostics),
            Width = GetLengthOrAuto(winners, "width", style.Width, diagnostics),
            Height = GetLengthOrAuto(winners, "height", style.Height, diagnostics),
            MinWidth = GetLengthOrAuto(winners, "min-width", style.MinWidth, diagnostics),
            MinHeight = GetLengthOrAuto(winners, "min-height", style.MinHeight, diagnostics),
            MaxWidth = GetLengthOrAuto(winners, "max-width", style.MaxWidth, diagnostics),
            MaxHeight = GetLengthOrAuto(winners, "max-height", style.MaxHeight, diagnostics),
            RowGap = GetLength(winners, "row-gap", style.RowGap, diagnostics),
            ColumnGap = GetLength(winners, "column-gap", style.ColumnGap, diagnostics),
            Overflow = GetEnum(winners, "overflow", style.Overflow, diagnostics),
            Color = GetColor(winners, "color", style.Color, diagnostics),
            BackgroundColor = GetColor(winners, "background-color", style.BackgroundColor, diagnostics),
            FontWeight = GetEnum(winners, "font-weight", style.FontWeight, diagnostics),
            FontStyle = GetEnum(winners, "font-style", style.FontStyle, diagnostics),
            TextDecoration = GetEnum(winners, "text-decoration", style.TextDecoration, diagnostics),
        };

        // Padding/Margin shorthands and longhands
        var padding = ResolveThickness(winners, "padding", style.Padding, diagnostics);
        var margin = ResolveThickness(winners, "margin", style.Margin, diagnostics);
        style = style with { Padding = padding, Margin = margin };

        // gap shorthand handling with precedence vs longhands
        // If a shorthand is present, apply to both row-gap and column-gap unless a longhand with higher precedence overrides
        ResolveGapPair(winners, style.RowGap, style.ColumnGap, out var resolvedRowGap, out var resolvedColumnGap, diagnostics);
        style = style with { RowGap = resolvedRowGap, ColumnGap = resolvedColumnGap };

        // Inheritance: apply from parent when not specified by winners
        if (parent is not null)
        {
            // color inherits
            if (!winners.ContainsKey("color"))
            {
                style = style with { Color = parent.Value.Color };
            }
            // Text properties inherit
            if (!winners.ContainsKey("font-weight"))
            {
                style = style with { FontWeight = parent.Value.FontWeight };
            }
            if (!winners.ContainsKey("font-style"))
            {
                style = style with { FontStyle = parent.Value.FontStyle };
            }
            if (!winners.ContainsKey("text-decoration"))
            {
                style = style with { TextDecoration = parent.Value.TextDecoration };
            }
            // background-color does not inherit (by CSS spec) - skip
        }

        // Keywords handling for supported properties: inherit | initial | unset
        // Apply after base mapping so explicit keywords take effect even for non-inherited properties
        if (TryGetKeyword(winners, "color", out var colorKeyword))
        {
            style = style with { Color = ResolveKeyword(colorKeyword, parent?.Color, ResolvedStyle.Default.Color, isInheritable: true) };
        }
        if (TryGetKeyword(winners, "background-color", out var bgKeyword))
        {
            style = style with { BackgroundColor = ResolveKeyword(bgKeyword, parent?.BackgroundColor, ResolvedStyle.Default.BackgroundColor, isInheritable: false) };
        }
        if (TryGetKeyword(winners, "font-weight", out var fwKeyword))
        {
            var v = ResolveKeyword(fwKeyword, parent?.FontWeight, ResolvedStyle.Default.FontWeight, isInheritable: true);
            style = style with { FontWeight = v };
        }
        if (TryGetKeyword(winners, "font-style", out var fsKeyword))
        {
            var v = ResolveKeyword(fsKeyword, parent?.FontStyle, ResolvedStyle.Default.FontStyle, isInheritable: true);
            style = style with { FontStyle = v };
        }
        if (TryGetKeyword(winners, "text-decoration", out var tdKeyword))
        {
            var v = ResolveKeyword(tdKeyword, parent?.TextDecoration, ResolvedStyle.Default.TextDecoration, isInheritable: true);
            style = style with { TextDecoration = v };
        }

        return style;
    }

    private static int Compare(DeclWinner a, DeclWinner b)
    {
        var s = a.Specificity.CompareTo(b.Specificity);
        if (s != 0) return s;
        // Later stylesheet wins regardless of intra-sheet rule counts.
        var sheet = a.SheetIndex.CompareTo(b.SheetIndex);
        if (sheet != 0) return sheet;
        return a.SourceOrder.CompareTo(b.SourceOrder);
    }

    private static object ResolveVars(object value, IDictionary<string, DeclWinner> winners, int depth = 0, HashSet<string>? active = null)
    {
        if (value is not string original) return value;
        var s = original.Trim();
        if (!s.StartsWith("var(", StringComparison.Ordinal)) return value;
        if (depth > 16) return value; // hard guard against pathological nesting

        // Locate the matching close paren for the opening '(' at index 3. Malformed input
        // (missing ')' or trailing content) is left as-is instead of slicing out of range.
        int open = 3;
        int close = FindMatchingParen(s, open);
        if (close < 0) return value; // unterminated var()
        var trailing = s[(close + 1)..].Trim();
        if (trailing.Length > 0) return value; // unsupported trailing content

        var inner = s[(open + 1)..close];
        int comma = inner.IndexOf(',');
        string name;
        string? fallback = null;
        if (comma >= 0)
        {
            name = inner[..comma].Trim();
            fallback = inner[(comma + 1)..].Trim();
        }
        else
        {
            name = inner.Trim();
        }

        if (!name.StartsWith("--", StringComparison.Ordinal)) return value; // invalid custom-property name

        // Follow the reference unless doing so would revisit a name already on the current
        // resolution chain (a self/mutual reference cycle). On a cycle we skip the reference
        // and fall through to the declared fallback, matching CSS custom-property semantics.
        bool cyclic = active is not null && active.Contains(name);
        if (!cyclic && winners.TryGetValue(name, out var w))
        {
            active ??= new HashSet<string>(StringComparer.Ordinal);
            active.Add(name);
            var resolved = ResolveVars(w.Value, winners, depth + 1, active);
            active.Remove(name);

            // A concrete value wins. If the reference resolved back to an unresolvable var()
            // token (e.g. a cycle deeper in the chain), fall through to the fallback below.
            if (resolved is not string rs || !rs.TrimStart().StartsWith("var(", StringComparison.Ordinal))
                return resolved;
        }

        if (!string.IsNullOrEmpty(fallback))
        {
            return ResolveVars(fallback, winners, depth + 1, active);
        }
        return value;
    }

    private static int FindMatchingParen(string s, int openIndex)
    {
        int level = 0;
        for (int i = openIndex; i < s.Length; i++)
        {
            if (s[i] == '(') level++;
            else if (s[i] == ')')
            {
                level--;
                if (level == 0) return i;
            }
        }
        return -1;
    }

    private static void Report(ICollection<StyleDiagnostic>? diagnostics, string property, object raw, string message)
    {
        if (diagnostics is null) return;
        diagnostics.Add(new StyleDiagnostic(property, raw?.ToString() ?? string.Empty, message));
    }

    // CSS-wide keywords (inherit | initial | unset) are valid on every property.
    // They are resolved through the dedicated keyword paths where supported and
    // must never be diagnosed as invalid values on length/number/int properties.
    private static bool IsCssWideKeyword(object raw)
        => raw is string s && s.Trim() is "inherit" or "initial" or "unset";

    private static TEnum GetEnum<TEnum>(IDictionary<string, DeclWinner> winners, string key, TEnum fallback, ICollection<StyleDiagnostic>? diagnostics) where TEnum : struct
    {
        if (!winners.TryGetValue(key, out var w)) return fallback;
        var raw = ResolveVars(w.Value, winners);
        if (raw is TEnum t) return t;
        if (raw is string s)
        {
            // Global CSS-wide keywords are handled elsewhere; do not diagnose them here.
            var lowered = s.Trim();
            if (lowered is "inherit" or "initial" or "unset") return fallback;

            // Custom mappings for CSS-friendly tokens
            if (typeof(TEnum) == typeof(TextDecoration))
            {
                if (string.Equals(s, "underline", StringComparison.OrdinalIgnoreCase)) return (TEnum)(object)TextDecoration.Underline;
                if (string.Equals(s, "line-through", StringComparison.OrdinalIgnoreCase)) return (TEnum)(object)TextDecoration.Strikethrough;
                if (string.Equals(s, "none", StringComparison.OrdinalIgnoreCase)) return (TEnum)(object)TextDecoration.None;
            }
            if (typeof(TEnum) == typeof(FontWeight))
            {
                if (int.TryParse(s, out var fwNum)) return (TEnum)(object)(fwNum >= 600 ? FontWeight.Bold : FontWeight.Normal);
            }
            // Accept both direct and kebab-case CSS keywords. CSS enum tokens are
            // hyphenated (flex-start, space-between, wrap-reverse); the corresponding
            // enum members are PascalCase without separators. Require a defined member
            // so numeric or out-of-range parses are rejected.
            if (Enum.TryParse<TEnum>(lowered, ignoreCase: true, out var parsed) && Enum.IsDefined(typeof(TEnum), parsed))
            {
                return parsed;
            }
            var collapsed = lowered.Replace("-", string.Empty);
            if (!string.Equals(collapsed, lowered, StringComparison.Ordinal) &&
                Enum.TryParse<TEnum>(collapsed, ignoreCase: true, out var parsedCollapsed) &&
                Enum.IsDefined(typeof(TEnum), parsedCollapsed))
            {
                return parsedCollapsed;
            }
            Report(diagnostics, key, s, $"unrecognized value for {typeof(TEnum).Name}");
            return fallback;
        }
        if (typeof(TEnum) == typeof(FontWeight))
        {
            if (raw is int i) return (TEnum)(object)(i >= 600 ? FontWeight.Bold : FontWeight.Normal);
            if (raw is double d) return (TEnum)(object)((d >= 600) ? FontWeight.Bold : FontWeight.Normal);
        }
        Report(diagnostics, key, raw, $"unrecognized value for {typeof(TEnum).Name}");
        return fallback;
    }

    private static int GetInt(IDictionary<string, DeclWinner> winners, string key, int fallback, ICollection<StyleDiagnostic>? diagnostics)
    {
        if (!winners.TryGetValue(key, out var w)) return fallback;
        var raw = ResolveVars(w.Value, winners);
        if (IsCssWideKeyword(raw)) return fallback;
        switch (raw)
        {
            case int i: return i;
            case double d: return (int)d;
            case string s when int.TryParse(s, out var i2): return i2;
            default:
                Report(diagnostics, key, raw, "expected an integer");
                return fallback;
        }
    }

    private static double GetDouble(IDictionary<string, DeclWinner> winners, string key, double fallback, ICollection<StyleDiagnostic>? diagnostics)
    {
        if (!winners.TryGetValue(key, out var w)) return fallback;
        var raw = ResolveVars(w.Value, winners);
        if (IsCssWideKeyword(raw)) return fallback;
        switch (raw)
        {
            case double d: return d;
            case int i: return i;
            case float f: return f;
            case string s when double.TryParse(s, out var d2): return d2;
            default:
                Report(diagnostics, key, raw, "expected a number");
                return fallback;
        }
    }

    private static Length GetLength(IDictionary<string, DeclWinner> winners, string key, Length fallback, ICollection<StyleDiagnostic>? diagnostics)
    {
        if (!winners.TryGetValue(key, out var w)) return fallback;
        var raw = ResolveVars(w.Value, winners);
        if (IsCssWideKeyword(raw)) return fallback;
        switch (raw)
        {
            case Length l: return l;
            case int i: return new Length(i);
            case double d: return new Length(d);
            case string s when s.EndsWith("%", StringComparison.Ordinal) && double.TryParse(s.TrimEnd('%'), out var pct): return Length.FromPercent(pct);
            case string s when double.TryParse(TrimPx(s), out var d2): return new Length(d2);
            default:
                Report(diagnostics, key, raw, "expected a length");
                return fallback;
        }
    }

    private static LengthOrAuto GetLengthOrAuto(IDictionary<string, DeclWinner> winners, string key, LengthOrAuto fallback, ICollection<StyleDiagnostic>? diagnostics)
    {
        if (!winners.TryGetValue(key, out var w)) return fallback;
        var raw = ResolveVars(w.Value, winners);
        if (IsCssWideKeyword(raw)) return fallback;
        switch (raw)
        {
            case string s when string.Equals(s, "auto", StringComparison.OrdinalIgnoreCase):
                return LengthOrAuto.Auto();
            // "none" removes the constraint, but it is only valid on max-width/max-height.
            // On any other length property it is invalid CSS and must be diagnosed.
            case string s when string.Equals(s, "none", StringComparison.OrdinalIgnoreCase):
                if (string.Equals(key, "max-width", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(key, "max-height", StringComparison.OrdinalIgnoreCase))
                {
                    return LengthOrAuto.Auto();
                }
                Report(diagnostics, key, s, $"'none' is not valid for {key}; only max-width/max-height accept it");
                return fallback;
            case string s when s.EndsWith("%", StringComparison.Ordinal) && double.TryParse(s.TrimEnd('%'), out var pct):
                return LengthOrAuto.FromPercent(pct);
            case string s when double.TryParse(TrimPx(s), out var d2):
                return LengthOrAuto.FromPixels(d2);
            case Length l:
                return new LengthOrAuto(l);
            case int i:
                return new LengthOrAuto(new Length(i));
            case double d:
                return new LengthOrAuto(new Length(d));
            default:
                Report(diagnostics, key, raw, "expected a length, percentage, auto, or none");
                return fallback;
        }
    }

    private static string TrimPx(string s)
    {
        s = s.Trim();
        return s.EndsWith("px", StringComparison.OrdinalIgnoreCase) ? s[..^2].Trim() : s;
    }

    private static RgbaColor GetColor(IDictionary<string, DeclWinner> winners, string key, RgbaColor fallback, ICollection<StyleDiagnostic>? diagnostics)
    {
        if (!winners.TryGetValue(key, out var w)) return fallback;
        var raw = ResolveVars(w.Value, winners);
        if (raw is RgbaColor c) return c;
        if (raw is string s)
        {
            var lowered = s.Trim();
            if (lowered is "inherit" or "initial" or "unset") return fallback;
            if (ColorParser.TryParse(s, out var parsed)) return parsed;
            Report(diagnostics, key, s, "unrecognized color");
            return fallback;
        }
        Report(diagnostics, key, raw, "unrecognized color");
        return fallback;
    }

    private static Thickness ResolveThickness(IDictionary<string, DeclWinner> winners, string baseKey, Thickness fallback, ICollection<StyleDiagnostic>? diagnostics)
    {
        // Consider both shorthand and longhands; precedence: higher specificity then later source order.
        DeclWinner? shorthandWinner = null;
        Thickness shorthandValue = default;
        if (winners.TryGetValue(baseKey, out var sh) && sh.Value is Thickness t)
        {
            shorthandWinner = sh;
            shorthandValue = t;
        }

        Length SelectEdge(string edgeKey, Func<Thickness, Length> selectFromThickness, Length edgeFallback)
        {
            winners.TryGetValue(edgeKey, out var lh);
            if (lh is not null)
            {
                if (shorthandWinner is not null)
                {
                    return Compare(lh, shorthandWinner) >= 0
                        ? GetLength(winners, edgeKey, edgeFallback, diagnostics)
                        : selectFromThickness(shorthandValue);
                }
                return GetLength(winners, edgeKey, edgeFallback, diagnostics);
            }
            return shorthandWinner is not null ? selectFromThickness(shorthandValue) : edgeFallback;
        }

        var left = SelectEdge(baseKey + "-left", th => th.Left, fallback.Left);
        var top = SelectEdge(baseKey + "-top", th => th.Top, fallback.Top);
        var right = SelectEdge(baseKey + "-right", th => th.Right, fallback.Right);
        var bottom = SelectEdge(baseKey + "-bottom", th => th.Bottom, fallback.Bottom);
        return new Thickness(left, top, right, bottom);
    }

    private static void ResolveGapPair(IDictionary<string, DeclWinner> winners, Length rowGapFallback, Length columnGapFallback, out Length rowGap, out Length columnGap, ICollection<StyleDiagnostic>? diagnostics)
    {
        DeclWinner? shorthandWinner = null;
        Length shorthandValue = rowGapFallback; // default
        if (winners.TryGetValue("gap", out var sh))
        {
            var raw = ResolveVars(sh.Value, winners);
            var len = raw switch { Length l => l, int i => new Length(i), double d => new Length(d), string s when s.EndsWith("%", StringComparison.Ordinal) && double.TryParse(s.TrimEnd('%'), out var pct) => Length.FromPercent(pct), string s when double.TryParse(TrimPx(s), out var d2) => new Length(d2), _ => (Length?)null };
            if (len is not null)
            {
                shorthandWinner = sh;
                shorthandValue = len.Value;
            }
        }

        Length SelectGap(string key, Length fallback)
        {
            winners.TryGetValue(key, out var lh);
            if (lh is not null)
            {
                if (shorthandWinner is not null)
                {
                    return Compare(lh, shorthandWinner) >= 0 ? GetLength(winners, key, fallback, diagnostics) : shorthandValue;
                }
                return GetLength(winners, key, fallback, diagnostics);
            }
            return shorthandWinner is not null ? shorthandValue : fallback;
        }

        rowGap = SelectGap("row-gap", rowGapFallback);
        columnGap = SelectGap("column-gap", columnGapFallback);
    }

    private static bool TryGetKeyword(IDictionary<string, DeclWinner> winners, string key, out string keyword)
    {
        keyword = string.Empty;
        if (!winners.TryGetValue(key, out var w)) return false;
        var raw = ResolveVars(w.Value, winners);
        if (raw is string s)
        {
            s = s.Trim().ToLowerInvariant();
            if (s is "inherit" or "initial" or "unset") { keyword = s; return true; }
        }
        return false;
    }

    private static T ResolveKeyword<T>(string keyword, T? parentValue, T initialValue, bool isInheritable) where T : struct
    {
        return keyword switch
        {
            "inherit" => parentValue ?? initialValue,
            "initial" => initialValue,
            "unset" => isInheritable ? (parentValue ?? initialValue) : initialValue,
            _ => initialValue
        };
    }
}
