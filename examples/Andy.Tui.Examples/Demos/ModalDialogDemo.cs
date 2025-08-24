using System;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Examples.Demos;

public static class ModalDialogDemo
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
            var dialog = new Andy.Tui.Widgets.ModalDialog();
            string status = string.Empty;
            dialog.ShowConfirm("Confirm Action", "Proceed with operation?");

            while (running)
            {
                viewport = Andy.Tui.Examples.TerminalHelpers.PollResize(viewport, scheduler);
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (!dialog.IsVisible())
                    {
                        if (k.Key == ConsoleKey.Escape) { running = false; break; }
                        if (k.Key == ConsoleKey.F2) hud.Enabled = !hud.Enabled;
                        if (k.Key == ConsoleKey.C) dialog.ShowConfirm("Confirm Action", "Proceed with operation?");
                        if (k.Key == ConsoleKey.P) dialog.ShowPrompt("Input", "Type your name:", "Alice");
                        continue;
                    }
                    // Dialog is visible: focus trap
                    if (k.Key == ConsoleKey.Escape) { dialog.Cancel(); status = "Selected: Cancel"; }
                    else if (k.Key == ConsoleKey.Enter) { dialog.Confirm(); status = "Selected: OK"; }
                    else if (k.Key == ConsoleKey.Tab && (k.Modifiers & ConsoleModifiers.Shift) == 0) dialog.MoveFocusNext();
                    else if (k.Key == ConsoleKey.Tab && (k.Modifiers & ConsoleModifiers.Shift) != 0) dialog.MoveFocusPrev();
                    else if (k.Key == ConsoleKey.RightArrow) dialog.MoveFocusNext();
                    else if (k.Key == ConsoleKey.LeftArrow) dialog.MoveFocusPrev();
                    else if (k.Key == ConsoleKey.Backspace) dialog.Backspace();
                    else if (!char.IsControl(k.KeyChar)) dialog.TypeChar(k.KeyChar);
                }

                var b = new DL.DisplayListBuilder();
                b.PushClip(new DL.ClipPush(0, 0, viewport.Width, viewport.Height));
                b.DrawRect(new DL.Rect(0, 0, viewport.Width, viewport.Height, new DL.Rgb24(0, 0, 0)));
                b.DrawText(new DL.TextRun(2, 1, "Modal Dialog — C:Confirm, P:Prompt, Enter confirms, Esc cancels; ESC twice to exit; F2 HUD", new DL.Rgb24(200, 200, 50), null, DL.CellAttrFlags.Bold));
                // Status line (if any)
                if (!string.IsNullOrEmpty(status))
                {
                    b.PushClip(new DL.ClipPush(0, Math.Max(0, viewport.Height - 1), viewport.Width, 1));
                    b.DrawRect(new DL.Rect(0, Math.Max(0, viewport.Height - 1), viewport.Width, 1, new DL.Rgb24(15, 15, 15)));
                    b.DrawText(new DL.TextRun(2, Math.Max(0, viewport.Height - 1), status, new DL.Rgb24(160, 160, 160), null, DL.CellAttrFlags.None));
                    b.Pop();
                }
                var baseDl = b.Build();

                var wb = new DL.DisplayListBuilder();
                dialog.Render(new L.Rect(0, 0, viewport.Width, viewport.Height), baseDl, wb);

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
