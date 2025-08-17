using System.Globalization;

namespace Andy.Tui.Text;

internal static class WcWidth
{
    public static int GetCharWidth(int codePoint)
    {
        // Control characters and non-spacing marks: width 0
        if (codePoint == 0) return 0;
        if (codePoint < 32 || (codePoint >= 0x7f && codePoint < 0xa0)) return 0;
        var category = CharUnicodeInfo.GetUnicodeCategory(char.ConvertFromUtf32(codePoint), 0);
        if (category == UnicodeCategory.NonSpacingMark || category == UnicodeCategory.EnclosingMark || category == UnicodeCategory.SpacingCombiningMark)
        {
            return 0;
        }

        // Common wide ranges (East Asian Wide/Fullwidth + emojis)
        if (IsWide(codePoint)) return 2;
        return 1;
    }

    public static bool IsWide(int codePoint)
    {
        // Approximation of wide/emoji ranges
        return
            (codePoint >= 0x1100 && codePoint <= 0x115F) || // Hangul Jamo init. consonants
            (codePoint == 0x2329 || codePoint == 0x232A) ||
            (codePoint >= 0x2E80 && codePoint <= 0xA4CF) || // CJK ... Yi
            (codePoint >= 0xAC00 && codePoint <= 0xD7A3) || // Hangul Syllables
            (codePoint >= 0xF900 && codePoint <= 0xFAFF) || // CJK compatibility Ideographs
            (codePoint >= 0xFE10 && codePoint <= 0xFE19) ||
            (codePoint >= 0xFE30 && codePoint <= 0xFE6F) ||
            (codePoint >= 0xFF00 && codePoint <= 0xFF60) || // Fullwidth Forms
            (codePoint >= 0xFFE0 && codePoint <= 0xFFE6) ||
            (codePoint >= 0x1F300 && codePoint <= 0x1F64F) || // Emojis
            (codePoint >= 0x1F900 && codePoint <= 0x1F9FF) ||
            (codePoint >= 0x20000 && codePoint <= 0x3FFFD);   // CJK Extensions
    }
}
