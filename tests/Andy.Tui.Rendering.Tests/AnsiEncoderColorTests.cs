using Andy.Tui.Backend.Terminal;
using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Rendering.Tests;

public class AnsiEncoderColorTests
{
    [Fact]
    public void Truecolor_Sets_Exact_FgBg()
    {
        var runs = new[] { new RowRun(0, 0, 2, CellAttrFlags.None, new Rgb24(1, 2, 3), new Rgb24(4, 5, 6), "ab") };
        var enc = new AnsiEncoder();
        var s = System.Text.Encoding.UTF8.GetString(enc.Encode(runs, new TerminalCapabilities { TrueColor = true, Palette256 = true }).Span);
        Assert.Contains("\x1b[38;2;1;2;3m", s);
        Assert.Contains("\x1b[48;2;4;5;6m", s);
    }

    [Fact]
    public void Palette256_Sets_Indexed_FgBg()
    {
        var runs = new[] { new RowRun(0, 0, 2, CellAttrFlags.None, new Rgb24(255, 0, 0), new Rgb24(0, 255, 0), "ab") };
        var enc = new AnsiEncoder();
        var s = System.Text.Encoding.UTF8.GetString(enc.Encode(runs, new TerminalCapabilities { TrueColor = false, Palette256 = true }).Span);
        Assert.Contains("\x1b[38;5;196m", s);
        Assert.Contains("\x1b[48;5;46m", s);
    }

    [Fact]
    public void Basic16_Falls_Back()
    {
        var runs = new[] { new RowRun(0, 0, 2, CellAttrFlags.None, new Rgb24(255, 255, 0), new Rgb24(0, 0, 255), "ab") };
        var enc = new AnsiEncoder();
        var s = System.Text.Encoding.UTF8.GetString(enc.Encode(runs, new TerminalCapabilities { TrueColor = false, Palette256 = false }).Span);
        // Foreground basic must include either 33(yellow) or bright 93
        Assert.True(s.Contains("\x1b[33m") || s.Contains("\x1b[93m"));
        // Background basic must include either 44(blue) or bright 104
        Assert.True(s.Contains("\x1b[44m") || s.Contains("\x1b[104m"));
    }
}
