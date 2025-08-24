using System;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using DL = Andy.Tui.DisplayList;
using Andy.Tui.Examples;

namespace Andy.Tui.Examples.Demos;

public static class HelloDemo
{
    public static async Task Run((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        var scheduler = new Andy.Tui.Core.FrameScheduler();
        var hud = new Andy.Tui.Observability.HudOverlay { Enabled = true };
        scheduler.SetMetricsSink(hud);
        var pty = new StdoutPty();
        Console.Write("\u001b[?1049h\u001b[?25l\u001b[?7l");
        try
        {
            bool running = true;
            while (running)
            {
                viewport = TerminalHelpers.PollResize(viewport, scheduler);
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape || k.Key == ConsoleKey.Q) { running = false; break; }
                    if (k.Key == ConsoleKey.F2) hud.Enabled = !hud.Enabled;
                }

                var hello = new DL.DisplayListBuilder();
                hello.PushClip(new DL.ClipPush(0, 0, viewport.Width, viewport.Height));
                hello.DrawRect(new DL.Rect(0, 0, viewport.Width, viewport.Height, new DL.Rgb24(0, 0, 0)));
                hello.DrawBorder(new DL.Border(2, 1, 30, 5, "single", new DL.Rgb24(180, 180, 180)));
                hello.DrawText(new DL.TextRun(4, 3, "Hello, Andy.Tui!", new DL.Rgb24(200, 200, 50), null, DL.CellAttrFlags.Bold));
                hello.Pop();
                var baseDl = hello.Build();

                var footer = new DL.DisplayListBuilder();
                var msg = "ESC/Q to return";
                footer.PushClip(new DL.ClipPush(0, viewport.Height - 1, viewport.Width, 1));
                footer.DrawRect(new DL.Rect(0, viewport.Height - 1, viewport.Width, 1, new DL.Rgb24(15, 15, 15)));
                footer.DrawText(new DL.TextRun(2, viewport.Height - 1, msg, new DL.Rgb24(160, 160, 160), null, DL.CellAttrFlags.None));
                footer.Pop();

                var combined = Combine(baseDl, footer.Build());
                var overlay = new DL.DisplayListBuilder();
                hud.ViewportCols = viewport.Width; hud.ViewportRows = viewport.Height;
                hud.Contribute(combined, overlay);
                await scheduler.RenderOnceAsync(Combine(combined, overlay.Build()), viewport, caps, pty, CancellationToken.None);
                await Task.Delay(16);
            }
        }
        finally
        {
            Console.Write("\u001b[?7h\u001b[?25h\u001b[?1049l");
        }
    }

    private static DL.DisplayList Combine(DL.DisplayList a, DL.DisplayList b)
    {
        var builder = new DL.DisplayListBuilder();
        Append(a); Append(b);
        return builder.Build();
        void Append(DL.DisplayList dl)
        {
            foreach (var op in dl.Ops)
            {
                switch (op)
                {
                    case DL.Rect r: builder.DrawRect(r); break;
                    case DL.Border br: builder.DrawBorder(br); break;
                    case DL.TextRun tr: builder.DrawText(tr); break;
                    case DL.ClipPush cp: builder.PushClip(cp); break;
                    case DL.LayerPush lp: builder.PushLayer(lp); break;
                    case DL.Pop: builder.Pop(); break;
                }
            }
        }
    }
}
