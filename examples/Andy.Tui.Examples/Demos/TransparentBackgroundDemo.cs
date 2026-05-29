using System;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using DL = Andy.Tui.DisplayList;
using Andy.Tui.Examples;

namespace Andy.Tui.Examples.Demos;

/// <summary>
/// Demonstrates transparent backgrounds. The whole frame is left transparent
/// (no root fill), so the terminal's own background — including a translucent
/// terminal window or a custom theme color — shows through. Only the bordered
/// panel paints an explicit opaque fill, to contrast against the transparency.
///
/// Transparency is expressed as a <c>null</c> background: <see cref="DL.Rect.Fill"/>
/// = <c>null</c> paints nothing, and <see cref="DL.TextRun.Bg"/> = <c>null</c>
/// keeps whatever is underneath. The encoder turns a null background into the
/// ANSI default-background reset (ESC[49m) instead of an explicit RGB color.
///
/// Tip: set your terminal window to a translucent background (e.g. iTerm2 /
/// kitty / Windows Terminal opacity &lt; 100%) to see the effect most clearly.
/// </summary>
public static class TransparentBackgroundDemo
{
    public static async Task Run((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        var scheduler = new Andy.Tui.Core.FrameScheduler();
        var hud = new Andy.Tui.Observability.HudOverlay { Enabled = true };
        scheduler.SetMetricsSink(hud);
        var pty = new StdoutPty();
        Console.Write("[?1049h[?25l[?7l");
        try
        {
            bool transparent = true;
            bool running = true;
            while (running)
            {
                viewport = TerminalHelpers.PollResize(viewport, scheduler);
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape || k.Key == ConsoleKey.Q) { running = false; break; }
                    if (k.Key == ConsoleKey.F2) hud.Enabled = !hud.Enabled;
                    // Space toggles between a transparent root and an opaque dark fill,
                    // so you can directly compare the two.
                    if (k.Key == ConsoleKey.Spacebar) { transparent = !transparent; scheduler.SetForceFullClear(true); }
                }

                var b = new DL.DisplayListBuilder();
                b.PushClip(new DL.ClipPush(0, 0, viewport.Width, viewport.Height));

                // The root background is expressed as a *style* color and converted to
                // a render color via the Style->render bridge. A transparent style color
                // (alpha 0) becomes a null fill, so the terminal shows through.
                var rootStyleColor = transparent
                    ? Andy.Tui.Style.RgbaColor.Transparent
                    : new Andy.Tui.Style.RgbaColor(20, 20, 28, 255);
                DL.Rgb24? rootFill = rootStyleColor.ToRgb24();
                b.DrawRect(new DL.Rect(0, 0, viewport.Width, viewport.Height, rootFill));

                // An opaque panel that always paints, to contrast with the background.
                int pw = Math.Min(46, viewport.Width - 4);
                b.DrawRect(new DL.Rect(2, 2, pw, 7, new DL.Rgb24(40, 44, 60)));
                b.DrawBorder(new DL.Border(2, 2, pw, 7, "single", new DL.Rgb24(120, 160, 220)));
                b.DrawText(new DL.TextRun(4, 3, "Opaque panel (explicit fill)", new DL.Rgb24(230, 230, 240), null, DL.CellAttrFlags.Bold));
                b.DrawText(new DL.TextRun(4, 5, "Background is: " + (transparent ? "TRANSPARENT" : "opaque (20,20,28)"),
                    new DL.Rgb24(180, 220, 160), null, DL.CellAttrFlags.None));
                b.DrawText(new DL.TextRun(4, 6, "Text here has a null bg (see-through)", new DL.Rgb24(200, 200, 120), null, DL.CellAttrFlags.None));
                b.DrawText(new DL.TextRun(4, 7, "Press SPACE to toggle, ESC/Q to return", new DL.Rgb24(150, 150, 160), null, DL.CellAttrFlags.None));

                // Text drawn directly on the (transparent) root — no panel behind it.
                b.DrawText(new DL.TextRun(2, 10, "↑ This text sits on the transparent root background.",
                    new DL.Rgb24(220, 180, 180), null, DL.CellAttrFlags.None));

                // Null foreground => the terminal's default text color (ESC[39m).
                b.DrawText(new DL.TextRun(2, 12, "This line uses the terminal's DEFAULT foreground (null fg).",
                    Fg: null, Bg: null, DL.CellAttrFlags.None));

                b.Pop();
                var baseDl = b.Build();

                var overlay = new DL.DisplayListBuilder();
                hud.ViewportCols = viewport.Width; hud.ViewportRows = viewport.Height;
                hud.Contribute(baseDl, overlay);
                await scheduler.RenderOnceAsync(Combine(baseDl, overlay.Build()), viewport, caps, pty, CancellationToken.None);
                await Task.Delay(16);
            }
        }
        finally
        {
            Console.Write("[?7h[?25h[?1049l");
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
