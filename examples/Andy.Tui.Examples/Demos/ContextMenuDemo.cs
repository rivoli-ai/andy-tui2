using System;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using DL = Andy.Tui.DisplayList;

namespace Andy.Tui.Examples.Demos;

public static class ContextMenuDemo
{
    public static async Task Run((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        var scheduler = new Andy.Tui.Core.FrameScheduler(targetFps: 30);
        var hud = new Andy.Tui.Observability.HudOverlay { Enabled = true };
        scheduler.SetMetricsSink(hud);
        var pty = new StdoutPty();
        Console.Write("\u001b[?1049h\u001b[?25l\u001b[?7l\u001b[?1000h\u001b[?1006h");
        Console.TreatControlCAsInput = true; // avoid terminating demo on Ctrl+C
        try
        {
            bool running = true;
            int anchorX = 10, anchorY = 5;
            var ctx = new Andy.Tui.Widgets.ContextMenu();
            var menu = new Andy.Tui.Widgets.Menu()
                .Add("Copy", 'C')
                .Add("Paste", 'P')
                .Add("Rename", 'R')
                .Add("Delete", 'D');
            ctx.SetMenu(menu);
            int selectedIndex = 0;
            bool open = false;
            string status = string.Empty;

            int lastPopupX = 0, lastPopupY = 0, lastPopupW = 0, lastPopupH = 0;
            while (running)
            {
                viewport = Andy.Tui.Examples.TerminalHelpers.PollResize(viewport, scheduler);
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    // Handle SGR mouse: right-click to open at cursor, left-click outside to dismiss
                    if (k.KeyChar == '\u001b')
                    {
                        if (TryReadSgrMouse(out int btn, out int mx, out int my, out bool isDown))
                        {
                            if (isDown)
                            {
                                if (btn == 2) // right button: open at mouse
                                {
                                    anchorX = mx; anchorY = my;
                                    open = true;
                                }
                                else if (btn == 0) // left button
                                {
                                    bool inside = open && mx >= lastPopupX && mx < lastPopupX + lastPopupW && my >= lastPopupY && my < lastPopupY + lastPopupH;
                                    if (open)
                                    {
                                        if (!inside) { open = false; }
                                        else
                                        {
                                            // click inside selects row
                                            int relY = my - (lastPopupY + 1);
                                            if (relY >= 0 && relY < menu.Items.Count) selectedIndex = relY;
                                        }
                                    }
                                }
                            }
                            continue; // consumed
                        }
                    }
                    if (k.Key == ConsoleKey.Escape) { if (open) { open = false; } else { running = false; break; } }
                    else if (k.Key == ConsoleKey.F2) hud.Enabled = !hud.Enabled;
                    // Triggers: Shift+F10 or Apps/Menu key are common, use both + Ctrl+C fallback for demo
                    else if ((k.Key == ConsoleKey.F10 && k.Modifiers.HasFlag(ConsoleModifiers.Shift)) || k.Key == ConsoleKey.Applications)
                    { open = !open; }
                    else if (!open && (k.Key == ConsoleKey.Enter || k.Key == ConsoleKey.Spacebar)) { open = true; }
                    else if (k.Key == ConsoleKey.C && k.Modifiers.HasFlag(ConsoleModifiers.Control)) { open = !open; }
                    else if (k.Key == ConsoleKey.UpArrow && open) selectedIndex = Math.Max(0, selectedIndex - 1);
                    else if (k.Key == ConsoleKey.DownArrow && open) selectedIndex = Math.Min(menu.Items.Count - 1, selectedIndex + 1);
                    else if ((k.Key == ConsoleKey.Enter || k.Key == ConsoleKey.Spacebar) && open)
                    { status = $"Selected: {menu.Items[selectedIndex].Text}"; open = false; }
                }

                var baseB = new DL.DisplayListBuilder();
                baseB.PushClip(new DL.ClipPush(0, 0, viewport.Width, viewport.Height));
                baseB.DrawRect(new DL.Rect(0, 0, viewport.Width, viewport.Height, new DL.Rgb24(0, 0, 0)));
                // Minimal header only; avoid any fancy graphics to blend with terminal backgrounds
                baseB.DrawText(new DL.TextRun(2, 1, "Context Menu — Right-click/Shift+F10/Apps; Up/Down; Enter selects; ESC closes", new DL.Rgb24(180, 180, 180), null, DL.CellAttrFlags.None));
                baseB.DrawText(new DL.TextRun(anchorX, anchorY, "Right-click target", new DL.Rgb24(180, 220, 200), null, DL.CellAttrFlags.Bold));
                if (!string.IsNullOrEmpty(status))
                {
                    Andy.Tui.Widgets.MenuHelpers.DrawStatusLine(baseB, 2, viewport.Width, status);
                }
                var baseDl = baseB.Build();

                var wb = new DL.DisplayListBuilder();
                if (open)
                {
                    ctx.SetSelectedIndex(selectedIndex);
                    var (w, h) = ctx.Measure();
                    var (x, y) = Andy.Tui.Widgets.MenuHelpers.ComputePopupPosition(anchorX, anchorY + 1, w, h, viewport.Width, viewport.Height);
                    lastPopupX = x; lastPopupY = y; lastPopupW = w; lastPopupH = h;
                    ctx.Render(new Andy.Tui.Layout.Rect(x, y, w, h), baseDl, wb);
                }

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
    // Minimal helpers copied from menu demo to read SGR mouse tokens
    private static bool TryReadSgrMouse(out int button, out int x, out int y, out bool isDown)
    {
        button = 0; x = 0; y = 0; isDown = false;
        if (!Console.KeyAvailable) return false;
        int next = Console.In.Peek();
        if (next != '[') return false; Console.In.Read();
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
        return true;
    }

    private static string ReadIntToken()
    {
        var sb = new System.Text.StringBuilder();
        while (Console.KeyAvailable)
        {
            int ch = Console.In.Peek();
            if (ch >= '0' && ch <= '9') { sb.Append((char)Console.In.Read()); }
            else break;
        }
        return sb.ToString();
    }
}
