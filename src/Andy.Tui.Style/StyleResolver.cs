namespace Andy.Tui.Style;

/// <summary>
/// Resolves styles for a given node by applying cascade: specificity > source order.
/// </summary>
public sealed class StyleResolver
{
    private sealed record DeclWinner(Specificity Specificity, int SourceOrder, object Value);

    public ResolvedStyle Compute(Node node, IEnumerable<Stylesheet> stylesheets, EnvironmentContext? env = null, ResolvedStyle? parent = null)
    {
        var winners = new Dictionary<string, DeclWinner>(StringComparer.OrdinalIgnoreCase);

        int order = 0;
        foreach (var sheet in stylesheets)
        {
            foreach (var rule in sheet.Rules)
            {
                if (rule.MediaCondition is not null && env is not null && !rule.MediaCondition(env))
                {
                    continue;
                }
                if (!rule.Selector.Matches(node)) continue;
                foreach (var kvp in rule.Declarations)
                {
                    var key = kvp.Key;
                    var candidate = new DeclWinner(rule.Selector.Specificity, rule.SourceOrder + order, kvp.Value);
                    if (!winners.TryGetValue(key, out var existing) || Compare(candidate, existing) > 0)
                    {
                        winners[key] = candidate;
                    }
                }
            }
            order += 10_000; // ensure later stylesheets have higher base order
        }

        var style = ResolvedStyle.Default;

        // Map known properties
        style = style with
        {
            Display = GetEnum(winners, "display", style.Display),
            FlexDirection = GetEnum(winners, "flex-direction", style.FlexDirection),
            FlexWrap = GetEnum(winners, "flex-wrap", style.FlexWrap),
            JustifyContent = GetEnum(winners, "justify-content", style.JustifyContent),
            AlignItems = GetEnum(winners, "align-items", style.AlignItems),
            AlignContent = GetEnum(winners, "align-content", style.AlignContent),
            Order = GetInt(winners, "order", style.Order),
            FlexGrow = GetDouble(winners, "flex-grow", style.FlexGrow),
            FlexShrink = GetDouble(winners, "flex-shrink", style.FlexShrink),
            FlexBasis = GetLengthOrAuto(winners, "flex-basis", style.FlexBasis),
            Width = GetLengthOrAuto(winners, "width", style.Width),
            Height = GetLengthOrAuto(winners, "height", style.Height),
            RowGap = GetLength(winners, "row-gap", style.RowGap),
            ColumnGap = GetLength(winners, "column-gap", style.ColumnGap),
            Overflow = GetEnum(winners, "overflow", style.Overflow),
            Color = GetColor(winners, "color", style.Color),
            BackgroundColor = GetColor(winners, "background-color", style.BackgroundColor),
            FontWeight = GetEnum(winners, "font-weight", style.FontWeight),
            FontStyle = GetEnum(winners, "font-style", style.FontStyle),
            TextDecoration = GetEnum(winners, "text-decoration", style.TextDecoration),
        };

        // Padding/Margin shorthands and longhands
        var padding = ResolveThickness(winners, "padding", style.Padding);
        var margin = ResolveThickness(winners, "margin", style.Margin);
        style = style with { Padding = padding, Margin = margin };

        // gap shorthand handling with precedence vs longhands
        // If a shorthand is present, apply to both row-gap and column-gap unless a longhand with higher precedence overrides
        ResolveGapPair(winners, style.RowGap, style.ColumnGap, out var resolvedRowGap, out var resolvedColumnGap);
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
        return a.SourceOrder.CompareTo(b.SourceOrder);
    }

    private static object ResolveVars(object value, IDictionary<string, DeclWinner> winners, int depth = 0)
    {
        if (value is not string s) return value;
        if (!s.StartsWith("var(", StringComparison.Ordinal)) return value;
        if (depth > 8) return value; // prevent cycles

        // very small parser for var(--name, fallback)
        var inner = s.AsSpan(4, s.Length - 5).Trim(); // strip var( )
        int comma = inner.IndexOf(',');
        ReadOnlySpan<char> nameSpan;
        ReadOnlySpan<char> fallbackSpan = default;
        if (comma >= 0)
        {
            nameSpan = inner.Slice(0, comma).Trim();
            fallbackSpan = inner.Slice(comma + 1).Trim();
        }
        else
        {
            nameSpan = inner.Trim();
        }

        var name = nameSpan.ToString();
        if (winners.TryGetValue(name, out var w))
        {
            return ResolveVars(w.Value, winners, depth + 1);
        }

        if (!fallbackSpan.IsEmpty)
        {
            return ResolveVars(fallbackSpan.ToString(), winners, depth + 1);
        }
        return value;
    }

