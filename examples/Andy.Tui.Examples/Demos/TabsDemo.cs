using System;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Examples.Demos;

public static class TabsDemo
{
    public static async Task Run((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        var scheduler = new Andy.Tui.Core.FrameScheduler(targetFps: 30);
        var hud = new Andy.Tui.Observability.HudOverlay { Enabled = true };
        scheduler.SetMetricsSink(hud);
        var pty = new StdoutPty();
        Console.Write("\u001b[?1049h\u001b[?25l\u001b[?7l");
        try
        {
            bool running = true;
            bool inputArmed = false;
            var tabs = new Andy.Tui.Widgets.Tabs();
            tabs.SetTabs(new[] { "Home", "Logs", "Settings" });
            tabs.SetContentRenderer((index, rect, baseDl, b) =>
            {
                var title = index switch { 0 => "Home", 1 => "Logs", 2 => "Settings", _ => "" };
                b.DrawText(new DL.TextRun((int)rect.X + 1, (int)rect.Y + 1, $"{title} content", new DL.Rgb24(220, 220, 220), null, DL.CellAttrFlags.None));
            });

            // Clear any residual keypresses from main menu selection
            while (Console.KeyAvailable) Console.ReadKey(true);

            while (running)
            {
                viewport = Andy.Tui.Examples.TerminalHelpers.PollResize(viewport, scheduler);
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape)
                    {
                        if (!inputArmed) continue; // ignore stray ESC immediately on entry
                        running = false; break;
                    }
                    if (k.Key == ConsoleKey.F2) hud.Enabled = !hud.Enabled;
                    if (k.Key == ConsoleKey.LeftArrow) tabs.Move(-1);
                    if (k.Key == ConsoleKey.RightArrow) tabs.Move(1);
                    if (k.Key >= ConsoleKey.D1 && k.Key <= ConsoleKey.D9) tabs.SetActive((int)(k.Key - ConsoleKey.D1));
                }

                var b = new DL.DisplayListBuilder();
                b.PushClip(new DL.ClipPush(0, 0, viewport.Width, viewport.Height));
                b.DrawRect(new DL.Rect(0, 0, viewport.Width, viewport.Height, new DL.Rgb24(0, 0, 0)));
                b.DrawText(new DL.TextRun(2, 1, "Tabs — Left/Right to switch; 1-9 to jump; ESC back; F2 HUD", new DL.Rgb24(200, 200, 50), null, DL.CellAttrFlags.Bold));
                var baseDl = b.Build();

                var wb = new DL.DisplayListBuilder();
                tabs.Render(new L.Rect(2, 3, Math.Max(0, viewport.Width - 4), Math.Max(0, viewport.Height - 4)), baseDl, wb);

                var combined = Combine(baseDl, wb.Build());
                var overlay = new DL.DisplayListBuilder();
                hud.ViewportCols = viewport.Width; hud.ViewportRows = viewport.Height;
                hud.Contribute(combined, overlay);
                await scheduler.RenderOnceAsync(Combine(combined, overlay.Build()), viewport, caps, pty, CancellationToken.None);
                inputArmed = true;
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
        Append(a); Append(b);
        return builder.Build();
    }
}
