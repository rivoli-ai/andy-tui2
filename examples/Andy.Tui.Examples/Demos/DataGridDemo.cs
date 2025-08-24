using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Examples.Demos;

public static class DataGridDemo
{
    public static async Task Run((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        var scheduler = new Andy.Tui.Core.FrameScheduler(targetFps: 30);
        var hud = new Andy.Tui.Observability.HudOverlay { Enabled = true };
        scheduler.SetMetricsSink(hud);
        var pty = new StdoutPty();
        Console.Write("\u001b[?1049h\u001b[?25l\u001b[?7l\u001b[?1000h\u001b[?1006h");
        try
        {
            bool running = true;
            string[] headers = new[] { "ID", "Name", "Department", "Salary" };
            int[] widths = new[] { 6, 16, 16, 10 };
            int rows = 50000;
            var grid = new Andy.Tui.Widgets.DataGrid();
            grid.SetColumns(headers, widths);
            grid.SetRowCount(rows);
            grid.SetCellTextProvider((row, col) => col switch
            {
                0 => row.ToString(),
                1 => $"Employee {row % 10000:D4}",
                2 => new[] { "Sales", "Engineering", "HR", "Ops", "Finance" }[row % 5],
                3 => $"${(row * 137) % 200000:D6}",
                _ => string.Empty
            });
            grid.SetActiveCell(0, 0);

            while (running)
            {
                viewport = Andy.Tui.Examples.TerminalHelpers.PollResize(viewport, scheduler);
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.KeyChar == '\u001b')
                    {
                        if (TryReadSgrMouse(out int btn, out int mx, out int my, out bool isDown, out int wheelDelta))
                        {
                            if (wheelDelta != 0)
                            {
                                int page = Math.Max(1, viewport.Height - 4);
                                grid.AdjustScroll(-wheelDelta, page);
                            }
                            continue;
                        }
                        // Not a mouse sequence: treat as ESC key to exit
                        running = false; break;
                    }
                    if (k.Key == ConsoleKey.Escape) { running = false; break; }
                    if (k.Key == ConsoleKey.F2) hud.Enabled = !hud.Enabled;
                    else if (k.Key == ConsoleKey.UpArrow) grid.MoveActiveCell(-1, 0, Math.Max(1, viewport.Height - 4));
                    else if (k.Key == ConsoleKey.DownArrow) grid.MoveActiveCell(1, 0, Math.Max(1, viewport.Height - 4));
                    else if (k.Key == ConsoleKey.LeftArrow) grid.MoveActiveCell(0, -1, Math.Max(1, viewport.Height - 4));
                    else if (k.Key == ConsoleKey.RightArrow) grid.MoveActiveCell(0, 1, Math.Max(1, viewport.Height - 4));
                    else if (k.Key == ConsoleKey.PageUp) grid.AdjustScroll(-(Math.Max(1, viewport.Height - 4)), Math.Max(1, viewport.Height - 4));
                    else if (k.Key == ConsoleKey.PageDown) grid.AdjustScroll((Math.Max(1, viewport.Height - 4)), Math.Max(1, viewport.Height - 4));
                    else if (k.Key == ConsoleKey.Home) { grid.SetActiveCell(0, 0); grid.EnsureVisible(Math.Max(1, viewport.Height - 4)); }
                    else if (k.Key == ConsoleKey.End) grid.SetActiveCell(rows - 1, headers.Length - 1);
                }

                var baseB = new DL.DisplayListBuilder();
                baseB.PushClip(new DL.ClipPush(0, 0, viewport.Width, viewport.Height));
                baseB.DrawRect(new DL.Rect(0, 0, viewport.Width, viewport.Height, new DL.Rgb24(0, 0, 0)));
                baseB.DrawText(new DL.TextRun(2, 1, "Data Grid (virtualized) — arrows to move cell; PgUp/PgDn; ESC back; F2 HUD", new DL.Rgb24(200, 200, 50), null, DL.CellAttrFlags.Bold));
                var baseDl = baseB.Build();

                var wb = new DL.DisplayListBuilder();
                int contentY = 3;
                grid.Render(new L.Rect(2, contentY, Math.Max(0, viewport.Width - 4), Math.Max(0, viewport.Height - contentY - 1)), baseDl, wb);

                var combined = Combine(baseDl, wb.Build());
                var overlay = new DL.DisplayListBuilder();
                hud.ViewportCols = viewport.Width; hud.ViewportRows = viewport.Height;
                hud.Contribute(combined, overlay);
                await scheduler.RenderOnceAsync(Combine(combined, overlay.Build()), viewport, caps, pty, CancellationToken.None);
            }
        }
        finally
        {
            Console.Write("\u001b[?7h\u001b[?25h\u001b[?1006l\u001b[?1000l\u001b[?1049l");
        }
    }

    private static bool TryReadSgrMouse(out int button, out int x, out int y, out bool isDown, out int wheelDelta)
    {
        button = 0; x = 0; y = 0; isDown = false; wheelDelta = 0;
        if (!Console.KeyAvailable) return false;
        if (Console.In.Peek() != '[') return false; Console.In.Read();
        if (!Console.KeyAvailable || Console.In.Peek() != '<') return false; Console.In.Read();
        string rb = ReadIntToken(); if (rb.Length == 0) return false;
        if (!Console.KeyAvailable || Console.In.Peek() != ';') return false; Console.In.Read();
        string rx = ReadIntToken(); if (rx.Length == 0) return false;
        if (!Console.KeyAvailable || Console.In.Peek() != ';') return false; Console.In.Read();
        string ry = ReadIntToken(); if (ry.Length == 0) return false;
        if (!Console.KeyAvailable) return false; int final = Console.In.Read();
        if (final != 'M' && final != 'm') return false;
        if (!int.TryParse(rb, out int b) || !int.TryParse(rx, out int px) || !int.TryParse(ry, out int py)) return false;
        button = b & 3; isDown = final == 'M';
        x = Math.Max(0, px - 1); y = Math.Max(0, py - 1);
        if ((b & 64) != 0) wheelDelta = ((b & 1) == 0) ? 3 : -3;
        return true;
    }

    private static string ReadIntToken()
    {
        var sb = new System.Text.StringBuilder();
        while (Console.KeyAvailable)
        {
            int ch = Console.In.Peek();
            if (ch >= '0' && ch <= '9') sb.Append((char)Console.In.Read()); else break;
        }
        return sb.ToString();
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
