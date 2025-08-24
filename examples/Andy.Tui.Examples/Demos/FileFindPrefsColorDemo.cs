using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Examples.Demos;

public static class FileFindPrefsColorDemo
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
            var dialog = new Andy.Tui.Widgets.FileDialog(); dialog.SetDirectory(Directory.GetCurrentDirectory());
            var fr = new Andy.Tui.Widgets.FindReplacePanel(); fr.SetVisible(true); fr.SetText("foo","bar");
            var prefs = new Andy.Tui.Widgets.PreferencesPanel(); prefs.SetItems(new[]{("Theme","Dark"),("FPS","30")});
            var color = new Andy.Tui.Widgets.ColorChooser();

            while (running)
            {
                viewport = Andy.Tui.Examples.TerminalHelpers.PollResize(viewport, scheduler);
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape) { running = false; break; }
                    if (k.Key == ConsoleKey.F2) hud.Enabled = !hud.Enabled;
                    if (k.Key == ConsoleKey.DownArrow) dialog.MoveCursor(1, System.Math.Max(5, viewport.Height - 8));
                    if (k.Key == ConsoleKey.UpArrow) dialog.MoveCursor(-1, System.Math.Max(5, viewport.Height - 8));
                    if (k.Key == ConsoleKey.Enter) dialog.Enter();
                    if (k.Key == ConsoleKey.LeftArrow) color.Move(-1);
                    if (k.Key == ConsoleKey.RightArrow) color.Move(1);
                }

                var b = new DL.DisplayListBuilder();
                b.PushClip(new DL.ClipPush(0, 0, viewport.Width, viewport.Height));
                b.DrawRect(new DL.Rect(0, 0, viewport.Width, viewport.Height, new DL.Rgb24(0, 0, 0)));
                b.DrawText(new DL.TextRun(2, 1, "File/Find/Prefs/Color — Arrows navigate; Enter; ESC back; F2 HUD", new DL.Rgb24(200,200,50), null, DL.CellAttrFlags.Bold));
                var baseDl = b.Build();

                var wb = new DL.DisplayListBuilder();
                int halfW = System.Math.Max(1, (viewport.Width - 6) / 2);
                dialog.Render(new L.Rect(2, 3, halfW, System.Math.Max(5, viewport.Height - 8)), baseDl, wb);
                fr.Render(new L.Rect(2 + halfW + 1, 3, halfW - 1, 3), baseDl, wb);
                prefs.Render(new L.Rect(2 + halfW + 1, 7, halfW - 1, 5), baseDl, wb);
                color.Render(new L.Rect(2 + halfW + 1, 13, halfW - 1, 3), baseDl, wb);

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
