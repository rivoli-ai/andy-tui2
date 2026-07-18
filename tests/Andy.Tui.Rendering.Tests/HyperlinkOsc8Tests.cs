using Andy.Tui.Backend.Terminal;
using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Rendering.Tests;

/// <summary>
/// Verifies the structured, capability-aware OSC 8 hyperlink pipeline: links are
/// carried as metadata (never embedded control bytes in the cell stream), gated on
/// terminal capabilities, sanitized against injection, and always terminated even
/// under clipping and across frame transitions. Covers issue #25.
/// </summary>
public class HyperlinkOsc8Tests
{
    private const string ESC = "";
    private const string BEL = "";
    private const string OSC8_OPEN_PREFIX = ESC + "]8;;";
    private const string OSC8_CLOSE = ESC + "]8;;" + ESC + "\\";
    // Marker for any OSC 8 introducer, present in both open and close.
    private const string OSC8_ANY = "]8;;";

    private static readonly TerminalCapabilities WithHyperlinks =
        new() { TrueColor = true, Palette256 = true, Hyperlinks = true };
    private static readonly TerminalCapabilities NoHyperlinks =
        new() { TrueColor = true, Palette256 = true, Hyperlinks = false };

    private static string Encode(IReadOnlyList<RowRun> runs, TerminalCapabilities caps)
        => System.Text.Encoding.UTF8.GetString(new AnsiEncoder().Encode(runs, caps).Span);

    private static IReadOnlyList<RowRun> Rows(Andy.Tui.DisplayList.DisplayList dl, int w, int h)
    {
        var comp = new TtyCompositor();
        var grid = comp.Composite(dl, (w, h));
        var damage = new List<DirtyRect> { new DirtyRect(0, 0, w, h) };
        return comp.RowRuns(grid, damage);
    }

