using System.Globalization;

namespace Andy.Tui.Text;

/// <summary>
/// The single shared source of truth for grapheme-cluster segmentation and
/// terminal-cell width across the whole rendering pipeline (measurement,
/// wrapping, clipping, compositor cells, row runs, widgets, and cursor
/// calculations). Every stage MUST route through this type so that the width a
/// string is measured at, the number of columns it is laid out into, the cells
/// the compositor paints, and the bytes the encoder emits all agree.
///
/// Width policy:
/// <list type="bullet">
///   <item>Control characters (C0/C1) and combining/enclosing/spacing marks,
///   zero-width joiners, and variation selectors contribute 0 columns.</item>
///   <item>East Asian Wide/Fullwidth characters, emoji, and regional-indicator
///   symbols (flags) contribute 2 columns.</item>
///   <item>A grapheme that carries an emoji presentation selector (U+FE0F) or a
///   zero-width joiner (U+200D) is rendered in emoji presentation and therefore
///   occupies 2 columns even when its base scalar would be narrow (keycaps,
///   ZWJ families, VS16 sequences).</item>
///   <item>East Asian Ambiguous characters default to 1 column (narrow), the
///   conventional terminal default. This is a deliberate, tested policy.</item>
///   <item>All other characters contribute 1 column.</item>
/// </list>
/// </summary>
public static class TerminalText
{
    private const int ZeroWidthJoiner = 0x200D;
    private const int ZeroWidthNonJoiner = 0x200C;
    private const int EmojiVariationSelector = 0xFE0F; // VS16

    /// <summary>
    /// Enumerates the Unicode grapheme clusters (user-perceived characters) of
    /// <paramref name="text"/>. Combining marks, surrogate pairs, ZWJ sequences,
    /// variation selectors, keycaps, flags, and skin-tone modifiers each stay in
    /// a single cluster.
    /// </summary>
    public static IEnumerable<string> EnumerateGraphemes(string? text)
        => new GraphemeEnumerator(text ?? string.Empty);

