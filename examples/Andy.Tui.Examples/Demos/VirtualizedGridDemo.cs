using System;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using DL = Andy.Tui.DisplayList;

namespace Andy.Tui.Examples.Demos;

public static class VirtualizedGridDemo
{
    public static async Task Run((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        var scheduler = new Andy.Tui.Core.FrameScheduler();
        var hud = new Andy.Tui.Observability.HudOverlay { Enabled = true };
        scheduler.SetMetricsSink(hud);
        var pty = new StdoutPty();
        Console.Write("\u001b[?1049h\u001b[?25l\u001b[?7l\u001b[?1000h\u001b[?1006h");
        try
        {
            bool running = true;
            int[] colWidths = new[] { 6, 10, 16, 12, 10 };
            string[] headers = new[] { "Row", "Alpha", "LoremIpsum", "Numbers", "Hex" };

            var grid = new Andy.Tui.Widgets.VirtualizedGrid();
            grid.SetColumnWidths(colWidths);
            grid.SetDimensions(100000, colWidths.Length);
            grid.SetCellTextProvider((row, col) =>
            {
                return col switch
                {
                    0 => row.ToString(),
                    1 => $"A{row % 1000:D3}",
                    2 => $"Lorem {row % 10000:D4}",
                    3 => $"{(row * 37) % 1000000:D6}",
                    _ => $"0x{(row * 2654435761 % int.MaxValue):X}"
                };
            });

            // Start with an active cell near top-left
            grid.SetActiveCell(0, 0);

            while (running)
            {
                viewport = Andy.Tui.Examples.TerminalHelpers.PollResize(viewport, scheduler);
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    // Mouse: SGR 1006 decoding for wheel
                    if (k.KeyChar == '\u001b' && TryReadSgrMouse(out int btn, out int mx, out int my, out bool isDown, out int wheelDelta))
                    {
                        if (wheelDelta != 0)
                        {
                            int page = Math.Max(1, viewport.Height - 4);
                            grid.AdjustScroll(-wheelDelta, page);
                        }
                        continue;
                    }
                    if (k.Key == ConsoleKey.Escape) { running = false; break; }
                    if (k.Key == ConsoleKey.F2) hud.Enabled = !hud.Enabled;
                    else if (k.Key == ConsoleKey.UpArrow) grid.MoveActiveCell(-1, 0, Math.Max(1, viewport.Height - 4));
                    else if (k.Key == ConsoleKey.DownArrow) grid.MoveActiveCell(1, 0, Math.Max(1, viewport.Height - 4));
                    else if (k.Key == ConsoleKey.LeftArrow) { grid.MoveActiveCell(0, -1, Math.Max(1, viewport.Height - 4)); grid.EnsureVisibleCols(Math.Max(1, viewport.Width - 4)); }
                    else if (k.Key == ConsoleKey.RightArrow) { grid.MoveActiveCell(0, 1, Math.Max(1, viewport.Height - 4)); grid.EnsureVisibleCols(Math.Max(1, viewport.Width - 4)); }
                    else if (k.Key == ConsoleKey.PageUp) grid.AdjustScroll(-(Math.Max(1, viewport.Height - 4)), Math.Max(1, viewport.Height - 4));
                    else if (k.Key == ConsoleKey.PageDown) grid.AdjustScroll((Math.Max(1, viewport.Height - 4)), Math.Max(1, viewport.Height - 4));
                    else if (k.Key == ConsoleKey.Home) grid.SetScrollRows(0);
                    else if (k.Key == ConsoleKey.End) grid.SetScrollRows(int.MaxValue);
                }

                // Base background
                var baseB = new DL.DisplayListBuilder();
                baseB.PushClip(new DL.ClipPush(0, 0, viewport.Width, viewport.Height));
                baseB.DrawRect(new DL.Rect(0, 0, viewport.Width, viewport.Height, new DL.Rgb24(0, 0, 0)));
                baseB.DrawText(new DL.TextRun(2, 1, "Virtualized Grid — Up/Down/PageUp/PageDown; Home/End; ESC back; F2 HUD", new DL.Rgb24(200, 200, 50), null, DL.CellAttrFlags.Bold));
                // Column headers
                int headerY = 2;
                int curX = 2;
                for (int i = 0; i < headers.Length && i < colWidths.Length; i++)
                {
                    string hdr = headers[i];
                    int w = colWidths[i];
                    baseB.DrawText(new DL.TextRun(curX, headerY, hdr.Length > w ? hdr.Substring(0, w) : hdr.PadRight(w), new DL.Rgb24(180, 180, 220), null, DL.CellAttrFlags.Bold));
                    curX += w + 1;
                }
                var baseDl = baseB.Build();

                // Render grid in content area
                var wb = new DL.DisplayListBuilder();
                int contentY = headerY + 1;
                grid.Render(new Andy.Tui.Layout.Rect(2, contentY, Math.Max(0, viewport.Width - 4), Math.Max(0, viewport.Height - contentY - 1)), baseDl, wb);

                // Compose
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
        // Wheel encoded in bit 6 and low bit up/down
        if ((b & 64) != 0)
        {
            wheelDelta = ((b & 1) == 0) ? 3 : -3; // up=+3, down=-3 for coarse steps
        }
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
        Append(a);
        Append(b);
        return builder.Build();
    }
}
