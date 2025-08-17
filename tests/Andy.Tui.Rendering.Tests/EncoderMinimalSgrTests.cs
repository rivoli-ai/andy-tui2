using Andy.Tui.Backend.Terminal;
using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Rendering.Tests;

public class EncoderMinimalSgrTests
{
    [Fact]
    public void No_Redundant_SGR_When_Colors_And_Attrs_Same()
    {
        var runs = new []
        {
            new RowRun(0,0,2, CellAttrFlags.Bold, new Rgb24(1,2,3), new Rgb24(4,5,6), "ab"),
            new RowRun(0,2,4, CellAttrFlags.Bold, new Rgb24(1,2,3), new Rgb24(4,5,6), "cd"),
        };
        var s = System.Text.Encoding.UTF8.GetString(new AnsiEncoder().Encode(runs, new TerminalCapabilities{ TrueColor=true, Palette256=true }).Span);
        // Expect one reset, one attrs apply, and one set of fg/bg, not duplicated for second run
        Assert.True(s.Split("\x1b[0m").Length - 1 <= 1);
        Assert.True(s.Split("\x1b[1m").Length - 1 <= 1);
        Assert.True(s.Split("\x1b[38;2;1;2;3m").Length - 1 <= 1);
        Assert.True(s.Split("\x1b[48;2;4;5;6m").Length - 1 <= 1);
    }
}
