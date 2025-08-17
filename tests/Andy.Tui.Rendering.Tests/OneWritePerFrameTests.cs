using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Rendering.Tests;

file sealed class MockPty : IPtyIo
{
    public int Writes;
    public ReadOnlyMemory<byte> LastBytes;
    public Task WriteAsync(ReadOnlyMemory<byte> frameBytes, CancellationToken cancellationToken)
    {
        Writes++;
        LastBytes = frameBytes;
        return Task.CompletedTask;
    }
}

public class OneWritePerFrameTests
{
    [Fact]
    public async Task Encoder_Writes_Once_Per_Frame()
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0,0,5,1));
        b.DrawText(new TextRun(0,0,"hi", new Rgb24(255,255,255), null, CellAttrFlags.Bold));
        b.Pop();
        var comp = new TtyCompositor();
        var grid = comp.Composite(b.Build(), (5,1));
        var dirty = comp.Damage(new CellGrid(5,1), grid);
        var runs = comp.RowRuns(grid, dirty);
        var pty = new MockPty();
        await new FrameWriter().RenderFrameAsync(runs, new TerminalCapabilities{ TrueColor=true, Palette256=true }, pty);
        Assert.Equal(1, pty.Writes);
        Assert.True(pty.LastBytes.Length > 0);
    }
}
