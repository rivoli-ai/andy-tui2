using System;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Examples.Demos;

public static class PagerToastSpinnerDemo
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
            var pager = new Andy.Tui.Widgets.Pager();
            pager.SetTotalItems(250);
            pager.SetPageSize(25);
            pager.SetCurrentPage(1);
            var toast = new Andy.Tui.Widgets.Toast();
            var spinner = new Andy.Tui.Widgets.Spinner();
            bool running = true;
            while (running)
            {
                viewport = Andy.Tui.Examples.TerminalHelpers.PollResize(viewport, scheduler);
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape) { running = false; break; }
                    if (k.Key == ConsoleKey.F2) hud.Enabled = !hud.Enabled;
                    if (k.Key == ConsoleKey.RightArrow) { pager.Next(); spinner.Tick(); }
                    if (k.Key == ConsoleKey.LeftArrow) { pager.Prev(); spinner.Tick(); }
                    if (k.Key == ConsoleKey.Enter) { toast.Show($"Page {pager.GetCurrentPage()} selected", TimeSpan.FromSeconds(1.5)); }
                }

                var b = new DL.DisplayListBuilder();
                b.PushClip(new DL.ClipPush(0, 0, viewport.Width, viewport.Height));
                b.DrawRect(new DL.Rect(0, 0, viewport.Width, viewport.Height, new DL.Rgb24(0, 0, 0)));
                b.DrawText(new DL.TextRun(2, 1, "Pager/Toast/Spinner — Left/Right page, Enter toast; ESC back; F2 HUD", new DL.Rgb24(200, 200, 50), null, DL.CellAttrFlags.Bold));
                var baseDl = b.Build();
                var wb = new DL.DisplayListBuilder();

                // Pager centered
                var (pw, ph) = pager.Measure();
                int px = Math.Max(0, viewport.Width / 2 - pw / 2);
                int py = viewport.Height / 2;
                pager.Render(new L.Rect(px, py, pw, ph), baseDl, wb);

                // Spinner near title
                spinner.Render(new L.Rect(2, 2, 1, 1), baseDl, wb);

                // Toast at bottom center
                if (toast.IsVisible())
                {
                    var (tw, th) = toast.Measure();
                    int tx = Math.Max(0, viewport.Width / 2 - tw / 2);
                    int ty = Math.Max(0, viewport.Height - 2);
                    toast.Render(new L.Rect(tx, ty, tw, th), baseDl, wb);
                }

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
