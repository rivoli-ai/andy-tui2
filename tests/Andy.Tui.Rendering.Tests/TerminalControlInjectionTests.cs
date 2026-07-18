using System.Text;
using System.Text.RegularExpressions;
using Andy.Tui.Backend.Terminal;
using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;
using Andy.Tui.Text;

namespace Andy.Tui.Rendering.Tests;

/// <summary>
/// Guards the trust boundary described by issue #22: adversarial terminal
/// control sequences arriving as ordinary text (logs, Markdown, chat, filenames,
/// network data) must be displayed as inert glyphs, never executed. Safe controls
/// required by the backend (cursor moves, SGR colors/attributes) must still work
/// through the typed encoder path.
/// </summary>
public class TerminalControlInjectionTests
{
    private static readonly TerminalCapabilities Caps =
        new TerminalCapabilities { TrueColor = true, Palette256 = true };

    // Matches exactly the CSI sequences the encoder is allowed to emit:
    // cursor moves (ESC[row;colH) and SGR (ESC[..m). Params are digits/semicolons only.
    private static readonly Regex EncoderCsi = new("\x1b\\[[0-9;]*[A-Za-z]", RegexOptions.Compiled);

    /// <summary>
    /// Asserts that, after removing the CSI sequences the encoder is legitimately
    /// allowed to produce, no terminal control byte survives anywhere in the output.
    /// A single leftover ESC, BEL, CR, LF, or C1 introducer would mean injected text
    /// escaped the text plane.
    /// </summary>
    private static void AssertNoExecutableControls(string encoded)
    {
        var residual = EncoderCsi.Replace(encoded, string.Empty);
        foreach (var ch in residual)
        {
            Assert.False(TerminalText.IsControl(ch),
                $"Residual control character U+{(int)ch:X4} escaped the text plane: {residual}");
        }
    }