    /// <summary>Enumerates the Unicode scalar values of <paramref name="s"/>.</summary>
    public static IEnumerable<int> EnumerateScalars(string? s)
    {
        s ??= string.Empty;
        for (int i = 0; i < s.Length; i++)
        {
            if (char.IsHighSurrogate(s[i]) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]))
            {
                yield return char.ConvertToUtf32(s[i], s[i + 1]);
                i++;
            }
            else
            {
                yield return s[i];
            }
        }
    }

    /// <summary>
    /// True when a single scalar occupies no columns: C0/C1 controls, combining
    /// or enclosing or spacing marks, zero-width (non-)joiners, and variation
    /// selectors.
    /// </summary>
    public static bool IsZeroWidth(int codePoint)
    {
        if (codePoint == 0) return true;
        if (codePoint < 32 || (codePoint >= 0x7f && codePoint < 0xa0)) return true;
        if (codePoint == ZeroWidthJoiner || codePoint == ZeroWidthNonJoiner) return true;
        if ((codePoint >= 0xFE00 && codePoint <= 0xFE0F) ||          // variation selectors
            (codePoint >= 0x0300 && codePoint <= 0x036F) ||          // combining diacriticals
            (codePoint >= 0x1AB0 && codePoint <= 0x1AFF) ||
            (codePoint >= 0x1DC0 && codePoint <= 0x1DFF) ||
            (codePoint >= 0x20D0 && codePoint <= 0x20FF) ||          // combining marks for symbols
            (codePoint >= 0xFE20 && codePoint <= 0xFE2F) ||
            (codePoint >= 0xE0100 && codePoint <= 0xE01EF))          // variation selectors supplement
        {
            return true;
        }

        var category = CharUnicodeInfo.GetUnicodeCategory(char.ConvertFromUtf32(codePoint), 0);
        return category is UnicodeCategory.NonSpacingMark
            or UnicodeCategory.EnclosingMark
            or UnicodeCategory.SpacingCombiningMark;
    }

    /// <summary>
    /// True for East Asian Wide/Fullwidth characters, emoji, and regional
    /// indicator (flag) symbols. This is the authoritative wide table shared by
    /// measurement and the compositor.
    /// </summary>
    public static bool IsWide(int codePoint)
    {
        return
            (codePoint >= 0x1100 && codePoint <= 0x115F) ||           // Hangul Jamo
            (codePoint == 0x2329 || codePoint == 0x232A) ||
            (codePoint >= 0x2E80 && codePoint <= 0xA4CF) ||           // CJK … Yi
            (codePoint >= 0xAC00 && codePoint <= 0xD7A3) ||           // Hangul syllables
            (codePoint >= 0xF900 && codePoint <= 0xFAFF) ||           // CJK compat ideographs
            (codePoint >= 0xFE10 && codePoint <= 0xFE19) ||
            (codePoint >= 0xFE30 && codePoint <= 0xFE6F) ||
            (codePoint >= 0xFF00 && codePoint <= 0xFF60) ||           // fullwidth forms
            (codePoint >= 0xFFE0 && codePoint <= 0xFFE6) ||
            (codePoint >= 0x1F1E6 && codePoint <= 0x1F1FF) ||         // regional indicators (flags)
            (codePoint >= 0x1F300 && codePoint <= 0x1F64F) ||         // misc symbols & pictographs, emoticons
            (codePoint >= 0x1F680 && codePoint <= 0x1F6FF) ||         // transport & map symbols
            (codePoint >= 0x1F900 && codePoint <= 0x1F9FF) ||         // supplemental symbols & pictographs
            (codePoint >= 0x1FA00 && codePoint <= 0x1FAFF) ||         // symbols & pictographs extended-A
            (codePoint >= 0x20000 && codePoint <= 0x3FFFD);           // CJK extensions
    }

    /// <summary>
    /// East Asian Ambiguous scalars. Policy: these are treated as narrow (1
    /// column). Exposed so callers and tests can reason about the policy.
    /// </summary>
    public static bool IsAmbiguous(int codePoint)
    {
        return
            (codePoint == 0x00A1) ||
            (codePoint == 0x00A4) ||
            (codePoint >= 0x00A7 && codePoint <= 0x00A8) ||
            (codePoint == 0x00AA) ||
            (codePoint >= 0x00B0 && codePoint <= 0x00B4) ||
            (codePoint >= 0x2100 && codePoint <= 0x214F) ||           // letterlike symbols
            (codePoint >= 0x2190 && codePoint <= 0x21FF) ||           // arrows
            (codePoint >= 0x2200 && codePoint <= 0x22FF) ||           // math operators
            (codePoint >= 0x2460 && codePoint <= 0x24FF) ||           // enclosed alphanumerics
            (codePoint >= 0x25A0 && codePoint <= 0x25FF) ||           // geometric shapes
            (codePoint >= 0x2600 && codePoint <= 0x26FF);             // misc symbols
    }

    /// <summary>Column width of a single Unicode scalar (0, 1, or 2).</summary>
    public static int ScalarCellWidth(int codePoint)
    {
        if (IsZeroWidth(codePoint)) return 0;
        if (IsWide(codePoint)) return 2;
        return 1; // ambiguous defaults to narrow per policy
    }

    /// <summary>
    /// Column width of an entire grapheme cluster. A cluster is at most 2 columns
    /// wide. Emoji presentation clusters (containing VS16 or ZWJ) are 2 columns.
    /// </summary>
    public static int GraphemeCellWidth(string? grapheme)
    {
        if (string.IsNullOrEmpty(grapheme)) return 0;

        bool hasBase = false;
        bool wide = false;
        bool emojiPresentation = false;
        foreach (var cp in EnumerateScalars(grapheme))
        {
            if (cp == ZeroWidthJoiner || cp == EmojiVariationSelector) emojiPresentation = true;
            int w = ScalarCellWidth(cp);
            if (w == 2) wide = true;
            else if (w == 1) hasBase = true;
        }

        if (wide) return 2;
        if (emojiPresentation && hasBase) return 2;
        if (hasBase) return 1;
        return 0;
    }

    /// <summary>Total column width of <paramref name="text"/> across all its grapheme clusters.</summary>
    public static int MeasureWidth(string? text)
    {
        text ??= string.Empty;
        int width = 0;
        foreach (var g in EnumerateGraphemes(text))
        {
            width += GraphemeCellWidth(g);
        }
        return width;
    }
}
