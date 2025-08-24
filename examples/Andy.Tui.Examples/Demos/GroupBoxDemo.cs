using System;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Examples.Demos;

public static class GroupBoxDemo
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

            var gb1 = new Andy.Tui.Widgets.GroupBox();
            gb1.SetTitle("Network Settings");
            gb1.SetContent((r, bd, b) =>
            {
                b.DrawText(new DL.TextRun((int)r.X, (int)r.Y, "Host: example.local", new DL.Rgb24(220,220,220), null, DL.CellAttrFlags.None));
                b.DrawText(new DL.TextRun((int)r.X, (int)r.Y+1, "Port: 8080", new DL.Rgb24(220,220,220), null, DL.CellAttrFlags.None));
            });

            var gb2 = new Andy.Tui.Widgets.GroupBox();
            gb2.SetTitle("Credentials");
            gb2.SetContent((r, bd, b) =>
            {
                b.DrawText(new DL.TextRun((int)r.X, (int)r.Y, "User: admin", new DL.Rgb24(220,220,220), null, DL.CellAttrFlags.None));
                b.DrawText(new DL.TextRun((int)r.X, (int)r.Y+1, "Password: ******", new DL.Rgb24(220,220,220), null, DL.CellAttrFlags.None));
            });

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
                b.DrawText(new DL.TextRun(2, 1, "GroupBox — ESC back; F2 HUD", new DL.Rgb24(200,200,50), null, DL.CellAttrFlags.Bold));
                var baseDl = b.Build();

                var wb = new DL.DisplayListBuilder();
                int margin = 2;
                int half = Math.Max(0, (viewport.Width - margin*3) / 2);
                gb1.Render(new L.Rect(margin, 3, half, Math.Max(0, viewport.Height - 5)), baseDl, wb);
                gb2.Render(new L.Rect(margin*2 + half, 3, half, Math.Max(0, viewport.Height - 5)), baseDl, wb);

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
