using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using DL = Andy.Tui.DisplayList;

namespace Andy.Tui.Examples.Demos;

public static class LargeTextClockDemo
{
    private static readonly (string City, string TimezoneId)[] Cities = new[]
    {
        ("Paris", "Europe/Paris"),
        ("London", "Europe/London"),
        ("Beijing", "Asia/Shanghai"),
        ("Delhi", "Asia/Kolkata"),
        ("Tokyo", "Asia/Tokyo"),
        ("San Francisco", "America/Los_Angeles"),
        ("New York", "America/New_York"),
    };

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
            int styleIndex = 0; // 0 Block, 1 SevenSegment, 2 Outline
            int scale = 2;
            while (running)
            {
                viewport = Andy.Tui.Examples.TerminalHelpers.PollResize(viewport, scheduler);
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape) { running = false; break; }
                    if (k.Key == ConsoleKey.F2) hud.Enabled = !hud.Enabled;
                    else if (k.Key == ConsoleKey.LeftArrow) styleIndex = (styleIndex + 2) % 3;
                    else if (k.Key == ConsoleKey.RightArrow) styleIndex = (styleIndex + 1) % 3;
                    else if (k.Key == ConsoleKey.UpArrow) scale = Math.Min(4, scale + 1);
                    else if (k.Key == ConsoleKey.DownArrow) scale = Math.Max(1, scale - 1);
                }

                var baseB = new DL.DisplayListBuilder();
                baseB.PushClip(new DL.ClipPush(0, 0, viewport.Width, viewport.Height));
                // Darcula-like dark background and accent
                var bg = new DL.Rgb24(43, 43, 43);
                var fg = new DL.Rgb24(187, 187, 187);
                var accent = new DL.Rgb24(169, 183, 198);
                baseB.DrawRect(new DL.Rect(0, 0, viewport.Width, viewport.Height, bg));
                baseB.DrawText(new DL.TextRun(2, 1, "LargeText Clock — Left/Right style; Up/Down size; ESC back; F2 HUD", accent, null, DL.CellAttrFlags.Bold));
                var baseDl = baseB.Build();

                var wb = new DL.DisplayListBuilder();
                // Compute rows per city block
                int top = 3;
                int availableH = Math.Max(1, viewport.Height - top - 1);
                int per = Math.Max(1, availableH / Cities.Length);
                var style = (Andy.Tui.Widgets.LargeText.LargeTextStyle)styleIndex;

                for (int i = 0; i < Cities.Length; i++)
                {
                    var (city, tz) = Cities[i];
                    DateTime nowUtc = DateTime.UtcNow;
                    DateTime local;
                    TimeZoneInfo? tzInfoVar = null;
                    try
                    {
                        tzInfoVar = TimeZoneInfo.FindSystemTimeZoneById(tz);
                        local = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tzInfoVar);
                    }
                    catch
                    {
                        // Fallback for environments without matching tz id
                        local = nowUtc;
                    }
                    string timeStr = local.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
                    // Title with GMT diff
                    var offset = tzInfoVar?.GetUtcOffset(local) ?? TimeSpan.Zero;
                    string gmt = FormatGmt(offset);
                    wb.DrawText(new DL.TextRun(2, top + i * per, $"{city} ({gmt})", accent, null, DL.CellAttrFlags.Bold));
                    // Large text clock below title
                    var lt = new Andy.Tui.Widgets.LargeText();
                    lt.SetText(timeStr);
                    lt.SetStyle(style);
                    lt.SetScale(scale);
                    // Color per city row for variety
                    lt.Background = bg;
                    int mod = i % 3;
                    DL.Rgb24 dig = (mod == 0)
                        ? new DL.Rgb24(187, 187, 187) // gray
                        : (mod == 1 ? new DL.Rgb24(152, 195, 121) /* greenish */ : new DL.Rgb24(224, 108, 117) /* red */);
                    lt.Foreground = dig;
                    lt.SetSpacing(scale); // add spacing proportional to scale
                    var (mw, mh) = lt.Measure();
                    int maxW = Math.Max(10, viewport.Width - 4);
                    int maxH = Math.Max(3, per - 1);
                    // Clamp rect height to available per-row block
                    int drawH = Math.Min(mh, maxH);
                    int drawW = Math.Min(mw, maxW);
                    int drawY = top + i * per + 1;
                    int drawX = 2;
                    lt.Render(new Andy.Tui.Layout.Rect(drawX, drawY, drawW, drawH), baseDl, wb);
                }

                // Footer hints
                wb.PushClip(new DL.ClipPush(0, viewport.Height - 1, viewport.Width, 1));
                wb.DrawRect(new DL.Rect(0, viewport.Height - 1, viewport.Width, 1, new DL.Rgb24(60, 63, 65)));
                wb.DrawText(new DL.TextRun(2, viewport.Height - 1, "ESC to return", fg, null, DL.CellAttrFlags.None));
                wb.Pop();

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
        Append(a);
        Append(b);
        return builder.Build();
    }

    private static string FormatGmt(TimeSpan offset)
    {
        if (offset == TimeSpan.Zero) return "GMT±00:00";
        char sign = offset.TotalMinutes >= 0 ? '+' : '-';
        offset = offset.Duration();
        return $"GMT{sign}{(int)offset.TotalHours:00}:{offset.Minutes:00}";
    }
}
