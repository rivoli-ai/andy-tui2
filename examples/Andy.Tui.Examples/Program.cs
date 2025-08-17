using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;

class Program
{
    static async Task Main()
    {
        var caps = CapabilityDetector.DetectFromEnvironment();
        var viewport = (Width: Console.WindowWidth, Height: Console.WindowHeight);

        // Example 1: Hello box
        var hello = new DisplayListBuilder();
        hello.PushClip(new ClipPush(0,0,viewport.Width, viewport.Height));
        hello.DrawRect(new Rect(0,0,viewport.Width, viewport.Height, new Rgb24(0,0,0)));
        hello.DrawBorder(new Border(2,1, 30, 5, "single", new Rgb24(180,180,180)));
        hello.DrawText(new TextRun(4,3, "Hello, Andy.Tui!", new Rgb24(200,200,50), null, CellAttrFlags.Bold));
        hello.Pop();

        await RenderAsync(hello, viewport, caps);

        Console.WriteLine();
        Console.WriteLine("Press Enter for Colors example...");
        Console.ReadLine();

        // Example 2: Color runs
        var colors = new DisplayListBuilder();
        colors.PushClip(new ClipPush(0,0,viewport.Width, viewport.Height));
        colors.DrawRect(new Rect(0,0,viewport.Width, viewport.Height, new Rgb24(0,0,0)));
        colors.DrawText(new TextRun(2,2, "Red", new Rgb24(255,0,0), null, CellAttrFlags.Bold));
        colors.DrawText(new TextRun(8,2, "Green", new Rgb24(0,255,0), null, CellAttrFlags.None));
        colors.DrawText(new TextRun(15,2, "Blue", new Rgb24(0,0,255), null, CellAttrFlags.Underline));
        colors.Pop();

        await RenderAsync(colors, viewport, caps);

        Console.WriteLine();
        Console.WriteLine("Done. Press Enter to exit.");
        Console.ReadLine();
    }

    static async Task RenderAsync(DisplayListBuilder builder, (int Width,int Height) viewport, TerminalCapabilities caps)
    {
        var dl = builder.Build();
        var comp = new TtyCompositor();
        var cells = comp.Composite(dl, viewport);
        var dirty = comp.Damage(new CellGrid(viewport.Width, viewport.Height), cells);
        var runs = comp.RowRuns(cells, dirty);
        var pty = new StdoutPty();
        await new FrameWriter().RenderFrameAsync(runs, caps, pty, CancellationToken.None);
    }
}

file sealed class StdoutPty : IPtyIo
{
    public Task WriteAsync(ReadOnlyMemory<byte> frameBytes, CancellationToken cancellationToken)
    {
        var s = Encoding.UTF8.GetString(frameBytes.Span);
        Console.Write(s);
        return Task.CompletedTask;
    }
}
