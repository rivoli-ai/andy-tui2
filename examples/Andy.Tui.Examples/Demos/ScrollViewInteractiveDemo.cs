using System;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using DL = Andy.Tui.DisplayList;
using Andy.Tui.Examples;

namespace Andy.Tui.Examples.Demos;

public static class ScrollViewInteractiveDemo
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
            int scrollY = 0;
            var content = string.Join("\n", System.Linq.Enumerable.Range(1, 200).Select(i => $"Line {i:D3}  The quick brown fox jumps over the lazy dog."));
            int viewW = Math.Max(30, Math.Min(80, viewport.Width - 4));
            int viewH = Math.Max(5, Math.Min(20, viewport.Height - 6));
            while (running)
            {
                viewport = TerminalHelpers.PollResize(viewport, scheduler);
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape) running = false;
                    if (k.Key == ConsoleKey.H) hud.Enabled = !hud.Enabled;
                    if (k.Key == ConsoleKey.UpArrow) scrollY = Math.Max(0, scrollY - 1);
                    if (k.Key == ConsoleKey.DownArrow) scrollY = Math.Max(0, scrollY + 1);
                    if (k.Key == ConsoleKey.PageUp) scrollY = Math.Max(0, scrollY - viewH);
                    if (k.Key == ConsoleKey.PageDown) scrollY = Math.Max(0, scrollY + viewH);
                    if (k.Key == ConsoleKey.Home) scrollY = 0;
                    if (k.Key == ConsoleKey.End) scrollY = int.MaxValue;
                }
                var b = new DL.DisplayListBuilder();
                b.PushClip(new DL.ClipPush(0, 0, viewport.Width, viewport.Height));
                b.DrawRect(new DL.Rect(0, 0, viewport.Width, viewport.Height, new DL.Rgb24(0, 0, 0)));
                b.DrawText(new DL.TextRun(2, 1, "ScrollView — Arrows/PageUp/PageDown/Home/End; ESC/Q back; h HUD", new DL.Rgb24(200, 200, 50), null, DL.CellAttrFlags.Bold));
                var baseDl = b.Build();
                var wb = new DL.DisplayListBuilder();
                var sv = new Andy.Tui.Widgets.ScrollView();
                sv.SetContent(content);
                sv.SetScrollY(scrollY);
                sv.Render(new Andy.Tui.Layout.Rect(2, 3, viewW, viewH), baseDl, wb);
                var combined = Combine(baseDl, wb.Build());
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
