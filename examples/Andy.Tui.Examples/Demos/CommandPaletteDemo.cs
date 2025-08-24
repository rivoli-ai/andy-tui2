using System;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using DL = Andy.Tui.DisplayList;

namespace Andy.Tui.Examples.Demos;

public static class CommandPaletteDemo
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
            bool open = true;
            string status = string.Empty;
            var cp = new Andy.Tui.Widgets.CommandPalette();
            cp.SetCommands(new[] {
                "Open File",
                "Save",
                "Save All",
                "Close Folder",
                "Toggle Sidebar",
                "Toggle HUD",
                "Go to Symbol",
                "Run Tests",
                "Build Project"
            });

            while (running)
            {
                viewport = Andy.Tui.Examples.TerminalHelpers.PollResize(viewport, scheduler);
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape)
                    {
                        if (open) open = false; else { running = false; break; }
                    }
                    else if (!open && (k.Key == ConsoleKey.P && (k.Modifiers & ConsoleModifiers.Control) != 0)) { open = true; }
                    else if (open)
                    {
                        if (k.Key == ConsoleKey.UpArrow) cp.MoveSelection(-1);
                        else if (k.Key == ConsoleKey.DownArrow) cp.MoveSelection(1);
                        else if (k.Key == ConsoleKey.Backspace)
                        {
                            var q = cp.GetQuery();
                            if (q.Length > 0) cp.SetQuery(q[..^1]);
                        }
                        else if (k.Key == ConsoleKey.Enter)
                        {
                            var sel = cp.GetSelected();
                            if (!string.IsNullOrEmpty(sel))
                            {
                                // simple action: toggle HUD if selected
                                if (sel.Contains("HUD", StringComparison.OrdinalIgnoreCase)) hud.Enabled = !hud.Enabled;
                                status = $"Selected: {sel}";
                                open = false;
                            }
                        }
                        else if (!char.IsControl(k.KeyChar))
                        {
                            cp.SetQuery(cp.GetQuery() + k.KeyChar);
                        }
                    }
                }

                var baseB = new DL.DisplayListBuilder();
                baseB.PushClip(new DL.ClipPush(0, 0, viewport.Width, viewport.Height));
                baseB.DrawRect(new DL.Rect(0, 0, viewport.Width, viewport.Height, new DL.Rgb24(0, 0, 0)));
                baseB.DrawText(new DL.TextRun(2, 1, "Command Palette — Ctrl+P to open; ESC closes/back; type to filter; Up/Down; Enter to run; F2 HUD", new DL.Rgb24(200, 200, 50), null, DL.CellAttrFlags.Bold));
                if (!string.IsNullOrEmpty(status))
                {
                    Andy.Tui.Widgets.MenuHelpers.DrawStatusLine(baseB, 2, viewport.Width, status);
                }
                var baseDl = baseB.Build();

                var wb = new DL.DisplayListBuilder();
                if (open)
                {
                    cp.Render(new Andy.Tui.Layout.Rect(0, 0, viewport.Width, viewport.Height), baseDl, wb);
                }

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
