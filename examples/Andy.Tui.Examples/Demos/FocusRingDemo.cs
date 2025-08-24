using System;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Examples.Demos;

public static class FocusRingDemo
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
            var ring = new Andy.Tui.Widgets.FocusRing();
            string[] values = { "", "", "", "" };
            int lastW = -1, lastH = -1;

            while (running)
            {
                viewport = Andy.Tui.Examples.TerminalHelpers.PollResize(viewport, scheduler);
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape) { running = false; break; }
                    if (k.Key == ConsoleKey.F2) hud.Enabled = !hud.Enabled;
                    if (k.Key == ConsoleKey.Tab)
                    {
                        if ((k.Modifiers & ConsoleModifiers.Shift) != 0) ring.Prev();
                        else ring.Next();
                    }
                    else if (k.Key == ConsoleKey.Enter)
                    {
                        // Type into focused field: simple prompt-like
                        int idx = ring.GetFocusedIndex();
                        values[idx] += "*"; // append a placeholder to visualize input
                    }
                    else if (k.Key == ConsoleKey.Backspace)
                    {
                        int idx = ring.GetFocusedIndex();
                        if (values[idx].Length > 0) values[idx] = values[idx].Substring(0, values[idx].Length - 1);
                    }
                }

                var b = new DL.DisplayListBuilder();
                b.PushClip(new DL.ClipPush(0, 0, viewport.Width, viewport.Height));
                b.DrawRect(new DL.Rect(0, 0, viewport.Width, viewport.Height, new DL.Rgb24(0, 0, 0)));
                b.DrawText(new DL.TextRun(2, 1, "Focus Ring — Tab/Shift+Tab to cycle; ESC back; F2 HUD", new DL.Rgb24(200,200,50), null, DL.CellAttrFlags.Bold));

                // Draw some sample focusable areas
                int w = Math.Max(10, viewport.Width / 4);
                int h = 3;
                var r1 = new L.Rect(4, 4, w, h);
                var r2 = new L.Rect(4 + w + 4, 4, w, h);
                var r3 = new L.Rect(4 + (w + 4)*2, 4, w, h);
                var r4 = new L.Rect(4, 8, w, h);

                var baseDl = b.Build();

                var wb = new DL.DisplayListBuilder();
                // Draw boxes
                wb.DrawBorder(new DL.Border((int)r1.X, (int)r1.Y, (int)r1.Width, (int)r1.Height, "single", new DL.Rgb24(100,100,100)));
                wb.DrawBorder(new DL.Border((int)r2.X, (int)r2.Y, (int)r2.Width, (int)r2.Height, "single", new DL.Rgb24(100,100,100)));
                wb.DrawBorder(new DL.Border((int)r3.X, (int)r3.Y, (int)r3.Width, (int)r3.Height, "single", new DL.Rgb24(100,100,100)));
                wb.DrawBorder(new DL.Border((int)r4.X, (int)r4.Y, (int)r4.Width, (int)r4.Height, "single", new DL.Rgb24(100,100,100)));
                wb.DrawText(new DL.TextRun((int)r1.X + 2, (int)r1.Y + 1, $"Field 1: {values[0]}", new DL.Rgb24(220,220,220), null, DL.CellAttrFlags.None));
                wb.DrawText(new DL.TextRun((int)r2.X + 2, (int)r2.Y + 1, $"Field 2: {values[1]}", new DL.Rgb24(220,220,220), null, DL.CellAttrFlags.None));
                wb.DrawText(new DL.TextRun((int)r3.X + 2, (int)r3.Y + 1, $"Field 3: {values[2]}", new DL.Rgb24(220,220,220), null, DL.CellAttrFlags.None));
                wb.DrawText(new DL.TextRun((int)r4.X + 2, (int)r4.Y + 1, $"Field 4: {values[3]}", new DL.Rgb24(220,220,220), null, DL.CellAttrFlags.None));

                // Only rebuild ring order on size changes so Tab cycles visibly
                if (viewport.Width != lastW || viewport.Height != lastH)
                {
                    ring.Clear();
                    ring.Add("f1", r1); ring.Add("f2", r2); ring.Add("f3", r3); ring.Add("f4", r4);
                    lastW = viewport.Width; lastH = viewport.Height;
                }
                ring.Render(baseDl, wb);

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
