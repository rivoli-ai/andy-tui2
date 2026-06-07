using System;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using Andy.Tui.Examples;
using DL = Andy.Tui.DisplayList;

namespace Andy.Tui.Examples.Demos;

/// <summary>
/// A gallery of the emoji glyph sets used by the cellular-automaton themes. Doubles as
/// a live demonstration of the compositor's wide-glyph (surrogate-pair / double-width)
/// rendering: every row is a single TextRun of double-width emoji and stays aligned.
/// A rainbow sweep animates the title and a highlight marches down the categories.
/// </summary>
public static class EmojiShowcaseDemo
{
    private static readonly (string Name, string Glyphs)[] Categories =
    {
        ("Forest", "🌱 🌿 🍀 🌾 🌳 🌲 🎋 🪴 🌵 🍂 🍃 🍄"),
        ("Plants", "🌷 🌸 🌹 🌺 🌻 🌼 💐 🪷 🌾 🪻 🌰 🥀"),
        ("Farm", "🐔 🐤 🐣 🐓 🐑 🐐 🐖 🐄 🐴 🐮 🦃 🐕 🐈 🐇"),
        ("Wild", "🦊 🐺 🦌 🐻 🦁 🐘 🦏 🐅 🐆 🦓 🦒 🦛 🐊 🦔"),
        ("Birds", "🐦 🐧 🦆 🦢 🦅 🦉 🦜 🐓 🦃 🦩 🪿 🐔"),
        ("Sea", "🐟 🐠 🐡 🦑 🐙 🦀 🐚 🐳 🐬 🦈 🐋 🦐 🦞"),
        ("Bugs", "🐛 🐜 🐝 🦋 🐞 🦗 🦟 🐌 🪲 🪳 🦂 🪰"),
        ("Food", "🍎 🍊 🍋 🍏 🍐 🍓 🫐 🍇 🍉 🍑 🍒 🥝 🥥"),
        ("Space", "🌑 🌒 🌓 🌔 🌕 🌖 🌗 🌘 🌝 🌚 🌟 🪐 🌌"),
        ("Construction", "🧱 🔩 🔧 🔨 🚧 🚜 🏭 🪨 🧰 🪚 🛞 🚂"),
        ("Computers", "💾 💻 💿 📀 🔌 🔋 📡 📟 📱 📺 🎮 🧮 🔭"),
        ("Faces", "😀 😃 😄 😁 😆 😅 😂 🙂 😉 😊 😍 😎 🤩 🥳"),
    };

    public static async Task Run((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        var scheduler = new Andy.Tui.Core.FrameScheduler();
        var hud = new Andy.Tui.Observability.HudOverlay { Enabled = false };
        scheduler.SetMetricsSink(hud);
        var pty = new StdoutPty();

        long startMs = Environment.TickCount64;

        Console.Write("\u001b[?1049h\u001b[?25l\u001b[?7l");
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
                    if (k.Key == ConsoleKey.F2) hud.Enabled = !hud.Enabled;
                }
                if (!running) break;

                int W = viewport.Width, H = viewport.Height;
                long t = Environment.TickCount64 - startMs;

                var b = new DL.DisplayListBuilder();
                b.PushClip(new DL.ClipPush(0, 0, W, H));
                b.DrawRect(new DL.Rect(0, 0, W, H, new DL.Rgb24(0, 0, 0)));

                // Rainbow-swept title.
                string title = "Emoji Showcase — wide-glyph rendering";
                double sweep = t / 30.0;
                for (int i = 0; i < title.Length && 2 + i < W; i++)
                    b.DrawText(new DL.TextRun(2 + i, 1, title[i].ToString(),
                        HsvToRgb((i * 9 + sweep) % 360.0, 0.7, 1.0), null, DL.CellAttrFlags.Bold));

                int highlight = (int)((t / 600) % Categories.Length);
                int labelW = 14;
                for (int ci = 0; ci < Categories.Length; ci++)
                {
                    int y = 3 + ci;
                    if (y >= H - 1) break;
                    var (name, glyphs) = Categories[ci];
                    bool hot = ci == highlight;
                    var labelColor = hot ? new DL.Rgb24(255, 240, 150) : new DL.Rgb24(150, 160, 180);
                    if (hot) b.DrawText(new DL.TextRun(0, y, "▶", new DL.Rgb24(255, 240, 150), null, DL.CellAttrFlags.Bold));
                    b.DrawText(new DL.TextRun(2, y, name, labelColor, null, hot ? DL.CellAttrFlags.Bold : DL.CellAttrFlags.None));
                    // Emoji render with their own colors; a single TextRun stays aligned.
                    b.DrawText(new DL.TextRun(2 + labelW, y, glyphs, new DL.Rgb24(230, 230, 230), null, DL.CellAttrFlags.None));
                }

                string footer = " ESC/Q return · F2 HUD · emoji themes also drive the Cellular Automaton (menu 67) ";
                if (footer.Length > W) footer = footer.Substring(0, W);
                b.DrawRect(new DL.Rect(0, H - 1, W, 1, new DL.Rgb24(12, 12, 18)));
                b.DrawText(new DL.TextRun(0, H - 1, footer, new DL.Rgb24(160, 160, 175), null, DL.CellAttrFlags.None));
                b.Pop();

                var baseDl = b.Build();
                var overlay = new DL.DisplayListBuilder();
                hud.ViewportCols = W; hud.ViewportRows = H;
                hud.Contribute(baseDl, overlay);
                await scheduler.RenderOnceAsync(Combine(baseDl, overlay.Build()), viewport, caps, pty, CancellationToken.None);
                await Task.Delay(33);
            }
        }
        finally
        {
            Console.Write("\u001b[?7h\u001b[?25h\u001b[?1049l");
        }
    }

    private static DL.Rgb24 HsvToRgb(double h, double s, double v)
    {
        h = ((h % 360.0) + 360.0) % 360.0;
        double c = v * s;
        double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
        double m = v - c;
        double r, g, b;
        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }
        return new DL.Rgb24(
            (byte)Math.Clamp((r + m) * 255.0, 0, 255),
            (byte)Math.Clamp((g + m) * 255.0, 0, 255),
            (byte)Math.Clamp((b + m) * 255.0, 0, 255));
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
