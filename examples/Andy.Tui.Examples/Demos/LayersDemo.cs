using System;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Examples.Demos;

public static class LayersDemo
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
            var layers = new Andy.Tui.Widgets.StackLayers();

            bool running = true;
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
                b.DrawText(new DL.TextRun(2, 1, "Layers — Top layer draws after base; ESC back; F2 HUD", new DL.Rgb24(200, 200, 50), null, DL.CellAttrFlags.Bold));
                var baseDl = b.Build();
                var wb = new DL.DisplayListBuilder();

                layers.Clear();
                // Base layer
                layers.AddLayer((bd, lb) =>
                {
                    // Blue base layer
                    lb.DrawRect(new DL.Rect(2, 3, Math.Max(10, viewport.Width - 4), Math.Max(5, viewport.Height - 6), new DL.Rgb24(20, 40, 80)));
                    lb.DrawText(new DL.TextRun(4, 5, "Base layer (blue)", new DL.Rgb24(220, 220, 220), null, DL.CellAttrFlags.None));
                    // Text that will be overlapped by the red layer
                    lb.DrawText(new DL.TextRun(12, 8, "This text is under the red layer", new DL.Rgb24(230, 230, 255), null, DL.CellAttrFlags.None));
                });
                // Top overlay layer
                layers.AddLayer((bd, lb) =>
                {
                    // Red middle layer overlapping blue and its text
                    lb.DrawRect(new DL.Rect(10, 7, Math.Max(10, viewport.Width / 2), 5, new DL.Rgb24(180, 60, 60)));
                    lb.DrawText(new DL.TextRun(12, 9, "Middle layer (red)", new DL.Rgb24(0, 0, 0), null, DL.CellAttrFlags.Bold));
                });
                // Third layer on top to show it overlaps both areas
                layers.AddLayer((bd, lb) =>
                {
                    // Bright yellow small banner crossing both previous layers
                    int tw = Math.Max(12, viewport.Width / 3);
                    lb.DrawRect(new DL.Rect(8, 6, tw, 3, new DL.Rgb24(240, 210, 60)));
                    lb.DrawText(new DL.TextRun(10, 7, "Top layer (yellow)", new DL.Rgb24(30, 30, 30), null, DL.CellAttrFlags.Bold));
                });

                layers.Render(new L.Rect(0, 0, viewport.Width, viewport.Height), baseDl, wb);

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
