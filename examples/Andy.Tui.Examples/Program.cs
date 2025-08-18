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

        // Example 1: Hello box + HUD toggle
        var hello = new DisplayListBuilder();
        hello.PushClip(new ClipPush(0, 0, viewport.Width, viewport.Height));
        hello.DrawRect(new Rect(0, 0, viewport.Width, viewport.Height, new Rgb24(0, 0, 0)));
        hello.DrawBorder(new Border(2, 1, 30, 5, "single", new Rgb24(180, 180, 180)));
        hello.DrawText(new TextRun(4, 3, "Hello, Andy.Tui!", new Rgb24(200, 200, 50), null, CellAttrFlags.Bold));
        hello.Pop();

        await RenderAsync(hello, viewport, caps, showHud: true);

        Console.WriteLine();
        Console.WriteLine("Press Enter for Colors example...");
        Console.ReadLine();

        // Example 2: Interactive HUD toggle, live resize, and simple color transition demo
        var scheduler = new Andy.Tui.Core.FrameScheduler();
        var hud = new Andy.Tui.Observability.HudOverlay { Enabled = true };
        scheduler.SetMetricsSink(hud);
        var pty = new StdoutPty();

        bool running = true;
        bool showHud = true;
        long animStart = Environment.TickCount64;
        Console.WriteLine();
        Console.WriteLine("Interactive demo: press 'h' to toggle HUD, 'q' to quit.");
        while (running)
        {
            // Handle simple key input (non-blocking)
            while (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.H)
                {
                    showHud = !showHud;
                    hud.Enabled = showHud;
                }
                else if (key.Key == ConsoleKey.Q)
                {
                    running = false;
                }
            }

            // Detect terminal resize
            viewport = (Console.WindowWidth, Console.WindowHeight);

            // Build base scene with animated color for a label
            var baseBuilder = new DisplayListBuilder();
            baseBuilder.PushClip(new ClipPush(0, 0, viewport.Width, viewport.Height));
            baseBuilder.DrawRect(new Rect(0, 0, viewport.Width, viewport.Height, new Rgb24(0, 0, 0)));
            var elapsed = Environment.TickCount64 - animStart;
            var from = new Rgb24(50, 150, 250);
            var to = new Rgb24(250, 100, 50);
            var tcol = Andy.Tui.Animations.ColorTransitionApplier.Apply(new TextRun(2, 1, $"HUD: {(showHud ? "ON" : "OFF")}  Size: {viewport.Width}x{viewport.Height}  (h=toggle, q=quit)", from, null, CellAttrFlags.None), animStart, Environment.TickCount64, new Andy.Tui.Animations.TransitionColor(from, to, 2000));
            baseBuilder.DrawText(tcol);
            baseBuilder.DrawBorder(new Border(1, 2, Math.Max(10, viewport.Width - 2), Math.Max(3, viewport.Height - 3), "single", new Rgb24(100, 100, 100)));
            baseBuilder.Pop();

            var baseDl = baseBuilder.Build();
            // Build HUD overlay ops
            var overlayBuilder = new DisplayListBuilder();
            hud.Contribute(baseDl, overlayBuilder);
            var overlayDl = overlayBuilder.Build();

            // Combine base + overlay into one DL
            var combined = Combine(baseDl, overlayDl);
            await scheduler.RenderOnceAsync(combined, viewport, caps, pty, CancellationToken.None);

            // Small delay to avoid busy loop
            await Task.Delay(16);
        }
    }

    static async Task RenderAsync(DisplayListBuilder builder, (int Width, int Height) viewport, TerminalCapabilities caps, bool showHud = false)
    {
        var dl = builder.Build();
        var scheduler = new Andy.Tui.Core.FrameScheduler();
        var hud = new Andy.Tui.Observability.HudOverlay{ Enabled = showHud };
        scheduler.SetMetricsSink(hud);
        var pty = new StdoutPty();
        // Combine base + overlay for a single frame
        var overlayBuilder = new DisplayListBuilder();
        hud.Contribute(dl, overlayBuilder);
        var combined = Combine(dl, overlayBuilder.Build());
        await scheduler.RenderOnceAsync(combined, viewport, caps, pty, CancellationToken.None);
    }

    static DisplayList Combine(DisplayList a, DisplayList b)
    {
        var builder = new DisplayListBuilder();
        void Append(DisplayList dl)
        {
            foreach (var op in dl.Ops)
            {
                switch (op)
                {
                    case Rect r: builder.DrawRect(r); break;
                    case Border br: builder.DrawBorder(br); break;
                    case TextRun tr: builder.DrawText(tr); break;
                    case ClipPush cp: builder.PushClip(cp); break;
                    case LayerPush lp: builder.PushLayer(lp); break;
                    case Pop: builder.Pop(); break;
                }
            }
        }
        Append(a);
        Append(b);
        return builder.Build();
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
