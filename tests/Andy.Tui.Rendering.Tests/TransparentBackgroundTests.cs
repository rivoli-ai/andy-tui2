using Andy.Tui.Backend.Terminal;
using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Rendering.Tests;

/// <summary>
/// Transparency is represented by a <c>null</c> background (not an RGB value).
/// A null background must survive compositing and be emitted as the terminal
/// default background (ESC[49m), so the terminal's own background — including
/// transparency — shows through.
/// </summary>
public class TransparentBackgroundTests
{
    // ---- Compositor ------------------------------------------------------

    [Fact]
    public void Untouched_Cells_Are_Transparent()
    {
        // Nothing drawn: every cell defaults to a transparent (null) background.
        var dl = new DisplayListBuilder().Build();
        var g = new TtyCompositor().Composite(dl, (3, 2));
        for (int y = 0; y < 2; y++)
            for (int x = 0; x < 3; x++)
                Assert.Null(g[x, y].Bg);
    }

    [Fact]
    public void Transparent_Rect_Does_Not_Paint_Background()
    {
        // A rect with a null fill paints nothing, leaving the cells transparent.
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, 5, 1));
        b.DrawRect(new Rect(0, 0, 5, 1, Fill: null));
        b.Pop();
        var g = new TtyCompositor().Composite(b.Build(), (5, 1));
        Assert.Null(g[0, 0].Bg);
    }

    [Fact]
    public void Transparent_Rect_Preserves_Existing_Content_Underneath()
    {
        // An opaque rect, then a transparent rect over the same region:
        // the transparent rect is a no-op and the opaque fill remains.
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, 5, 1));
        b.DrawRect(new Rect(0, 0, 5, 1, new Rgb24(7, 8, 9)));
        b.DrawRect(new Rect(0, 0, 5, 1, Fill: null));
        b.Pop();
        var g = new TtyCompositor().Composite(b.Build(), (5, 1));
        Assert.Equal(new Rgb24(7, 8, 9), g[0, 0].Bg);
    }

    [Fact]
    public void Text_Without_Bg_Over_Transparent_Stays_Transparent()
    {
        // Text with no background over a transparent cell keeps the cell transparent.
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, 5, 1));
        b.DrawText(new TextRun(0, 0, "A", new Rgb24(10, 10, 10), Bg: null, CellAttrFlags.None));
        b.Pop();
        var g = new TtyCompositor().Composite(b.Build(), (5, 1));
        Assert.Null(g[0, 0].Bg);
        Assert.Equal(new Rgb24(10, 10, 10), g[0, 0].Fg);
    }

    // ---- Encoder ---------------------------------------------------------

    [Fact]
    public void Null_Bg_Emits_Default_Background_Reset()
    {
        var runs = new[] { new RowRun(0, 0, 2, CellAttrFlags.None, new Rgb24(1, 2, 3), Bg: null, "ab") };
        var enc = new AnsiEncoder();
        var s = System.Text.Encoding.UTF8.GetString(enc.Encode(runs, new TerminalCapabilities { TrueColor = true, Palette256 = true }).Span);
        Assert.Contains("\x1b[49m", s);
        // Must NOT paint an explicit background.
        Assert.DoesNotContain("\x1b[48;", s);
    }

    [Fact]
    public void Null_Bg_Does_Not_Emit_Default_Reset_When_Truecolor_Disabled_Either()
    {
        // Transparent must be honored regardless of color capabilities — it is
        // never downconverted to an RGB/palette background.
        var runs = new[] { new RowRun(0, 0, 2, CellAttrFlags.None, new Rgb24(1, 2, 3), Bg: null, "ab") };
        var enc = new AnsiEncoder();
        var s = System.Text.Encoding.UTF8.GetString(enc.Encode(runs, new TerminalCapabilities { TrueColor = false, Palette256 = false }).Span);
        Assert.Contains("\x1b[49m", s);
        Assert.DoesNotContain("\x1b[48;", s);
        Assert.DoesNotContain("\x1b[40m", s); // not basic black either
    }

    [Fact]
    public void Switching_From_Explicit_To_Transparent_Emits_Reset()
    {
        var runs = new[]
        {
            new RowRun(0, 0, 2, CellAttrFlags.None, new Rgb24(1, 1, 1), new Rgb24(9, 9, 9), "ab"),
            new RowRun(1, 0, 2, CellAttrFlags.None, new Rgb24(1, 1, 1), Bg: null, "cd"),
        };
        var enc = new AnsiEncoder();
        var s = System.Text.Encoding.UTF8.GetString(enc.Encode(runs, new TerminalCapabilities { TrueColor = true, Palette256 = true }).Span);
        Assert.Contains("\x1b[48;2;9;9;9m", s); // first run explicit
        Assert.Contains("\x1b[49m", s);          // second run transparent
    }

    [Fact]
    public void Consecutive_Transparent_Runs_Emit_Reset_Only_Once()
    {
        var runs = new[]
        {
            new RowRun(0, 0, 2, CellAttrFlags.None, new Rgb24(1, 1, 1), Bg: null, "ab"),
            new RowRun(0, 2, 4, CellAttrFlags.None, new Rgb24(1, 1, 1), Bg: null, "cd"),
        };
        var enc = new AnsiEncoder();
        var s = System.Text.Encoding.UTF8.GetString(enc.Encode(runs, new TerminalCapabilities { TrueColor = true, Palette256 = true }).Span);
        int count = 0;
        for (int i = s.IndexOf("\x1b[49m", System.StringComparison.Ordinal); i >= 0; i = s.IndexOf("\x1b[49m", i + 1, System.StringComparison.Ordinal))
            count++;
        Assert.Equal(1, count);
    }

    [Fact]
    public void Transparent_Run_After_Attr_Reset_Still_Honored()
    {
        // An attr change emits ESC[0m (which also resets bg). A following
        // transparent run must still be expressed as the default background.
        var runs = new[]
        {
            new RowRun(0, 0, 2, CellAttrFlags.Bold, new Rgb24(1, 1, 1), new Rgb24(9, 9, 9), "ab"),
            new RowRun(1, 0, 2, CellAttrFlags.None, new Rgb24(1, 1, 1), Bg: null, "cd"),
        };
        var enc = new AnsiEncoder();
        var s = System.Text.Encoding.UTF8.GetString(enc.Encode(runs, new TerminalCapabilities { TrueColor = true, Palette256 = true }).Span);
        Assert.Contains("\x1b[49m", s);
    }

    // ---- Foreground transparency (ESC[39m) -------------------------------

    [Fact]
    public void Text_With_Null_Fg_Composites_To_Transparent_Foreground()
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, 5, 1));
        b.DrawText(new TextRun(0, 0, "A", Fg: null, Bg: null, CellAttrFlags.None));
        b.Pop();
        var g = new TtyCompositor().Composite(b.Build(), (5, 1));
        Assert.Null(g[0, 0].Fg);
        Assert.Null(g[0, 0].Bg);
    }

    [Fact]
    public void Null_Fg_Emits_Default_Foreground_Reset()
    {
        var runs = new[] { new RowRun(0, 0, 2, CellAttrFlags.None, Fg: null, new Rgb24(4, 5, 6), "ab") };
        var enc = new AnsiEncoder();
        var s = System.Text.Encoding.UTF8.GetString(enc.Encode(runs, new TerminalCapabilities { TrueColor = true, Palette256 = true }).Span);
        Assert.Contains("\x1b[39m", s);
        Assert.DoesNotContain("\x1b[38;", s);
    }

    [Fact]
    public void Null_Fg_Honored_Without_Truecolor()
    {
        var runs = new[] { new RowRun(0, 0, 2, CellAttrFlags.None, Fg: null, new Rgb24(4, 5, 6), "ab") };
        var enc = new AnsiEncoder();
        var s = System.Text.Encoding.UTF8.GetString(enc.Encode(runs, new TerminalCapabilities { TrueColor = false, Palette256 = false }).Span);
        Assert.Contains("\x1b[39m", s);
        Assert.DoesNotContain("\x1b[38;", s);
    }

    [Fact]
    public void Fully_Transparent_Run_Emits_Both_Defaults()
    {
        var runs = new[] { new RowRun(0, 0, 2, CellAttrFlags.None, Fg: null, Bg: null, "ab") };
        var enc = new AnsiEncoder();
        var s = System.Text.Encoding.UTF8.GetString(enc.Encode(runs, new TerminalCapabilities { TrueColor = true, Palette256 = true }).Span);
        Assert.Contains("\x1b[39m", s);
        Assert.Contains("\x1b[49m", s);
        Assert.DoesNotContain("\x1b[38;", s);
        Assert.DoesNotContain("\x1b[48;", s);
    }

    // ---- Frame baseline / cross-frame SGR leak ---------------------------

    [Fact]
    public void First_Run_Emits_Baseline_Reset_Even_With_Default_Attrs()
    {
        var runs = new[] { new RowRun(0, 0, 2, CellAttrFlags.None, new Rgb24(1, 2, 3), new Rgb24(4, 5, 6), "ab") };
        var enc = new AnsiEncoder();
        var s = System.Text.Encoding.UTF8.GetString(enc.Encode(runs, new TerminalCapabilities { TrueColor = true, Palette256 = true }).Span);
        Assert.Contains("\x1b[0m", s);
    }

    [Fact]
    public void Fresh_Encoder_Per_Frame_Resets_So_Attrs_Do_Not_Leak()
    {
        var caps = new TerminalCapabilities { TrueColor = true, Palette256 = true };
        // Frame 1 leaves Bold active.
        var f1 = new[] { new RowRun(0, 0, 2, CellAttrFlags.Bold, new Rgb24(1, 1, 1), new Rgb24(2, 2, 2), "ab") };
        _ = new AnsiEncoder().Encode(f1, caps);
        // Frame 2 (a new encoder, as FrameScheduler does) uses default attrs and
        // must begin with a reset so the prior Bold cannot bleed through.
        var f2 = new[] { new RowRun(0, 0, 2, CellAttrFlags.None, new Rgb24(1, 1, 1), new Rgb24(2, 2, 2), "cd") };
        var s2 = System.Text.Encoding.UTF8.GetString(new AnsiEncoder().Encode(f2, caps).Span);
        // Frame 2 must begin with a reset so the prior Bold cannot bleed through.
        Assert.Contains("\x1b[0m", s2);
        Assert.DoesNotContain("\x1b[1m", s2); // no stray bold
    }

    [Fact]
    public void Empty_Run_List_Produces_No_Output()
    {
        var enc = new AnsiEncoder();
        var s = System.Text.Encoding.UTF8.GetString(enc.Encode(System.Array.Empty<RowRun>(), new TerminalCapabilities { TrueColor = true, Palette256 = true }).Span);
        Assert.Equal(string.Empty, s);
    }

    [Fact]
    public void End_To_End_Transparent_Frame_Emits_Default_Background()
    {
        // Compose an empty (fully transparent) frame and encode every cell.
        var comp = new TtyCompositor();
        var grid = comp.Composite(new DisplayListBuilder().Build(), (4, 1));
        var dirty = new[] { new DirtyRect(0, 0, 4, 1) };
        var runs = comp.RowRuns(grid, dirty);
        var enc = new AnsiEncoder();
        var s = System.Text.Encoding.UTF8.GetString(enc.Encode(runs, new TerminalCapabilities { TrueColor = true, Palette256 = true }).Span);
        Assert.Contains("\x1b[49m", s);
        Assert.DoesNotContain("\x1b[48;", s);
    }
}