    private static int Count(string haystack, string needle)
    {
        int n = 0, i = 0;
        while ((i = haystack.IndexOf(needle, i, System.StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }

    [Fact]
    public void Encoder_Emits_Balanced_Osc8_When_Supported()
    {
        var runs = new List<RowRun>
        {
            new RowRun(0, 0, 4, CellAttrFlags.None, new Rgb24(255,255,255), null, "link")
                { Hyperlink = "https://example.com" }
        };
        var s = Encode(runs, WithHyperlinks);
        Assert.Contains(OSC8_OPEN_PREFIX + "https://example.com" + ESC + "\\", s);
        Assert.Contains(OSC8_CLOSE, s);
        // Exactly one open and one close: no dangling state.
        Assert.Equal(1, Count(s, OSC8_OPEN_PREFIX + "https://example.com"));
        Assert.Equal(1, Count(s, OSC8_CLOSE));
    }

    [Fact]
    public void Encoder_Emits_Plain_Text_When_Terminal_Lacks_Hyperlinks()
    {
        var runs = new List<RowRun>
        {
            new RowRun(0, 0, 4, CellAttrFlags.None, new Rgb24(255,255,255), null, "link")
                { Hyperlink = "https://example.com" }
        };
        var s = Encode(runs, NoHyperlinks);
        Assert.DoesNotContain(OSC8_ANY, s);
        Assert.Contains("link", s);
    }

    [Fact]
    public void Sanitizer_Strips_Malicious_Delimiters_From_Uri()
    {
        // An attacker tries to terminate the OSC early (ESC \\), ring the bell (BEL),
        // and inject a fresh control sequence.
        var evil = "https://ok.com" + ESC + "\\" + BEL + ESC + "[31mHACK";
        var sanitized = AnsiEncoder.SanitizeHyperlinkUri(evil);
        Assert.Equal("https://ok.com\\[31mHACK", sanitized);
        Assert.False(sanitized.Contains('')); // no ESC survived
        Assert.False(sanitized.Contains('')); // no BEL survived

        // End-to-end: the emitted stream contains exactly one OSC 8 open and close,
        // and no stray ESC-backslash terminator inside the URI portion.
        var runs = new List<RowRun>
        {
            new RowRun(0, 0, 2, CellAttrFlags.None, new Rgb24(1,1,1), null, "go") { Hyperlink = evil }
        };
        var s = Encode(runs, WithHyperlinks);
        Assert.Equal(1, Count(s, OSC8_CLOSE));
        // The only ESC-backslash sequences are the two String Terminators (open + close).
        Assert.Equal(2, Count(s, ESC + "\\"));
    }

    [Fact]
    public void Narrow_Clip_Cannot_Produce_Unterminated_Osc()
    {
        // Draw a link far wider than the clip; clipping keeps only 3 visible cells.
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, 3, 1));
        b.DrawText(new TextRun(0, 0, "HelloWorld", new Rgb24(200, 200, 200), null, CellAttrFlags.Underline)
        {
            Hyperlink = "https://example.com/very/long/path"
        });
        b.Pop();
        var runs = Rows(b.Build(), 3, 1);
        var s = Encode(runs, WithHyperlinks);

        // Whatever survived the clip is bracketed by a matching open/close pair.
        Assert.Equal(Count(s, OSC8_OPEN_PREFIX + "https://example.com/very/long/path"), Count(s, OSC8_CLOSE));
        Assert.True(Count(s, OSC8_CLOSE) >= 1);
        // The clip limited the run to 3 cells; the URL never expanded the visible text.
        var linkRun = Assert.Single(runs, r => r.Hyperlink is not null);
        Assert.Equal(3, linkRun.Text.Length);
    }

    [Fact]
    public void Multiple_Links_On_One_Row_Are_Split_And_Each_Terminated()
    {
        var b = new DisplayListBuilder();
        b.DrawText(new TextRun(0, 0, "AA", new Rgb24(10, 10, 10), null, CellAttrFlags.None) { Hyperlink = "https://a.com" });
        b.DrawText(new TextRun(2, 0, "BB", new Rgb24(10, 10, 10), null, CellAttrFlags.None) { Hyperlink = "https://b.com" });
        var runs = Rows(b.Build(), 4, 1);

        // Two distinct hyperlink runs, one per URL.
        var linkRuns = runs.Where(r => r.Hyperlink is not null).ToList();
        Assert.Equal(2, linkRuns.Count);
        Assert.Contains(linkRuns, r => r.Hyperlink == "https://a.com");
        Assert.Contains(linkRuns, r => r.Hyperlink == "https://b.com");

        var s = Encode(runs, WithHyperlinks);
        Assert.Equal(1, Count(s, OSC8_OPEN_PREFIX + "https://a.com"));
        Assert.Equal(1, Count(s, OSC8_OPEN_PREFIX + "https://b.com"));
        Assert.Equal(2, Count(s, OSC8_CLOSE));
    }

    [Fact]
    public void Adjacent_Cells_Without_Link_Are_Not_Wrapped()
    {
        var b = new DisplayListBuilder();
        b.DrawText(new TextRun(0, 0, "AA", new Rgb24(10, 10, 10), null, CellAttrFlags.None) { Hyperlink = "https://a.com" });
        b.DrawText(new TextRun(2, 0, "BB", new Rgb24(10, 10, 10), null, CellAttrFlags.None)); // no link
        var runs = Rows(b.Build(), 4, 1);
        var s = Encode(runs, WithHyperlinks);
        // Only the "AA" run is a hyperlink; the plain "BB" cells stay outside any OSC 8.
        Assert.Equal(1, Count(s, OSC8_CLOSE));
        Assert.Contains("BB", s);
    }

    [Fact]
    public void Frame_Transition_Does_Not_Leak_Hyperlink_State()
    {
        // Frame 1: a link. Frame 2: the same region with no link.
        var b1 = new DisplayListBuilder();
        b1.DrawText(new TextRun(0, 0, "on", new Rgb24(5, 5, 5), null, CellAttrFlags.None) { Hyperlink = "https://x.com" });
        var s1 = Encode(Rows(b1.Build(), 4, 1), WithHyperlinks);
        Assert.Equal(Count(s1, OSC8_OPEN_PREFIX + "https://x.com"), Count(s1, OSC8_CLOSE));

        var b2 = new DisplayListBuilder();
        b2.DrawText(new TextRun(0, 0, "off", new Rgb24(5, 5, 5), null, CellAttrFlags.None)); // no link
        var s2 = Encode(Rows(b2.Build(), 4, 1), WithHyperlinks);
        // The second frame never opens or (re)closes a hyperlink: state is fully
        // closed at the previous frame's boundary because every run self-terminates.
        Assert.DoesNotContain(OSC8_ANY, s2);
    }

    [Fact]
    public void Link_Widget_End_To_End_Emits_Safe_Hyperlink()
    {
        var link = new Andy.Tui.Widgets.Link();
        link.SetText("Docs");
        link.SetUrl("https://example.com/docs" + ESC + "\\evil");
        link.EnableOsc8(true);
        var b = new DisplayListBuilder();
        link.Render(new L.Rect(0, 0, 10, 1), new DisplayListBuilder().Build(), b);

        var runs = Rows(b.Build(), 10, 1);
        var s = Encode(runs, WithHyperlinks);

        Assert.Contains("Docs", s);
        // The injected ESC \\ terminator was stripped, so there is a single matched pair.
        Assert.Equal(1, Count(s, OSC8_CLOSE));
        Assert.Equal(2, Count(s, ESC + "\\"));

        // Without terminal support the same widget output degrades to plain text.
        var plain = Encode(runs, NoHyperlinks);
        Assert.DoesNotContain(OSC8_ANY, plain);
        Assert.Contains("Docs", plain);
    }
}
