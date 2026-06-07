using System;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using Andy.Tui.Examples;
using Andy.Tui.Style;
using Andy.Tui.Widgets;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Examples.Demos;

/// <summary>
/// Shows a wide range of widgets on one screen and cycles through every built-in
/// theme. Press T (or Space) to advance the theme, Q/Esc to return. Each frame the
/// widgets are reconstructed after <see cref="ThemeContext.Set"/>, so they pick up
/// the active theme's tokens at construction.
/// </summary>
public static class ThemesShowcaseDemo
{
    public static async Task Run((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        var scheduler = new Andy.Tui.Core.FrameScheduler();
        var hud = new Andy.Tui.Observability.HudOverlay { Enabled = false };
        scheduler.SetMetricsSink(hud);
        var pty = new StdoutPty();

        var themes = Themes.All; // built-ins + 32 popular ports
        int themeIndex = 0;

        Console.Write("[?1049h[?25l[?7l");
        try
        {
            bool running = true;
            while (running)
            {
                viewport = TerminalHelpers.PollResize(viewport, scheduler);
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape || k.Key == ConsoleKey.Q) { running = false; break; }
                    if (k.Key == ConsoleKey.T || k.Key == ConsoleKey.Spacebar || k.Key == ConsoleKey.RightArrow
                        || (k.Key == ConsoleKey.Tab && (k.Modifiers & ConsoleModifiers.Shift) == 0))
                        themeIndex = (themeIndex + 1) % themes.Count;
                    if (k.Key == ConsoleKey.LeftArrow || k.Key == ConsoleKey.Backspace
                        || (k.Key == ConsoleKey.Tab && (k.Modifiers & ConsoleModifiers.Shift) != 0))
                        themeIndex = (themeIndex - 1 + themes.Count) % themes.Count;
                    if (k.Key == ConsoleKey.F2) hud.Enabled = !hud.Enabled;
                }

                // Activate the theme BEFORE building widgets so they seed their colors from it.
                var theme = themes[themeIndex];
                ThemeContext.Set(theme);

                var baseDl = BuildBase(viewport, theme, themeIndex, themes.Count);

                var wb = new DL.DisplayListBuilder();
                BuildWidgets(wb, baseDl, viewport, theme);
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
            ThemeContext.Set(BuiltinThemes.Dark); // restore default for other demos
            Console.Write("[?7h[?25h[?1049l");
        }
    }

    private static DL.DisplayList BuildBase((int Width, int Height) vp, Theme theme, int idx, int count)
    {
        DL.Rgb24 Bg(ThemeToken t, byte r, byte g, byte b) => theme.GetRgb(t, new DL.Rgb24(r, g, b));
        var bg = Bg(ThemeToken.Background, 12, 12, 12);
        var surface = Bg(ThemeToken.Surface, 40, 40, 40);
        var fg = Bg(ThemeToken.Foreground, 220, 220, 220);
        var muted = Bg(ThemeToken.ForegroundMuted, 150, 150, 150);
        var accent = Bg(ThemeToken.Accent, 90, 120, 255);

        var b = new DL.DisplayListBuilder();
        b.PushClip(new DL.ClipPush(0, 0, vp.Width, vp.Height));
        b.DrawRect(new DL.Rect(0, 0, vp.Width, vp.Height, bg));

        // Header
        b.DrawRect(new DL.Rect(0, 0, vp.Width, 1, surface));
        b.DrawText(new DL.TextRun(2, 0, "Theme Showcase", accent, surface, DL.CellAttrFlags.Bold));
        var name = $"[{idx + 1}/{count}] {theme.Name}";
        b.DrawText(new DL.TextRun(18, 0, name, fg, surface, DL.CellAttrFlags.Bold));

        // Palette strip: one colored swatch per token.
        b.DrawText(new DL.TextRun(2, 2, "Palette:", muted, bg, DL.CellAttrFlags.None));
        int sx = 11, sy = 2;
        foreach (ThemeToken tok in Enum.GetValues(typeof(ThemeToken)))
        {
            if (sx + 3 > vp.Width - 2) { sx = 11; sy++; }
            var c = theme.Get(tok);
            var swatch = c.IsTransparent ? bg : new DL.Rgb24(c.R, c.G, c.B);
            b.DrawRect(new DL.Rect(sx, sy, 3, 1, swatch));
            sx += 4;
        }

        // Footer hint
        int fy = vp.Height - 1;
        b.DrawRect(new DL.Rect(0, fy, vp.Width, 1, surface));
        b.DrawText(new DL.TextRun(2, fy, "←/→ or T: change theme   F2: HUD   Q/Esc: back", muted, surface, DL.CellAttrFlags.None));

        b.Pop();
        return b.Build();
    }

