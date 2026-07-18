using System.Text;

namespace Andy.Tui.DisplayList;

/// <summary>
/// The trust boundary for application-supplied text.
///
/// <para>
/// <see cref="TextRun.Content"/> and any string that ultimately reaches the
/// terminal must be <b>plain display text</b>: it must never carry terminal
/// control characters. The ANSI encoder writes run text verbatim to the
/// terminal, so a control byte embedded in ordinary content — a log line,
/// Markdown, a chat message, a filename, or a network payload — would be
/// interpreted as a terminal command. Adversarial text could otherwise move the
/// cursor, clear the screen, ring the bell (BEL), open an OSC 8 hyperlink,
/// write the system clipboard via OSC 52, or set the window title. The
/// compositor also stores every code point as a visible cell, so clipping or
/// scrolling could cut a multi-byte escape sequence before its terminator and
/// corrupt the stream.
/// </para>
///
/// <para>
/// Trusted terminal control — cursor positioning, SGR colors and attributes — is
/// represented as <i>typed operations</i>: it flows through <see cref="TextRun"/>
/// coordinates and <see cref="TextRun.Fg"/>/<see cref="TextRun.Bg"/>/<see cref="TextRun.Attrs"/>
/// and is emitted by the encoder itself, never smuggled through text. This class
/// rewrites control characters found in ordinary text into visible, inert,
/// single-cell placeholders so untrusted content cannot escape the text plane.
/// </para>
/// </summary>
public static class TerminalText
{
    /// <summary>
    /// True when <paramref name="codePoint"/> is a terminal control character that
    /// must not appear in plain display text: a C0 control (U+0000..U+001F,
    /// including ESC, BEL, CR, and LF), DEL (U+007F), or a C1 control
    /// (U+0080..U+009F, which some terminals treat as CSI/OSC introducers).
    /// </summary>
    public static bool IsControl(int codePoint)
        => (codePoint >= 0x00 && codePoint <= 0x1F)
        || codePoint == 0x7F
        || (codePoint >= 0x80 && codePoint <= 0x9F);

    /// <summary>
    /// Returns a visible, inert, single-cell replacement for a control code point,
    /// or <c>null</c> when the code point is not a control character. C0 controls
    /// map to the Unicode Control Pictures block (U+2400..U+241F, e.g. ESC becomes
    /// "␛"), DEL maps to U+2421 ("␡"), and C1 controls map to the replacement
    /// character U+FFFD ("�"). None of the replacements contain a control byte, so
    /// they can never re-introduce an escape sequence.
    /// </summary>
    public static string? ReplacementFor(int codePoint)
    {
        if (codePoint >= 0x00 && codePoint <= 0x1F)
            return char.ConvertFromUtf32(0x2400 + codePoint);
        if (codePoint == 0x7F)
            return "␡";
        if (codePoint >= 0x80 && codePoint <= 0x9F)
            return "�";
        return null;
    }

    /// <summary>
    /// Rewrites every terminal control character in <paramref name="text"/> into a
    /// visible, inert placeholder (see <see cref="ReplacementFor(int)"/>), returning
    /// text that is safe to write directly to the terminal. The original string
    /// instance is returned unchanged when it contains no control characters. All
    /// control characters are in the Basic Multilingual Plane, so this operates one
    /// UTF-16 code unit at a time and never splits a surrogate pair.
    /// </summary>
    public static string Sanitize(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        int firstControl = -1;
        for (int i = 0; i < text.Length; i++)
        {
            if (IsControl(text[i])) { firstControl = i; break; }
        }
        if (firstControl < 0) return text;

        var sb = new StringBuilder(text.Length + 8);
        sb.Append(text, 0, firstControl);
        for (int i = firstControl; i < text.Length; i++)
        {
            char c = text[i];
            var replacement = ReplacementFor(c);
            if (replacement is null) sb.Append(c);
            else sb.Append(replacement);
        }
        return sb.ToString();
    }
}
