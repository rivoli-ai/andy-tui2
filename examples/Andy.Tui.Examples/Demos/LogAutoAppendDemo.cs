using System;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using DL = Andy.Tui.DisplayList;
using Andy.Tui.Examples;

namespace Andy.Tui.Examples.Demos;

public static class LogAutoAppendDemo
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
            var lines = new System.Collections.Generic.List<string>(1024);
            string lorem = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Integer posuere erat a ante.";
            int viewW = Math.Max(30, Math.Min(100, viewport.Width - 4));
            int viewH = Math.Max(5, Math.Min(25, viewport.Height - 6));
            var rnd = new Random(1234);
            long lastTick = Environment.TickCount64;
            while (running)
            {
                viewport = TerminalHelpers.PollResize(viewport, scheduler);
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape) running = false;
                    if (k.Key == ConsoleKey.H) hud.Enabled = !hud.Enabled;
                }
                long now = Environment.TickCount64;
                int bursts = (int)Math.Max(1, (now - lastTick) / 20);
                lastTick = now;
                for (int b = 0; b < bursts && lines.Count < 1000; b++)
                {
                    int toAppend = 5;
                    for (int i = 0; i < toAppend && lines.Count < 1000; i++)
                    {
                        var noise = new string((char)('A' + rnd.Next(0, 26)), rnd.Next(3, 8));
                        lines.Add($"{lines.Count + 1:D4}  {lorem}  {noise}");
                    }
                }
                if (lines.Count >= 1000) running = false;

                var baseBuilder = new DL.DisplayListBuilder();
                baseBuilder.PushClip(new DL.ClipPush(0, 0, viewport.Width, viewport.Height));
                baseBuilder.DrawRect(new DL.Rect(0, 0, viewport.Width, viewport.Height, new DL.Rgb24(0, 0, 0)));
                baseBuilder.DrawText(new DL.TextRun(2, 1, $"Real-time Log — lines: {lines.Count}/1000; ESC/Q back; h HUD", new DL.Rgb24(200, 200, 50), null, DL.CellAttrFlags.Bold));
                var baseDl = baseBuilder.Build();
                var wb = new DL.DisplayListBuilder();
                var sv = new Andy.Tui.Widgets.ScrollView();
                sv.SetContent(string.Join("\n", lines));
                int scrollY = Math.Max(0, lines.Count - viewH);
                sv.SetScrollY(scrollY);
                sv.Render(new Andy.Tui.Layout.Rect(2, 3, viewW, viewH), baseDl, wb);
                var combined = Combine(baseDl, wb.Build());
                var overlay = new DL.DisplayListBuilder();
                hud.ViewportCols = viewport.Width; hud.ViewportRows = viewport.Height;
                hud.Contribute(combined, overlay);
                await scheduler.RenderOnceAsync(Combine(combined, overlay.Build()), viewport, caps, pty, CancellationToken.None);
                await Task.Delay(20);
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
