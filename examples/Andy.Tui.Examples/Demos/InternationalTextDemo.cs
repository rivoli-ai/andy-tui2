using System;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using DL = Andy.Tui.DisplayList;
using Andy.Tui.Examples;

namespace Andy.Tui.Examples.Demos;

public static class InternationalTextDemo
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
            while (running)
            {
                // Resize poll
                viewport = Andy.Tui.Examples.TerminalHelpers.PollResize(viewport, scheduler);
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape || k.Key == ConsoleKey.Q) { running = false; break; }
                    if (k.Key == ConsoleKey.F2) hud.Enabled = !hud.Enabled;
                }

                var b = new DL.DisplayListBuilder();
                b.PushClip(new DL.ClipPush(0, 0, viewport.Width, viewport.Height));
                b.DrawRect(new DL.Rect(0, 0, viewport.Width, viewport.Height, new DL.Rgb24(0, 0, 0)));
                int y = 1;
                b.DrawText(new DL.TextRun(2, y++, "International text demo — ESC/Q back; F2 HUD", new DL.Rgb24(200, 200, 50), null, DL.CellAttrFlags.Bold));
                y++;
                // Samples (note: RTL shaping is not applied; this is a rendering demo)
                b.DrawText(new DL.TextRun(2, y++, "Japanese: 日本語のテキスト", new DL.Rgb24(220, 220, 220), null, DL.CellAttrFlags.None));
                b.DrawText(new DL.TextRun(2, y++, "Chinese: 中文示例文本", new DL.Rgb24(220, 220, 220), null, DL.CellAttrFlags.None));
                b.DrawText(new DL.TextRun(2, y++, "Korean: 한국어 예시 문장", new DL.Rgb24(220, 220, 220), null, DL.CellAttrFlags.None));
                b.DrawText(new DL.TextRun(2, y++, "Hindi: हिन्दी उदाहरण पाठ", new DL.Rgb24(220, 220, 220), null, DL.CellAttrFlags.None));
                b.DrawText(new DL.TextRun(2, y++, "Arabic (unshaped LTR demo): العربية تجربة نص", new DL.Rgb24(220, 220, 220), null, DL.CellAttrFlags.None));
                b.DrawText(new DL.TextRun(2, y++, "Bengali: বাংলা উদাহরণ লেখা", new DL.Rgb24(220, 220, 220), null, DL.CellAttrFlags.None));
                b.DrawText(new DL.TextRun(2, y++, "Persian (unshaped LTR demo): فارسی نمونه متن", new DL.Rgb24(220, 220, 220), null, DL.CellAttrFlags.None));
                b.DrawText(new DL.TextRun(2, y++, "Russian: Пример текста", new DL.Rgb24(220, 220, 220), null, DL.CellAttrFlags.None));
                b.DrawText(new DL.TextRun(2, y++, "Emoji: 👨‍👩‍👧‍👦  🧑🏽‍💻  🚀✨", new DL.Rgb24(220, 220, 220), null, DL.CellAttrFlags.None));
                b.Pop();

                var baseDl = b.Build();
                var overlay = new DL.DisplayListBuilder();
                hud.ViewportCols = viewport.Width; hud.ViewportRows = viewport.Height;
                hud.Contribute(baseDl, overlay);
                await scheduler.RenderOnceAsync(DemosCombine(baseDl, overlay.Build()), viewport, caps, pty, CancellationToken.None);
            }
        }
        finally
        {
            Console.Write("\u001b[?7h\u001b[?25h\u001b[?1049l");
        }
    }

    private static DL.DisplayList DemosCombine(DL.DisplayList a, DL.DisplayList b)
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