    private static void BuildWidgets(DL.DisplayListBuilder wb, DL.DisplayList baseDl, (int Width, int Height) vp, Theme theme)
    {
        var bg = theme.GetRgb(ThemeToken.Background, new DL.Rgb24(12, 12, 12));
        var fg = theme.GetRgb(ThemeToken.Foreground, new DL.Rgb24(220, 220, 220));
        var accent = theme.GetRgb(ThemeToken.Accent, new DL.Rgb24(90, 120, 255));

        void SectionLabel(int x, int y, string text) =>
            wb.DrawText(new DL.TextRun(x, y, text, accent, bg, DL.CellAttrFlags.Bold));

        int top = 5;
        // Three responsive columns.
        int colW = Math.Max(24, (vp.Width - 8) / 3);
        int c1 = 2, c2 = c1 + colW + 2, c3 = c2 + colW + 2;

        // ----- Column 1: inputs -----
        int y = top;
        SectionLabel(c1, y, "Buttons");
        y += 1;
        var states = new[] { ("Normal", (Action<Button>)(_ => { })),
                             ("Hover",  b => b.SetHovered(true)),
                             ("Active", b => b.SetActive(true)),
                             ("Disabled", b => b.SetEnabled(false)) };
        int bw = Math.Max(9, (colW - 3) / 2);
        for (int i = 0; i < states.Length; i++)
        {
            var btn = new Button(states[i].Item1);
            states[i].Item2(btn);
            int bx = c1 + (i % 2) * (bw + 1);
            int by = y + (i / 2) * 3;
            btn.Render(new L.Rect(bx, by, bw, 3), baseDl, wb);
        }
        y += 7;

        SectionLabel(c1, y, "Checkbox / Toggle"); y += 1;
        new Checkbox("Accept terms", true).Render(new L.Rect(c1, y, colW, 1), baseDl, wb); y += 1;
        new Checkbox("Subscribe", false).Render(new L.Rect(c1, y, colW, 1), baseDl, wb); y += 2;
        new Toggle(true, "WiFi").Render(new L.Rect(c1, y, colW, 1), baseDl, wb); y += 1;
        new Toggle(false, "Bluetooth").Render(new L.Rect(c1, y, colW, 1), baseDl, wb); y += 2;

        SectionLabel(c1, y, "Radio Group"); y += 1;
        var radio = new RadioGroup();
        radio.SetItems(new[] { "Small", "Medium", "Large" });
        radio.SetSelectedIndex(1);
        radio.Render(new L.Rect(c1, y, colW, 3), baseDl, wb);

        // ----- Column 2: values & text -----
        y = top;
        SectionLabel(c2, y, "Text Input"); y += 1;
        var input = new TextInput();
        input.SetText("hello@andy.tui");
        input.SetFocused(true);
        input.Render(new L.Rect(c2, y, colW, 3), baseDl, wb); y += 4;

        SectionLabel(c2, y, "Slider"); y += 1;
        new Slider { Value = 0.62 }.Render(new L.Rect(c2, y, colW, 1), baseDl, wb); y += 2;

        SectionLabel(c2, y, "Progress"); y += 1;
        new ProgressBar { Value = 0.45 }.Render(new L.Rect(c2, y, colW, 1), baseDl, wb); y += 2;

        SectionLabel(c2, y, "Select"); y += 1;
        var sel = new Select();
        sel.SetItems(new[] { "Apple", "Banana", "Cherry" });
        sel.SetSelectedIndex(1);
        sel.Render(new L.Rect(c2, y, colW, 3), baseDl, wb); y += 4;

        SectionLabel(c2, y, "Status colors"); y += 1;
        var statuses = new[] { (ThemeToken.Success, "Success"), (ThemeToken.Warning, "Warning"),
                               (ThemeToken.Error, "Error"), (ThemeToken.Info, "Info") };
        foreach (var (tok, label) in statuses)
        {
            var col = theme.GetRgb(tok, fg);
            wb.DrawText(new DL.TextRun(c2, y, "● " + label, col, bg, DL.CellAttrFlags.Bold));
            y += 1;
        }

        // ----- Column 3: containers / lists -----
        if (c3 + 10 < vp.Width)
        {
            y = top;
            SectionLabel(c3, y, "List Box"); y += 1;
            var list = new ListBox();
            list.SetItems(new[] { "Inbox", "Sent", "Drafts", "Spam", "Trash", "Archive" });
            list.SetSelectedIndex(2);
            int listH = Math.Min(8, Math.Max(4, vp.Height - y - 10));
            list.Render(new L.Rect(c3, y, colW, listH), baseDl, wb);
            y += listH + 1;

            SectionLabel(c3, y, "Panel"); y += 1;
            var panel = new Panel();
            panel.SetTitle("Details");
            int panelH = Math.Max(4, vp.Height - y - 2);
            panel.Render(new L.Rect(c3, y, colW, panelH), baseDl, wb);
            new Label("Themed container.") { }.Render(new L.Rect(c3 + 2, y + 1, colW - 4, 1), baseDl, wb);
            new Label("Try T to cycle.").Render(new L.Rect(c3 + 2, y + 2, colW - 4, 1), baseDl, wb);
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