    private static string EncodeThroughPipeline(string content, (int Width, int Height) viewport, int x = 0, int y = 0)
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, viewport.Width, viewport.Height));
        b.DrawText(new TextRun(x, y, content, new Rgb24(255, 255, 255), new Rgb24(0, 0, 0), CellAttrFlags.None));
        b.Pop();
        var comp = new TtyCompositor();
        var grid = comp.Composite(b.Build(), viewport);
        var dirty = comp.Damage(new CellGrid(viewport.Width, viewport.Height), grid);
        var runs = comp.RowRuns(grid, dirty);
        return Encoding.UTF8.GetString(new AnsiEncoder().Encode(runs, Caps).Span);
    }

    // ---- Compositor: control bytes never become visible cells ----------------

    [Fact]
    public void Compositor_Does_Not_Store_Control_Characters_As_Cells()
    {
        // CSI clear-screen, OSC set-title, BEL terminator, bare ESC, CR, LF.
        var evil = "\x1b[2J\x1b]0;pwned\x07A\rB\nC\x1bD";
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, 40, 1));
        b.DrawText(new TextRun(0, 0, evil, new Rgb24(255, 255, 255), null, CellAttrFlags.None));
        b.Pop();
        var grid = new TtyCompositor().Composite(b.Build(), (40, 1));

        for (int gx = 0; gx < grid.Width; gx++)
        {
            var g = grid[gx, 0].Grapheme;
            if (string.IsNullOrEmpty(g)) continue;
            foreach (var ch in g)
                Assert.False(TerminalText.IsControl(ch), $"Cell {gx} held control U+{(int)ch:X4}");
        }

        // ESC (U+001B) is displayed as its Control Picture "␛" (U+241B), not executed.
        Assert.Equal("␛", grid[0, 0].Grapheme);
    }

    // ---- Direct text through the encoder -------------------------------------

    [Fact]
    public void DirectText_Csi_And_Osc_Cannot_Execute_Through_TextRun()
    {
        var evil = "\x1b[31mred\x1b[0m and \x1b]0;title\x07 done";
        var s = EncodeThroughPipeline(evil, (60, 1));
        AssertNoExecutableControls(s);
        // The injected ESC introducers were rewritten to the inert Control Picture
        // glyph "␛" (U+241B) instead of being emitted as raw ESC, and BEL to "␇".
        Assert.Contains("␛", s);
        Assert.Contains("␇", s);
        // The visible text survives.
        Assert.Contains("red", s);
    }

    [Fact]
    public void Osc8_Hyperlink_Injection_Is_Neutralised()
    {
        var evil = "\x1b]8;;http://evil.example\x1b\\click me\x1b]8;;\x1b\\";
        var s = EncodeThroughPipeline(evil, (60, 1));
        AssertNoExecutableControls(s);
        // Every ESC introducer of the OSC 8 hyperlink is now the inert "␛" glyph.
        Assert.Contains("␛", s);
        Assert.Contains("click me", s);
    }

    [Fact]
    public void Osc52_Clipboard_Injection_Is_Neutralised()
    {
        var evil = "\x1b]52;c;ZXZpbA==\x07";
        var s = EncodeThroughPipeline(evil, (60, 1));
        AssertNoExecutableControls(s);
        // The OSC 52 introducer (ESC) and BEL terminator are inert glyphs now.
        Assert.Contains("␛", s);
        Assert.Contains("␇", s);
    }

    [Fact]
    public void Bel_And_Bare_Esc_And_Partial_Sequences_Are_Neutralised()
    {
        // A lone BEL, a bare ESC, and a truncated/partial CSI (no final byte).
        var evil = "ding\x07 esc\x1b partial\x1b[3";
        var s = EncodeThroughPipeline(evil, (60, 1));
        AssertNoExecutableControls(s);
        Assert.Contains("ding", s);
        Assert.Contains("partial", s);
    }

    [Fact]
    public void C1_Control_Introducers_Are_Neutralised()
    {
        // 0x9B is the 8-bit CSI introducer; 0x9D is the 8-bit OSC introducer.
        var evil = "\u009B2Jtext\u009D0;pwn\u0007";
        var s = EncodeThroughPipeline(evil, (60, 1));
        AssertNoExecutableControls(s);
        Assert.Contains("text", s);
    }

    // ---- Source-shaped payloads (issue acceptance list) ----------------------

    [Fact]
    public void Markdown_Content_With_Embedded_Escape_Is_Safe()
    {
        var md = "See [the docs](http://x)\x1b[2K and **bold**\x1b]0;md\x07";
        var s = EncodeThroughPipeline(md, (80, 1));
        AssertNoExecutableControls(s);
        Assert.Contains("the docs", s);
    }

    [Fact]
    public void Log_Line_With_Carriage_Return_Overwrite_Is_Safe()
    {
        // Classic CR trick to overwrite an "OK" status with a fake one.
        var log = "2026-07-17 INFO request ok\rERROR forged line";
        var s = EncodeThroughPipeline(log, (80, 1));
        AssertNoExecutableControls(s);
        Assert.Contains("forged line", s); // shown as text, CR shown as "␍"
    }

    [Fact]
    public void Chat_Message_With_Ansi_Colors_Is_Safe()
    {
        var chat = "hey \x1b[5;31mLOOK AT ME\x1b[0m please";
        var s = EncodeThroughPipeline(chat, (80, 1));
        AssertNoExecutableControls(s);
        // The injected SGR ESC introducers were rewritten to the inert "␛" glyph, so
        // the payload cannot recolour the terminal. Without this positive assertion the
        // test would still pass if AppendSanitized were reverted, because the residual
        // CSI stripper in AssertNoExecutableControls treats a raw ESC[..m as legitimate.
        Assert.Contains("␛", s);
        Assert.Contains("LOOK AT ME", s);
    }

    [Fact]
    public void Filename_With_Embedded_Escape_Is_Safe()
    {
        var filename = "report\x1b[2Kmonthly\x07.txt";
        var s = EncodeThroughPipeline(filename, (80, 1));
        AssertNoExecutableControls(s);
        Assert.Contains("report", s);
        Assert.Contains(".txt", s);
    }

    // ---- Split / clipped content ---------------------------------------------

    [Fact]
    public void Clipped_Escape_Sequence_Cannot_Be_Cut_Before_Its_Terminator()
    {
        // The escape sequence would straddle a narrow clip. Because controls never
        // become cells, there is no sequence to cut — the clip just drops glyphs.
        var evil = "AB\x1b]0;pwned\x07CD";
        var s = EncodeThroughPipeline(evil, (4, 1)); // clip is 4 columns wide
        AssertNoExecutableControls(s);
        // The ESC that fell inside the clip is an inert "␛" glyph, not a raw introducer.
        Assert.Contains("␛", s);
        Assert.Contains("A", s);
        Assert.Contains("B", s);
    }

    [Fact]
    public void Sequence_Split_Across_Two_TextRuns_Is_Safe()
    {
        // An attacker splits ESC]0; ... BEL across two adjacent runs hoping the
        // encoder concatenates them into a live OSC sequence.
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, 40, 1));
        b.DrawText(new TextRun(0, 0, "\x1b]0;p", new Rgb24(255, 255, 255), new Rgb24(0, 0, 0), CellAttrFlags.None));
        b.DrawText(new TextRun(5, 0, "wned\x07", new Rgb24(255, 255, 255), new Rgb24(0, 0, 0), CellAttrFlags.None));
        b.Pop();
        var comp = new TtyCompositor();
        var grid = comp.Composite(b.Build(), (40, 1));
        var dirty = comp.Damage(new CellGrid(40, 1), grid);
        var runs = comp.RowRuns(grid, dirty);
        var s = Encoding.UTF8.GetString(new AnsiEncoder().Encode(runs, Caps).Span);
        AssertNoExecutableControls(s);
        // Even split across runs, both the ESC and BEL are inert glyphs — the encoder
        // never reassembled a live OSC sequence from the concatenation.
        Assert.Contains("␛", s);
        Assert.Contains("␇", s);
    }

    // ---- Encoder defense-in-depth for directly built RowRuns -----------------

    [Fact]
    public void Encoder_Sanitizes_RowRuns_Built_Directly()
    {
        // Bypass the compositor: hand the encoder a RowRun whose text carries controls.
        var runs = new List<RowRun>
        {
            new RowRun(0, 0, 5, CellAttrFlags.None, new Rgb24(255, 255, 255), new Rgb24(0, 0, 0), "\x1b]0;x\x07hi")
        };
        var s = Encoding.UTF8.GetString(new AnsiEncoder().Encode(runs, Caps).Span);
        AssertNoExecutableControls(s);
        // The encoder sanitized the directly-built RowRun: ESC -> "␛", BEL -> "␇".
        Assert.Contains("␛", s);
        Assert.Contains("␇", s);
        Assert.Contains("hi", s);
    }

    // ---- Typed / trusted controls still work ---------------------------------

    [Fact]
    public void Trusted_Controls_Through_Typed_Api_Still_Work()
    {
        // Colors, attributes and cursor positioning flow through typed fields and
        // must still be emitted — sanitization only touches Content text.
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, 20, 2));
        b.DrawText(new TextRun(2, 1, "ok", new Rgb24(200, 100, 50), new Rgb24(0, 0, 0), CellAttrFlags.Bold));
        b.Pop();
        var comp = new TtyCompositor();
        var grid = comp.Composite(b.Build(), (20, 2));
        var dirty = comp.Damage(new CellGrid(20, 2), grid);
        var runs = comp.RowRuns(grid, dirty);
        var s = Encoding.UTF8.GetString(new AnsiEncoder().Encode(runs, Caps).Span);

        Assert.Contains("\x1b[2;3H", s);           // cursor move to row 2, col 3
        Assert.Contains("\x1b[1m", s);              // bold attribute
        Assert.Contains("\x1b[38;2;200;100;50m", s); // truecolor foreground
        Assert.Contains("ok", s);
    }

    // ---- Sanitizer unit behaviour --------------------------------------------

    [Fact]
    public void Sanitize_Returns_Same_Instance_When_Clean()
    {
        var clean = "hello world .txt (unicode: café 🚀)";
        Assert.Same(clean, TerminalText.Sanitize(clean));
    }

    [Theory]
    [InlineData(0x1B, "␛")] // ESC -> "␛"
    [InlineData(0x07, "␇")] // BEL -> "␇"
    [InlineData(0x0D, "␍")] // CR  -> "␍"
    [InlineData(0x0A, "␊")] // LF  -> "␊"
    [InlineData(0x00, "␀")] // NUL -> "␀"
    [InlineData(0x7F, "␡")] // DEL -> "␡"
    [InlineData(0x9B, "�")] // C1 CSI -> replacement char
    public void ReplacementFor_Maps_Controls_To_Inert_Glyphs(int codePoint, string expected)
    {
        Assert.Equal(expected, TerminalText.ReplacementFor(codePoint));
        foreach (var ch in expected)
            Assert.False(TerminalText.IsControl(ch));
    }

    [Fact]
    public void ReplacementFor_Returns_Null_For_Ordinary_Text()
    {
        Assert.Null(TerminalText.ReplacementFor('A'));
        Assert.Null(TerminalText.ReplacementFor(0x1F600)); // emoji
    }
}
