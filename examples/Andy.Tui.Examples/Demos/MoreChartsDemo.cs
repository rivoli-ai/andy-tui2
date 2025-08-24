using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Examples.Demos;

public static class MoreChartsDemo
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
            var hm = new Andy.Tui.Widgets.Heatmap(); hm.SetGrid(16);
            var bl = new Andy.Tui.Widgets.BulletChart(); bl.SetRange(0,100); bl.SetValue(65); bl.SetTarget(80);
            var g = new Andy.Tui.Widgets.Gauge(); g.SetRange(0,100); g.SetValue(45);
            var cs = new Andy.Tui.Widgets.Candlestick();
            var rnd = new Random(3);
            double price = 100;
            var candles = Enumerable.Range(0, 50).Select(_ => {
                double open = price;
                double high = open + rnd.NextDouble()*3;
                double low = open - rnd.NextDouble()*3;
                double close = low + rnd.NextDouble()*(high-low);
                price = close; return new Andy.Tui.Widgets.Candlestick.Candle(open, high, low, close);
            });
            cs.SetSeries(candles);
            hm.SetValues(Enumerable.Range(0, 16*8).Select(_ => rnd.NextDouble()));

            while (running)
            {
                viewport = Andy.Tui.Examples.TerminalHelpers.PollResize(viewport, scheduler);
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape) { running = false; break; }
                    if (k.Key == ConsoleKey.F2) hud.Enabled = !hud.Enabled;
                }

                var b = new DL.DisplayListBuilder();
                b.PushClip(new DL.ClipPush(0, 0, viewport.Width, viewport.Height));
                b.DrawRect(new DL.Rect(0, 0, viewport.Width, viewport.Height, new DL.Rgb24(0, 0, 0)));
                b.DrawText(new DL.TextRun(2, 1, "Heatmap / Bullet / Gauge / Candles — ESC back; F2 HUD", new DL.Rgb24(200,200,50), null, DL.CellAttrFlags.Bold));
                var baseDl = b.Build();

                var wb = new DL.DisplayListBuilder();
                int halfW = Math.Max(1, (viewport.Width - 6) / 2);
                int halfH = Math.Max(1, (viewport.Height - 5) / 2);
                hm.Render(new L.Rect(2, 3, halfW, halfH), baseDl, wb);
                bl.Render(new L.Rect(2 + halfW + 2, 3, halfW - 2, 1), baseDl, wb);
                g.Render(new L.Rect(2 + halfW + 2, 5, halfW - 2, 3), baseDl, wb);
                cs.Render(new L.Rect(2, 3 + halfH + 1, viewport.Width - 4, Math.Max(5, viewport.Height - (3 + halfH + 1) - 2)), baseDl, wb);

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
