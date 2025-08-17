using Andy.Tui.Backend.Terminal;
using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Rendering.Tests;

public class E2eParityTests
{
    [Fact]
    public void Dl_To_Cells_To_Bytes_Roundtrip_TextPlacement()
    {
        var dlb = new DisplayListBuilder();
        dlb.PushClip(new ClipPush(0,0,10,3));
        dlb.DrawRect(new Rect(0,0,10,3,new Rgb24(0,0,0)));
        dlb.DrawText(new TextRun(2,1,"hello", new Rgb24(255,255,255), null, CellAttrFlags.None));
        dlb.Pop();
        var dl = dlb.Build();

        var comp = new TtyCompositor();
        var cells = comp.Composite(dl, (10,3));
        var dirty = comp.Damage(new CellGrid(10,3), cells);
        var runs = comp.RowRuns(cells, dirty);

        var enc = new AnsiEncoder();
        var bytes = enc.Encode(runs, new TerminalCapabilities{ TrueColor=true, Palette256=true });
        var oracle = VirtualScreenOracle.Decode(bytes.Span, (10,3));

        Assert.Equal(cells[2,1].Grapheme, oracle[2,1].Grapheme);
        Assert.Equal("h", oracle[2,1].Grapheme);
    }
}
