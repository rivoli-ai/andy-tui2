using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Examples.Demos;

public static class ListViewDemo2
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
            var lv = new Andy.Tui.Widgets.ListView();
            lv.SetItems(Enumerable.Range(1, 200).Select(i => $"Item {i}"));
            string tip = "Up/Down move; Shift+Up/Down select range; Space toggle; PgUp/PgDn/Home/End; ESC back; F2 HUD";
            int anchor = -1;

            while (running)
            {
                viewport = Andy.Tui.Examples.TerminalHelpers.PollResize(viewport, scheduler);
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape) { running = false; break; }
                    if (k.Key == ConsoleKey.F2) hud.Enabled = !hud.Enabled;
                    if (k.Key == ConsoleKey.DownArrow) { lv.MoveCursor(1); if ((k.Modifiers & ConsoleModifiers.Shift) != 0) { if (anchor == -1) anchor = lv.GetCursor()-1; lv.SelectRange(anchor, lv.GetCursor()); } else { anchor = -1; } }
                    if (k.Key == ConsoleKey.UpArrow) { lv.MoveCursor(-1); if ((k.Modifiers & ConsoleModifiers.Shift) != 0) { if (anchor == -1) anchor = lv.GetCursor()+1; lv.SelectRange(anchor, lv.GetCursor()); } else { anchor = -1; } }
                    if (k.Key == ConsoleKey.Spacebar) lv.ToggleSelect();
                    if (k.Key == ConsoleKey.PageDown) lv.Page(1);
                    if (k.Key == ConsoleKey.PageUp) lv.Page(-1);
                    if (k.Key == ConsoleKey.Home) lv.Home();
                    if (k.Key == ConsoleKey.End) lv.End();
                    if (k.Key == ConsoleKey.C && (k.Modifiers & ConsoleModifiers.Control) != 0) lv.ClearSelection();
                }

                var b = new DL.DisplayListBuilder();
                b.PushClip(new DL.ClipPush(0, 0, viewport.Width, viewport.Height));
                b.DrawRect(new DL.Rect(0, 0, viewport.Width, viewport.Height, new DL.Rgb24(0, 0, 0)));
                b.DrawText(new DL.TextRun(2, 1, $"ListView (multi-select) — {tip}", new DL.Rgb24(200,200,50), null, DL.CellAttrFlags.Bold));
                var baseDl = b.Build();

                var wb = new DL.DisplayListBuilder();
                lv.Render(new L.Rect(2, 3, Math.Max(0, viewport.Width - 4), Math.Max(0, viewport.Height - 5)), baseDl, wb);

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
