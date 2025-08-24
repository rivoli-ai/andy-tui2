using System;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Examples.Demos;

public static class SplitterDemo
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
            var split = new Andy.Tui.Widgets.Splitter();
            split.SetOrientation(Andy.Tui.Widgets.SplitterOrientation.Vertical);
            split.SetFirstPane((rect, baseDl, b) =>
            {
                b.DrawRect(new DL.Rect((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height, new DL.Rgb24(20, 40, 80)));
                RenderWrappedLorem(b, rect, new DL.Rgb24(220, 220, 220), header: "Left pane (use Left/Right to move)");
            });
            split.SetSecondPane((rect, baseDl, b) =>
            {
                b.DrawRect(new DL.Rect((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height, new DL.Rgb24(40, 20, 20)));
                RenderWrappedLorem(b, rect, new DL.Rgb24(220, 220, 220), header: "Right pane");
            });

            static void RenderWrappedLorem(DL.DisplayListBuilder b, L.Rect rect, DL.Rgb24 fg, string header)
            {
                int x = (int)rect.X; int y = (int)rect.Y; int w = (int)rect.Width; int h = (int)rect.Height;
                int cx = x + 2; int cy = y + 1; int contentW = Math.Max(4, w - 4);
                b.DrawText(new DL.TextRun(cx, cy, header, fg, null, DL.CellAttrFlags.Bold));
                cy += 2;
                if (contentW <= 0 || cy >= y + h) return;
                string lorem = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. " +
                               "Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.";
                var words = lorem.Split(' ');
                string line = string.Empty;
                foreach (var word in words)
                {
                    string next = (line.Length == 0) ? word : line + " " + word;
                    if (next.Length > contentW)
                    {
                        b.DrawText(new DL.TextRun(cx, cy, line, fg, null, DL.CellAttrFlags.None));
                        cy += 1;
                        if (cy >= y + h - 1) break;
                        line = word;
                    }
                    else line = next;
                }
                if (cy < y + h - 1 && line.Length > 0)
                {
                    b.DrawText(new DL.TextRun(cx, cy, line, fg, null, DL.CellAttrFlags.None));
                }
            }

            while (running)
            {
                viewport = Andy.Tui.Examples.TerminalHelpers.PollResize(viewport, scheduler);
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape) { running = false; break; }
                    if (k.Key == ConsoleKey.F2) hud.Enabled = !hud.Enabled;
                    if (k.Key == ConsoleKey.LeftArrow) split.Adjust(-0.02);
                    if (k.Key == ConsoleKey.RightArrow) split.Adjust(0.02);
                    if (k.Key == ConsoleKey.UpArrow) { split.SetOrientation(Andy.Tui.Widgets.SplitterOrientation.Horizontal); }
                    if (k.Key == ConsoleKey.DownArrow) { split.SetOrientation(Andy.Tui.Widgets.SplitterOrientation.Vertical); }
                }

                var b = new DL.DisplayListBuilder();
                b.PushClip(new DL.ClipPush(0, 0, viewport.Width, viewport.Height));
                b.DrawRect(new DL.Rect(0, 0, viewport.Width, viewport.Height, new DL.Rgb24(0, 0, 0)));
                b.DrawText(new DL.TextRun(2, 1, "Splitter — Left/Right to move handle; Up: horizontal, Down: vertical; ESC back; F2 HUD", new DL.Rgb24(200, 200, 50), null, DL.CellAttrFlags.Bold));
                var baseDl = b.Build();

                var wb = new DL.DisplayListBuilder();
                split.Render(new L.Rect(2, 3, Math.Max(0, viewport.Width - 4), Math.Max(0, viewport.Height - 4)), baseDl, wb);

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
