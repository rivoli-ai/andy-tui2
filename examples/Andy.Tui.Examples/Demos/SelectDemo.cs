using System;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using DL = Andy.Tui.DisplayList;

namespace Andy.Tui.Examples.Demos;

public static class SelectDemo
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
            var select = new Andy.Tui.Widgets.Select();
            select.SetItems(new[] { "Apple", "Banana", "Cherry", "Date", "Elderberry" });
            string status = string.Empty;

            while (running)
            {
                viewport = Andy.Tui.Examples.TerminalHelpers.PollResize(viewport, scheduler);
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape) { running = false; break; }
                    if (k.Key == ConsoleKey.F2) hud.Enabled = !hud.Enabled;
                    else if (k.Key == ConsoleKey.Enter || k.Key == ConsoleKey.Spacebar)
                    { if (!select.IsOpen()) select.ToggleOpen(); else { select.ConfirmSelection(); status = $"Selected: {select.GetSelectedText()}"; } }
                    else if (k.Key == ConsoleKey.UpArrow) { if (select.IsOpen()) select.MoveHighlight(-1); }
                    else if (k.Key == ConsoleKey.DownArrow) { if (select.IsOpen()) select.MoveHighlight(1); }
                    else if (k.Key == ConsoleKey.Escape && select.IsOpen()) { select.Cancel(); }
                }

                var baseB = new DL.DisplayListBuilder();
                baseB.PushClip(new DL.ClipPush(0, 0, viewport.Width, viewport.Height));
                baseB.DrawRect(new DL.Rect(0, 0, viewport.Width, viewport.Height, new DL.Rgb24(0, 0, 0)));
                baseB.DrawText(new DL.TextRun(2, 1, "Select — Enter/Space open; Up/Down navigate; Enter confirm; Esc cancel; ESC back; F2 HUD", new DL.Rgb24(200, 200, 50), null, DL.CellAttrFlags.Bold));
                if (!string.IsNullOrEmpty(status))
                {
                    Andy.Tui.Widgets.MenuHelpers.DrawStatusLine(baseB, 2, viewport.Width, status);
                }
                var baseDl = baseB.Build();

                var wb = new DL.DisplayListBuilder();
                int selX = 2; int selY = 4; int selW = Math.Max(18, select.MeasureClosedWidth());
                select.Render(new Andy.Tui.Layout.Rect(selX, selY, selW, 1), baseDl, wb);
                // Render popup as a separate layer with clamping, so it doesn't corrupt adjacent lines
                select.RenderPopup(selX, selY + 1, viewport.Width, viewport.Height, baseDl, wb);

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
        Append(a);
        Append(b);
        return builder.Build();
    }
}
