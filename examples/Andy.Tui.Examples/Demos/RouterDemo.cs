using System;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Examples.Demos;

public static class RouterDemo
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
            var router = new Andy.Tui.Widgets.Router();
            router.SetBackground(new DL.Rgb24(0,0,0));
            router.SetRoute("home", (r,bd,b) => b.DrawText(new DL.TextRun((int)r.X+2, (int)r.Y+2, "Home View", new DL.Rgb24(220,220,220), null, DL.CellAttrFlags.Bold)));
            router.SetRoute("settings", (r,bd,b) => b.DrawText(new DL.TextRun((int)r.X+2, (int)r.Y+2, "Settings View", new DL.Rgb24(220,220,220), null, DL.CellAttrFlags.Bold)));
            router.SetRoute("about", (r,bd,b) => b.DrawText(new DL.TextRun((int)r.X+2, (int)r.Y+2, "About View", new DL.Rgb24(220,220,220), null, DL.CellAttrFlags.Bold)));
            router.NavigateTo("home");

            string status = "";
            while (running)
            {
                viewport = Andy.Tui.Examples.TerminalHelpers.PollResize(viewport, scheduler);
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape) { running = false; break; }
                    if (k.Key == ConsoleKey.F2) hud.Enabled = !hud.Enabled;
                    if (k.Key == ConsoleKey.H) { router.NavigateTo("home"); status = "Navigated: home"; }
                    if (k.Key == ConsoleKey.S) { router.NavigateTo("settings"); status = "Navigated: settings"; }
                    if (k.Key == ConsoleKey.A) { router.NavigateTo("about"); status = "Navigated: about"; }
                    if (k.Key == ConsoleKey.LeftArrow) { router.Back(); status = "Back"; }
                    if (k.Key == ConsoleKey.RightArrow) { router.Forward(); status = "Forward"; }
                }

                var b = new DL.DisplayListBuilder();
                b.PushClip(new DL.ClipPush(0, 0, viewport.Width, viewport.Height));
                b.DrawRect(new DL.Rect(0, 0, viewport.Width, viewport.Height, new DL.Rgb24(0, 0, 0)));
                b.DrawText(new DL.TextRun(2, 1, "Router — H/S/A switch views; Left/Right Back/Forward; ESC back; F2 HUD", new DL.Rgb24(200,200,50), null, DL.CellAttrFlags.Bold));
            b.DrawText(new DL.TextRun(2, 2, $"Current: {router.GetCurrent()}  History: [{string.Join(',', router.GetHistory())}]", new DL.Rgb24(200,200,200), null, DL.CellAttrFlags.None));
            if (!string.IsNullOrEmpty(status))
                b.DrawText(new DL.TextRun(2, 3, status, new DL.Rgb24(200,200,50), null, DL.CellAttrFlags.None));
            var baseDl = b.Build();

                var wb = new DL.DisplayListBuilder();
            router.Render(new L.Rect(0, 4, viewport.Width, viewport.Height - 4), baseDl, wb);

                var combined = Combine(baseDl, wb.Build());
                var overlay = new DL.DisplayListBuilder();
                hud.ViewportCols = viewport.Width; hud.ViewportRows = viewport.Height;
                hud.Contribute(combined, overlay);
                await scheduler.RenderOnceAsync(Combine(combined, overlay.Build()), viewport, caps, pty, CancellationToken.None);
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