    private static TEnum GetEnum<TEnum>(IDictionary<string, DeclWinner> winners, string key, TEnum fallback) where TEnum : struct
    {
        if (!winners.TryGetValue(key, out var w)) return fallback;
        var raw = ResolveVars(w.Value, winners);
        if (raw is TEnum t) return t;
        if (raw is string s)
        {
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
            if (Enum.TryParse<TEnum>(s, ignoreCase: true, out var parsed)) return parsed;
        }
        if (typeof(TEnum) == typeof(FontWeight))
        {
            if (raw is int i) return (TEnum)(object)(i >= 600 ? FontWeight.Bold : FontWeight.Normal);
            if (raw is double d) return (TEnum)(object)((d >= 600) ? FontWeight.Bold : FontWeight.Normal);
        }
        return fallback;
    }

    private static int GetInt(IDictionary<string, DeclWinner> winners, string key, int fallback)
    {
        if (!winners.TryGetValue(key, out var w)) return fallback;
        var raw = ResolveVars(w.Value, winners);
        return raw switch { int i => i, double d => (int)d, string s when int.TryParse(s, out var i2) => i2, _ => fallback };
    }

    private static double GetDouble(IDictionary<string, DeclWinner> winners, string key, double fallback)
    {
        if (!winners.TryGetValue(key, out var w)) return fallback;
        var raw = ResolveVars(w.Value, winners);
        return raw switch { double d => d, int i => i, float f => f, string s when double.TryParse(s, out var d2) => d2, _ => fallback };
    }

    private static Length GetLength(IDictionary<string, DeclWinner> winners, string key, Length fallback)
    {
        if (!winners.TryGetValue(key, out var w)) return fallback;
        var raw = ResolveVars(w.Value, winners);
        return raw switch { Length l => l, int i => new Length(i), double d => new Length(d), string s when double.TryParse(s, out var d2) => new Length(d2), _ => fallback };
    }

    private static LengthOrAuto GetLengthOrAuto(IDictionary<string, DeclWinner> winners, string key, LengthOrAuto fallback)
    {
        if (!winners.TryGetValue(key, out var w)) return fallback;
        var raw = ResolveVars(w.Value, winners);
        return raw switch
        {
            string s when string.Equals(s, "auto", StringComparison.OrdinalIgnoreCase) => LengthOrAuto.Auto(),
            string s when s.EndsWith("%", StringComparison.Ordinal) && double.TryParse(s.TrimEnd('%'), out var pct) => LengthOrAuto.FromPercent(pct),
            Length l => new LengthOrAuto(l),
            int i => new LengthOrAuto(new Length(i)),
            double d => new LengthOrAuto(new Length(d)),
            _ => fallback
        };
    }

    private static RgbaColor GetColor(IDictionary<string, DeclWinner> winners, string key, RgbaColor fallback)
    {
        if (!winners.TryGetValue(key, out var w)) return fallback;
        var raw = ResolveVars(w.Value, winners);
        if (raw is RgbaColor c) return c;
        if (raw is string s)
        {
            if (ColorParser.TryParse(s, out var parsed)) return parsed;
        }
        return fallback;
    }

    private static Thickness ResolveThickness(IDictionary<string, DeclWinner> winners, string baseKey, Thickness fallback)
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
                        ? GetLength(winners, edgeKey, edgeFallback)
                        : selectFromThickness(shorthandValue);
                }
                return GetLength(winners, edgeKey, edgeFallback);
            }
            return shorthandWinner is not null ? selectFromThickness(shorthandValue) : edgeFallback;
        }

        var left = SelectEdge(baseKey + "-left", th => th.Left, fallback.Left);
        var top = SelectEdge(baseKey + "-top", th => th.Top, fallback.Top);
        var right = SelectEdge(baseKey + "-right", th => th.Right, fallback.Right);
        var bottom = SelectEdge(baseKey + "-bottom", th => th.Bottom, fallback.Bottom);
        return new Thickness(left, top, right, bottom);
    }

    private static void ResolveGapPair(IDictionary<string, DeclWinner> winners, Length rowGapFallback, Length columnGapFallback, out Length rowGap, out Length columnGap)
    {
        DeclWinner? shorthandWinner = null;
        Length shorthandValue = rowGapFallback; // default
        if (winners.TryGetValue("gap", out var sh))
        {
            var raw = ResolveVars(sh.Value, winners);
            var len = raw switch { Length l => l, int i => new Length(i), double d => new Length(d), string s when double.TryParse(s, out var d2) => new Length(d2), _ => (Length?)null };
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
                    return Compare(lh, shorthandWinner) >= 0 ? GetLength(winners, key, fallback) : shorthandValue;
                }
                return GetLength(winners, key, fallback);
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