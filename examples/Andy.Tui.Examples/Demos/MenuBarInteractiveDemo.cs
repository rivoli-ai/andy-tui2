using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using DL = Andy.Tui.DisplayList;
using Andy.Tui.Examples;
using System.Runtime.InteropServices;

namespace Andy.Tui.Examples.Demos;

public static class MenuBarInteractiveDemo
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
            var headers = new[] { "File", "Edit", "View" };
            int activeHeader = 0;
            bool menuOpen = false;
            int activeItem = 0;
            string status = ""; // persists after Enter

            var recentSub = new Andy.Tui.Widgets.Menu()
                .Add("Project A")
                .Add("Project B")
                .Add("Project C");
            var fileMenuObj = new Andy.Tui.Widgets.Menu()
                .Add("New", 'N')
                .Add("Open…", 'O', submenu: recentSub)
                .Add("Save", 'S')
                .Add("Exit", 'X');
            var editMenuObj = new Andy.Tui.Widgets.Menu()
                .Add("Undo", 'U').Add("Redo", 'R').Add("Cut", 'T').Add("Copy", 'C').Add("Paste", 'P');
            var viewMenuObj = new Andy.Tui.Widgets.Menu()
                .Add("Toggle HUD (F2)", 'H').Add("Zoom In", '+').Add("Zoom Out", '-');

            // Platform-specific accelerator modifier: Alt on Windows/Linux, Ctrl on macOS terminals
            bool isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            var accelMod = isMac ? ConsoleModifiers.Control : ConsoleModifiers.Alt;
            string accelModName = isMac ? "Ctrl" : "Alt";

            static bool TryGetAccelChar(ConsoleKeyInfo k, bool isMac, ConsoleModifiers accelMod, out char ch)
            {
                ch = '\0';
                if (isMac)
                {
                    // Never treat Enter/Tab/Backspace as accelerators
                    if (k.Key == ConsoleKey.Enter || k.Key == ConsoleKey.Tab || k.Key == ConsoleKey.Backspace)
                        return false;
                    // On macOS terminals, Ctrl+Letter often yields a control code (1..26)
                    if (k.KeyChar is >= (char)1 and <= (char)26)
                    {
                        ch = (char)('A' + (k.KeyChar - 1));
                        return true;
                    }
                    if ((k.Modifiers & accelMod) != 0 && !char.IsControl(k.KeyChar))
                    {
                        ch = k.KeyChar;
                        return true;
                    }
                    return false;
                }
                // Windows/Linux: Alt+letter should be reported as Alt modifier with printable char
                if ((k.Modifiers & accelMod) != 0 && !char.IsControl(k.KeyChar))
                {
                    ch = k.KeyChar;
                    return true;
                }
                return false;
            }

            var behavior = new Andy.Tui.Widgets.MenuBehaviorOptions { SubmenuOpenDelayMs = 300 };
            long hoverStartTicks = 0;
            int hoverItemIndex = -1;
            bool submenuOpen = false;
            int submenuIndex = -1;

            int lastPopupX = 0, lastPopupY = 0, lastPopupW = 0, lastPopupH = 0;
            int lastSubX = 0, lastSubY = 0, lastSubW = 0, lastSubH = 0;
            while (running)
            {
                viewport = TerminalHelpers.PollResize(viewport, scheduler);
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    // Consume SGR mouse events to handle header click and dismiss on outside click
                    if (k.KeyChar == '\u001b')
                    {
                        if (TryReadSgrMouse(out int btn, out int mx, out int my, out bool isDown))
                        {
                            if (isDown && btn == 0) // left click down
                            {
                                // Header click to open corresponding menu
                                if (my == 0)
                                {
                                    var clickMb = new Andy.Tui.Widgets.MenuBar()
                                        .Add(headers[0], new Andy.Tui.Widgets.Menu())
                                        .Add(headers[1], new Andy.Tui.Widgets.Menu())
                                        .Add(headers[2], new Andy.Tui.Widgets.Menu());
                                    var pos = clickMb.ComputeHeaderPositions(2, 4, viewport.Width);
                                    for (int i = 0; i < headers.Length && i < pos.Count; i++)
                                    {
                                        int hx = pos[i].X;
                                        int hw = headers[i].Length;
                                        if (mx >= hx && mx < hx + hw)
                                        {
                                            activeHeader = i; menuOpen = true; activeItem = 0; submenuOpen = false; break;
                                        }
                                    }
                                    continue;
                                }
                                bool insideMain = menuOpen && mx >= lastPopupX && mx < lastPopupX + lastPopupW && my >= lastPopupY && my < lastPopupY + lastPopupH;
                                bool insideSub = submenuOpen && mx >= lastSubX && mx < lastSubX + lastSubW && my >= lastSubY && my < lastSubY + lastSubH;
                                bool insideBar = my == 0; // menubar row
                                if (menuOpen && !(insideMain || insideSub || insideBar)) { menuOpen = false; submenuOpen = false; }
                            }
                            continue; // handled as mouse
                        }
                    }
                    if (k.Key == ConsoleKey.F2) { hud.Enabled = !hud.Enabled; }
                    else if (k.Key == ConsoleKey.Escape)
                    {
                        if (isMac && Console.KeyAvailable)
                        {
                            // Treat ESC + letter on macOS as Option/Alt chord instead of escape/back
                            var k2 = Console.ReadKey(true);
                            if (!char.IsControl(k2.KeyChar))
                            {
                                char chAlt = k2.KeyChar;
                                if (!menuOpen)
                                {
                                    char ch = char.ToUpperInvariant(chAlt);
                                    for (int i = 0; i < headers.Length; i++)
                                    {
                                        if (char.ToUpperInvariant(headers[i][0]) == ch) { activeHeader = i; break; }
                                    }
                                    menuOpen = true; activeItem = 0; // open immediately on header accel
                                }
                                else
                                {
                                    var src = activeHeader switch { 0 => fileMenuObj, 1 => editMenuObj, _ => viewMenuObj };
                                    int ix = src.IndexOfAccelerator(chAlt);
                                    if (ix < 0) ix = src.IndexOfFirstStartingWith(chAlt);
                                    if (ix >= 0) activeItem = ix;
                                }
                                continue; // consumed ESC+char
                            }
                        }
                        if (menuOpen) { menuOpen = false; submenuOpen = false; } else { running = false; break; }
                    }
                    else if (!menuOpen)
                    {
                        if (k.Key == ConsoleKey.LeftArrow) activeHeader = (activeHeader - 1 + headers.Length) % headers.Length;
                        else if (k.Key == ConsoleKey.RightArrow) activeHeader = (activeHeader + 1) % headers.Length;
                        else if (k.Key == ConsoleKey.Enter || k.Key == ConsoleKey.DownArrow)
                        { menuOpen = true; activeItem = 0; }
                        else if (TryGetAccelChar(k, isMac, accelMod, out var chRaw))
                        {
                            // Header accelerator/type-ahead by first letter
                            char ch = char.ToUpperInvariant(chRaw);
                            for (int i = 0; i < headers.Length; i++)
                            {
                                if (char.ToUpperInvariant(headers[i][0]) == ch) { activeHeader = i; break; }
                            }
                            // Accelerator chord should open the targeted menu (mac: Ctrl+<Letter>, win/linux: Alt+<Letter>)
                            menuOpen = true; activeItem = 0;
                        }
                    }
                    else
                    {
                        int itemCount = activeHeader switch { 0 => fileMenuObj.Items.Count, 1 => editMenuObj.Items.Count, _ => viewMenuObj.Items.Count };
                        if (k.Key == ConsoleKey.UpArrow) activeItem = (activeItem - 1 + itemCount) % itemCount;
                        else if (k.Key == ConsoleKey.DownArrow) activeItem = (activeItem + 1) % itemCount;
                        else if (k.Key == ConsoleKey.LeftArrow)
                        {
                            if (submenuOpen) { submenuOpen = false; submenuIndex = -1; }
                            else { activeHeader = (activeHeader - 1 + headers.Length) % headers.Length; activeItem = 0; }
                        }
                        else if (k.Key == ConsoleKey.RightArrow)
                        {
                            var src = activeHeader switch { 0 => fileMenuObj, 1 => editMenuObj, _ => viewMenuObj };
                            if (src.Items[activeItem].Submenu is not null) { submenuOpen = true; submenuIndex = activeItem; }
                            else { activeHeader = (activeHeader + 1) % headers.Length; activeItem = 0; }
                        }
                        else if (TryGetAccelChar(k, isMac, accelMod, out var ch2Raw))
                        {
                            // First, treat accel chord as header switch if it matches a header
                            char ch = char.ToUpperInvariant(ch2Raw);
                            int headerMatch = -1;
                            for (int i = 0; i < headers.Length; i++)
                            {
                                if (char.ToUpperInvariant(headers[i][0]) == ch) { headerMatch = i; break; }
                            }
                            if (headerMatch >= 0)
                            {
                                activeHeader = headerMatch; activeItem = 0; menuOpen = true;
                            }
                            else
                            {
                                // Otherwise, menu item accelerator or first-letter match within current menu
                                var src = activeHeader switch { 0 => fileMenuObj, 1 => editMenuObj, _ => viewMenuObj };
                                int ix = src.IndexOfAccelerator(ch2Raw);
                                if (ix < 0) ix = src.IndexOfFirstStartingWith(ch2Raw);
                                if (ix >= 0) activeItem = ix;
                            }
                        }
                        else if (k.Key == ConsoleKey.Enter || k.Key == ConsoleKey.Spacebar || k.KeyChar == '\r' || k.KeyChar == '\n')
                        {
                            string chosen;
                            var src = activeHeader switch { 0 => fileMenuObj, 1 => editMenuObj, _ => viewMenuObj };
                            if (submenuOpen && submenuIndex == activeItem && src.Items[activeItem].Submenu is not null)
                            {
                                // For simplicity choose the highlighted parent item when in submenu mode
                                chosen = src.Items[activeItem].Text;
                                submenuOpen = false; submenuIndex = -1;
                            }
                            else
                            {
                                chosen = src.Items[activeItem].Text;
                            }
                            // Use library helper to produce a standard selected-path string
                            var mbForHelper = new Andy.Tui.Widgets.MenuBar()
                                .Add(headers[0], fileMenuObj)
                                .Add(headers[1], editMenuObj)
                                .Add(headers[2], viewMenuObj);
                            var selectedPath = Andy.Tui.Widgets.MenuHelpers.GetSelectedItemPath(mbForHelper, activeHeader, activeItem);
                            status = selectedPath is null ? $"Selected: {headers[activeHeader]} › {chosen}" : $"Selected: {selectedPath}";
                            if (headers[activeHeader] == "File" && chosen == "Exit") { running = false; break; }
                            if (headers[activeHeader] == "View" && chosen.StartsWith("Toggle HUD")) { hud.Enabled = !hud.Enabled; }
                            menuOpen = false;
                        }
                    }
                    // Track hover start for submenu delay
                    if (menuOpen)
                    {
                        if (hoverItemIndex != activeItem) { hoverItemIndex = activeItem; hoverStartTicks = Environment.TickCount64; }
                        else
                        {
                            var src = activeHeader switch { 0 => fileMenuObj, 1 => editMenuObj, _ => viewMenuObj };
                            if (!submenuOpen && src.Items[activeItem].Submenu is not null && Environment.TickCount64 - hoverStartTicks > behavior.SubmenuOpenDelayMs)
                            { submenuOpen = true; submenuIndex = activeItem; }
                        }
                    }
                }

                var b = new DL.DisplayListBuilder();
                b.PushClip(new DL.ClipPush(0, 0, viewport.Width, viewport.Height));
                b.DrawRect(new DL.Rect(0, 0, viewport.Width, viewport.Height, new DL.Rgb24(0, 0, 0)));
                b.DrawText(new DL.TextRun(2, 1, $"MenuBar — Left/Right to choose, Enter/Down to open, Up/Down to navigate, {accelModName}+letter (mac: Ctrl) / Alt+letter (win/linux), Esc to close/back; F2 HUD", new DL.Rgb24(200, 200, 50), null, DL.CellAttrFlags.Bold));
                var baseDl = b.Build();

                var wb = new DL.DisplayListBuilder();
                int menubarY = 0;
                var menubar = new Andy.Tui.Widgets.MenuBar()
                    .Add(headers[0], new Andy.Tui.Widgets.Menu())
                    .Add(headers[1], new Andy.Tui.Widgets.Menu())
                    .Add(headers[2], new Andy.Tui.Widgets.Menu());
                menubar.Render(new Andy.Tui.Layout.Rect(0, menubarY, viewport.Width, 1), baseDl, wb, activeHeader);

                var headerPos = menubar.ComputeHeaderPositions(2, 4, viewport.Width);
                int[] headerX = headerPos.Select(p => p.X).ToArray();

                int underlineX = headerX[activeHeader];
                wb.DrawRect(new DL.Rect(underlineX - 1, menubarY, headers[activeHeader].Length + 2, 1, new DL.Rgb24(50, 50, 50)));
                wb.DrawText(new DL.TextRun(underlineX, menubarY, headers[activeHeader], new DL.Rgb24(255, 255, 200), new DL.Rgb24(50, 50, 50), DL.CellAttrFlags.Bold));

                int statusY = 3;
                if (menuOpen)
                {
                    var src = activeHeader switch { 0 => fileMenuObj, 1 => editMenuObj, _ => viewMenuObj };
                    var popup = new Andy.Tui.Widgets.MenuPopup();
                    popup.SetMenu(src);
                    popup.SetSelectedIndex(activeItem);
                    var (w, h) = popup.Measure();
                    int x = headerX[activeHeader];
                    int y = 1;
                    lastPopupX = x; lastPopupY = y; lastPopupW = w; lastPopupH = h;
                    // Draw any existing Selected line behind the popup (row 2), so dropdown occludes it
                    if (!string.IsNullOrEmpty(status))
                    {
                        if (2 < viewport.Height)
                            Andy.Tui.Widgets.MenuHelpers.DrawStatusLine(wb, 2, viewport.Width, status);
                    }
                    // Compute where to show the live Current line and draw it BEFORE the popup, so it's behind
                    statusY = Math.Min(viewport.Height - 1, y + h + 1);

                    // Live preview of current item (so it's visible before Enter)
                    var livePath = new Andy.Tui.Widgets.MenuBar()
                        .Add(headers[0], fileMenuObj)
                        .Add(headers[1], editMenuObj)
                        .Add(headers[2], viewMenuObj);
                    var currentText = Andy.Tui.Widgets.MenuHelpers.GetSelectedItemPath(livePath, activeHeader, activeItem);
                    if (!string.IsNullOrEmpty(currentText))
                    {
                        int barY = Math.Max(2, statusY);
                        if (barY < viewport.Height)
                            Andy.Tui.Widgets.MenuHelpers.DrawStatusLine(wb, barY, viewport.Width, $"Current: {currentText}  (Enter to select)", new DL.Rgb24(20, 20, 20), new DL.Rgb24(220, 220, 220));
                    }
                    // Render the popup and any submenu (submenu on top)
                    popup.Render(new Andy.Tui.Layout.Rect(x, y, w, h), baseDl, wb);
                    if (submenuOpen && submenuIndex == activeItem && src.Items[activeItem].Submenu is not null)
                    {
                        var sub = src.Items[activeItem].Submenu!;
                        var subPopup = new Andy.Tui.Widgets.MenuPopup();
                        subPopup.SetMenu(sub);
                        subPopup.SetSelectedIndex(0);
                        var (sw, sh) = subPopup.Measure();
                        var (sx, sy) = Andy.Tui.Widgets.MenuHelpers.ComputeSubmenuPosition(x, y, w, activeItem, sw, sh, viewport.Width, viewport.Height);
                        lastSubX = sx; lastSubY = sy; lastSubW = sw; lastSubH = sh;
                        subPopup.Render(new Andy.Tui.Layout.Rect(sx, sy, sw, sh), baseDl, wb);
                    }
                }

                if (!menuOpen && !string.IsNullOrEmpty(status))
                {
                    int barY = 2; // fixed line just below the instruction line
                    if (barY < viewport.Height)
                        Andy.Tui.Widgets.MenuHelpers.DrawStatusLine(wb, barY, viewport.Width, status);
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

    // Minimal SGR mouse reader using Console.In.Peek/Read when ESC detected
    private static bool TryReadSgrMouse(out int button, out int x, out int y, out bool isDown)
    {
        button = 0; x = 0; y = 0; isDown = false;
        // Expect sequence: ESC [ < b ; x ; y M|m
        if (!Console.KeyAvailable) return false;
        int next = Console.In.Peek();
        if (next != '[') return false; Console.In.Read();
        if (!Console.KeyAvailable || Console.In.Peek() != '<') return false; Console.In.Read();
        // read b
        string rb = ReadIntToken(); if (rb.Length == 0) return false;
        if (!Console.KeyAvailable || Console.In.Peek() != ';') return false; Console.In.Read();
        string rx = ReadIntToken(); if (rx.Length == 0) return false;
        if (!Console.KeyAvailable || Console.In.Peek() != ';') return false; Console.In.Read();
        string ry = ReadIntToken(); if (ry.Length == 0) return false;
        if (!Console.KeyAvailable) return false; int final = Console.In.Read();
        if (final != 'M' && final != 'm') return false;
        if (!int.TryParse(rb, out int b) || !int.TryParse(rx, out int px) || !int.TryParse(ry, out int py)) return false;
        // Map to left button index 0 for convenience
        button = b & 3;
        isDown = final == 'M';
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
