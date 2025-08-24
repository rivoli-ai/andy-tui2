using System;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using DL = Andy.Tui.DisplayList;
using Andy.Tui.Examples;

namespace Andy.Tui.Examples.Demos;

public static class ButtonsInteractiveDemo
{
    static bool _btn1Active;
    static bool _btn2Active;
    static int _focusIndex;

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
            bool showHud = true;
            long animStart = Environment.TickCount64;
            _focusIndex = 0; _btn1Active = false; _btn2Active = false;
            while (running)
            {
                viewport = TerminalHelpers.PollResize(viewport, scheduler);
                while (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    if (key.Key == ConsoleKey.H) { showHud = !showHud; hud.Enabled = showHud; }
                    else if (key.Key == ConsoleKey.Tab) { _focusIndex = (_focusIndex + 1) % 2; }
                    else if (key.Key == ConsoleKey.LeftArrow) { if (_focusIndex > 0) _focusIndex--; }
                    else if (key.Key == ConsoleKey.RightArrow) { if (_focusIndex < 1) _focusIndex++; }
                    else if (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.Spacebar)
                    { if (_focusIndex == 0) _btn1Active = !_btn1Active; else _btn2Active = !_btn2Active; }
                    else if (key.Key == ConsoleKey.Escape) { running = false; break; }
                }
                var baseBuilder = new DL.DisplayListBuilder();
                baseBuilder.PushClip(new DL.ClipPush(0, 0, viewport.Width, viewport.Height));
                baseBuilder.DrawRect(new DL.Rect(0, 0, viewport.Width, viewport.Height, new DL.Rgb24(0, 0, 0)));
                var from = new DL.Rgb24(50, 150, 250);
                var to = new DL.Rgb24(250, 100, 50);
                var tcol = Andy.Tui.Animations.ColorTransitionApplier.Apply(new DL.TextRun(2, 1, $"Buttons — Tab/Arrows, Enter/Space; ESC back; h HUD", from, null, DL.CellAttrFlags.None), animStart, Environment.TickCount64, new Andy.Tui.Animations.TransitionColor(from, to, 2000));
                baseBuilder.DrawText(tcol);
                int panelX = 2; int panelY = 6; int panelW = Math.Min(40, Math.Max(24, viewport.Width - 4)); int panelH = 5;
                baseBuilder.DrawBorder(new DL.Border(panelX, panelY, panelW, panelH, "single", new DL.Rgb24(100, 100, 100)));
                var baseDl = baseBuilder.Build();
                var btn1 = new Andy.Tui.Widgets.Button("Button 1"); btn1.SetFocused(_focusIndex == 0); btn1.SetHovered(_focusIndex == 0); btn1.SetActive(_btn1Active);
                btn1.Render(new Andy.Tui.Layout.Rect(panelX + 2, panelY + 1, 14, 1), baseDl, baseBuilder);
                var btn2 = new Andy.Tui.Widgets.Button("Button 2"); btn2.SetFocused(_focusIndex == 1); btn2.SetHovered(_focusIndex == 1); btn2.SetActive(_btn2Active);
                btn2.Render(new Andy.Tui.Layout.Rect(panelX + 18, panelY + 1, 14, 1), baseDl, baseBuilder);
                baseBuilder.Pop();

                baseDl = baseBuilder.Build();
                var overlayBuilder = new DL.DisplayListBuilder();
                hud.ViewportCols = viewport.Width; hud.ViewportRows = viewport.Height;
                hud.Contribute(baseDl, overlayBuilder);
                var overlayDl = overlayBuilder.Build();
                var combined = Combine(baseDl, overlayDl);
                await scheduler.RenderOnceAsync(combined, viewport, caps, pty, CancellationToken.None);
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
