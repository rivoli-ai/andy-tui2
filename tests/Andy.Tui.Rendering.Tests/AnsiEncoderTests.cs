using Andy.Tui.Backend.Terminal;
using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Rendering.Tests;

public class AnsiEncoderTests
{
    [Fact]
    public void Encoder_Emits_Cursor_Moves_And_Text()
    {
        var runs = new List<RowRun>
        {
            new RowRun(0, 1, 3, CellAttrFlags.None, new Rgb24(255,255,255), new Rgb24(0,0,0), "hi")
        };
        var enc = new AnsiEncoder();
        var bytes = enc.Encode(runs, new TerminalCapabilities{ TrueColor=true, Palette256=true });
        var s = System.Text.Encoding.UTF8.GetString(bytes.Span);
        Assert.Contains("\x1b[1;2H", s); // row 1, col 2
        Assert.Contains("hi", s);
    }
}
