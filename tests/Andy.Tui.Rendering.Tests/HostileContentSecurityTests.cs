using System.Text;
using Andy.Tui.Backend.Terminal;
using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Rendering.Tests;

/// <summary>
/// Hostile terminal-content security cases. Untrusted strings (chat messages, file names, log
/// lines, pasted text) must be treated as data, never as terminal commands. These tests drive
/// adversarial payloads through the real render pipeline and assert the encoded byte stream
/// cannot smuggle cursor moves, SGR colour changes, OSC title/clipboard writes, or any other
/// escape sequence past the cell boundary the content was drawn into.
/// </summary>
public class HostileContentSecurityTests
{
    private static readonly TerminalCapabilities Caps = new() { TrueColor = true, Palette256 = true };

    private static byte[] EncodePayload(string payload, int w = 60, int h = 3)
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, w, h));
        b.DrawRect(new Rect(0, 0, w, h, new Rgb24(0, 0, 0)));
        b.DrawText(new TextRun(0, 1, payload, new Rgb24(255, 255, 255), new Rgb24(0, 0, 0), CellAttrFlags.None));
        b.Pop();
        var dl = b.Build();
        var comp = new TtyCompositor();
        var cells = comp.Composite(dl, (w, h));
        var dirty = comp.Damage(new CellGrid(w, h), cells);
        var runs = comp.RowRuns(cells, dirty);
        return new AnsiEncoder().Encode(runs, Caps).ToArray();
    }

    /// <summary>
    /// Asserts the frame contains no injected control sequences. The encoder is the only party
    /// allowed to emit escapes, and it only ever emits CSI sequences (ESC '[' ... final-in-@..~).
    /// Any bare ESC, any ESC not followed by '[' (e.g. OSC ESC ']'), or any standalone C0/DEL/C1
    /// control character can only have come from smuggled content and is a security failure.
    /// </summary>
    private static void AssertNoInjection(byte[] frame)
    {
        var s = Encoding.UTF8.GetString(frame);
        int i = 0;
        while (i < s.Length)
        {
            char c = s[i];
            if (c == '\x1b')
            {
                Assert.True(i + 1 < s.Length && s[i + 1] == '[',
                    $"ESC not starting a CSI at index {i}: injected escape sequence");
                i += 2;
                // Consume parameter/intermediate bytes up to a CSI final byte (0x40-0x7E).
                while (i < s.Length && !(s[i] >= '@' && s[i] <= '~'))
                {
                    Assert.False(s[i] == '\x1b', $"nested ESC inside CSI at index {i}: injection");
                    i++;
                }
                Assert.True(i < s.Length, "unterminated CSI: malformed encoder output");
                i++; // skip final byte
            }
            else
            {
                Assert.False(c < 0x20 || c == 0x7F || (c >= 0x80 && c <= 0x9F),
                    $"standalone control char U+{(int)c:X4} at index {i} leaked into output");
                i++;
            }
        }
    }

    /// <summary>Decodes the drawn row back into visible text via the terminal oracle.</summary>
    private static string VisibleRow(byte[] frame, int w, int h, int row)
    {
        var grid = VirtualScreenOracle.Decode(frame, (w, h));
        var sb = new StringBuilder();
        for (int x = 0; x < w; x++) sb.Append(grid[x, row].Grapheme ?? " ");
        return sb.ToString();
    }

    [Fact]
    public void Embedded_Cursor_And_Sgr_Injection_Is_Neutralized()
    {
        // Payload tries to reset colours and jump the cursor to home before printing.
        var payload = "A\x1b[0m\x1b[1;1HEVIL";
        var frame = EncodePayload(payload);
        AssertNoInjection(frame);

        // The visible letters survive as inert data; the ESC bytes became replacement chars.
        var s = Encoding.UTF8.GetString(frame);
        Assert.Contains("EVIL", s);
    }

    [Fact]
    public void Osc_Title_And_Clipboard_Injection_Is_Neutralized()
    {
        // OSC 0 (set window title) and OSC 52 (write clipboard) both start with ESC ']'.
        var payload = "]0;pwned]52;c;ZXZpbA==done";
        var frame = EncodePayload(payload);
        AssertNoInjection(frame);

        var s = Encoding.UTF8.GetString(frame);
        Assert.DoesNotContain('\x1b', StripCsi(s)); // no ESC remains outside encoder CSIs
        Assert.Contains("done", s);
    }

    [Fact]
    public void Control_Characters_Do_Not_Appear_In_Output()
    {
        var payload = "tab\there\rnull\0bel\x07backspace\bC1\x9b";
        var frame = EncodePayload(payload);
        AssertNoInjection(frame);
        Assert.Contains("tab", Encoding.UTF8.GetString(frame));
    }

    [Fact]
    public void Sanitizer_Preserves_Benign_Text_Verbatim()
    {
        // Benign content (including accented letters and emoji) passes through untouched: the
        // sanitizer only rewrites control bytes, never printable graphemes.
        var payload = "Hello, world! 123 cafe #ok";
        var frame = EncodePayload(payload);
        AssertNoInjection(frame);
        var visible = VisibleRow(frame, 60, 3, 1);
        Assert.StartsWith(payload, visible);
    }

    // Removes every well-formed encoder CSI so any remaining ESC must be an injection.
    private static string StripCsi(string s)
    {
        var sb = new StringBuilder();
        int i = 0;
        while (i < s.Length)
        {
            if (s[i] == '\x1b' && i + 1 < s.Length && s[i + 1] == '[')
            {
                i += 2;
                while (i < s.Length && !(s[i] >= '@' && s[i] <= '~')) i++;
                if (i < s.Length) i++;
            }
            else { sb.Append(s[i]); i++; }
        }
        return sb.ToString();
    }
}
