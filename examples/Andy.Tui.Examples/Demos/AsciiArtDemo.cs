using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using DL = Andy.Tui.DisplayList;

namespace Andy.Tui.Examples.Demos;

public static class AsciiArtDemo
{
    private static readonly string Attribution = "Source: Horace Vernet, Public domain, via Wikimedia Commons";

    public static async Task Run((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        var scheduler = new Andy.Tui.Core.FrameScheduler(targetFps: 30);
        var hud = new Andy.Tui.Observability.HudOverlay { Enabled = true };
        scheduler.SetMetricsSink(hud);
        var pty = new StdoutPty();
        Console.Write("\u001b[?1049h\u001b[?25l\u001b[?7l");
        Console.TreatControlCAsInput = true;
        try
        {
            bool running = true;
            // Load pre-sampled raw RGB asset(s) from tmp/ if available (e.g., napoleon_80x60.rgb, napoleon_120x90.rgb, ...)
            byte[] rasterRgb;
            int imgW, imgH;
            string loadedFrom = "";
            if (!TryLoadBestRgbAsset(viewport.Width - 4, viewport.Height - 6, out rasterRgb, out imgW, out imgH, out loadedFrom))
            {
                // fall back to embedded assets path or gradient
                var defaultPath = System.IO.Path.Combine(AppContext.BaseDirectory, "assets", "napoleon_80x60.rgb");
                if (System.IO.File.Exists(defaultPath))
                {
                    rasterRgb = await System.IO.File.ReadAllBytesAsync(defaultPath);
                    imgW = 80; imgH = 60;
                    loadedFrom = defaultPath;
                }
                else
                {
                    imgW = 120; imgH = 40; rasterRgb = GenerateFallbackGradient(imgW, imgH);
                    loadedFrom = "[gradient]";
                }
            }

            // Clear any residual keys from menu selection
            while (Console.KeyAvailable) Console.ReadKey(true);

            while (running)
            {
                viewport = Andy.Tui.Examples.TerminalHelpers.PollResize(viewport, scheduler);
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape) { running = false; break; }
                    if (k.Key == ConsoleKey.F2) hud.Enabled = !hud.Enabled;
                }

                var baseB = new DL.DisplayListBuilder();
                baseB.PushClip(new DL.ClipPush(0, 0, viewport.Width, viewport.Height));
                baseB.DrawRect(new DL.Rect(0, 0, viewport.Width, viewport.Height, new DL.Rgb24(0, 0, 0)));
                baseB.DrawText(new DL.TextRun(2, 1, "ASCII Art (true color) — ESC/Q back; F2 HUD", new DL.Rgb24(200, 200, 50), null, DL.CellAttrFlags.Bold));
                // Attribution at bottom
                baseB.PushClip(new DL.ClipPush(0, Math.Max(0, viewport.Height - 1), viewport.Width, 1));
                baseB.DrawRect(new DL.Rect(0, Math.Max(0, viewport.Height - 1), viewport.Width, 1, new DL.Rgb24(15, 15, 15)));
                baseB.DrawText(new DL.TextRun(2, Math.Max(0, viewport.Height - 1), Attribution, new DL.Rgb24(160, 160, 160), null, DL.CellAttrFlags.None));
                baseB.Pop();
                // Show loaded asset hint one line below the title
                baseB.DrawText(new DL.TextRun(2, 2, $"Loaded: {System.IO.Path.GetFileName(loadedFrom)} ({imgW}x{imgH})", new DL.Rgb24(140, 140, 140), null, DL.CellAttrFlags.None));
                var baseDl = baseB.Build();

                // Render ASCII into builder
                var wb = new DL.DisplayListBuilder();
                int topY = 3;
                int maxH = Math.Max(0, viewport.Height - topY - 2);
                int maxW = Math.Max(0, viewport.Width - 2);
                if (maxW > 0 && maxH > 0)
                {
                    try
                    {
                        RenderAsciiColor(rasterRgb, imgW, imgH, 2, topY, maxW, maxH, baseDl, wb);
                    }
                    catch (Exception ex)
                    {
                        // Show error on screen so we don't just exit
                        wb.DrawText(new DL.TextRun(2, topY, $"[error] {ex.Message}", new DL.Rgb24(255, 80, 80), null, DL.CellAttrFlags.Bold));
                    }
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
            Console.Write("\u001b[?7h\u001b[?25h\u001b[?1049l");
        }
    }

    private static void RenderAsciiColor(byte[] rgb, int imgW, int imgH, int x, int y, int maxW, int maxH, DL.DisplayList baseDl, DL.DisplayListBuilder b)
    {
        const string ramp = " .,:;ox%#@";
        for (int row = 0; row < maxH; row++)
        {
            int srcY = (int)((row / (double)maxH) * imgH);
            if (srcY >= imgH) srcY = imgH - 1;
            for (int col = 0; col < maxW; col++)
            {
                int srcX = (int)Math.Floor((col + 0.5) * imgW / (double)maxW);
                if (srcX >= imgW) srcX = imgW - 1;
                int i = (srcY * imgW + srcX) * 3;
                byte r = rgb[i + 0], g = rgb[i + 1], bl = rgb[i + 2];
                byte lum = (byte)Math.Clamp((0.2126 * r + 0.7152 * g + 0.0722 * bl), 0, 255);
                int idx = (int)Math.Round((lum / 255.0) * (ramp.Length - 1));
                char ch = ramp[idx];
                // Use ANSI truecolor by rendering colored text on black
                b.DrawText(new DL.TextRun(x + col, y + row, ch.ToString(), new DL.Rgb24(r, g, bl), null, DL.CellAttrFlags.None));
            }
        }
    }

    private static bool TryLoadBestRgbAsset(int targetCols, int targetRows, out byte[] data, out int w, out int h, out string chosenPath)
    {
        data = Array.Empty<byte>(); w = 0; h = 0; chosenPath = string.Empty;
        try
        {
            // Candidate directories to search for assets (prefer example assets/, then tmp/ fallbacks)
            var candidates = new[]
            {
                // Built output assets (copied by csproj)
                System.IO.Path.Combine(AppContext.BaseDirectory, "assets"),
                // Source tree assets (when running from repo root)
                System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "examples", "Andy.Tui.Examples", "assets")),
                System.IO.Path.Combine(Environment.CurrentDirectory, "examples", "Andy.Tui.Examples", "assets"),
                // Legacy tmp locations (still check, but lower priority)
                System.IO.Path.Combine(Environment.CurrentDirectory, "tmp"),
                System.IO.Path.Combine(AppContext.BaseDirectory, "tmp"),
                System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "tmp"))
            };
            string? bestPath = null; int bestW = 0, bestH = 0; int bestScore = int.MaxValue;
            foreach (var dir in candidates)
            {
                if (!System.IO.Directory.Exists(dir)) continue;
                foreach (var file in System.IO.Directory.EnumerateFiles(dir, "*.rgb"))
                {
                    var name = System.IO.Path.GetFileNameWithoutExtension(file);
                    // try to extract WxH from last _WxH pattern in the name
                    int us = name.LastIndexOf('_'); if (us < 0) continue;
                    var sizePart = name.Substring(us + 1);
                    var parts = sizePart.Split('x', 'X');
                    if (parts.Length != 2) continue;
                    if (!int.TryParse(parts[0], out int aw) || !int.TryParse(parts[1], out int ah)) continue;
                    // Validate raw size via file length (must equal W*H*3)
                    var fi = new System.IO.FileInfo(file);
                    if (fi.Length != (long)aw * ah * 3) continue;
                    // Score: prefer sizes nearest to current viewport content area
                    int score = Math.Abs(aw - Math.Max(1, targetCols)) + Math.Abs(ah - Math.Max(1, targetRows));
                    if (score < bestScore)
                    {
                        bestScore = score; bestPath = file; bestW = aw; bestH = ah;
                    }
                }
            }
            if (!string.IsNullOrEmpty(bestPath))
            {
                data = System.IO.File.ReadAllBytes(bestPath);
                w = bestW; h = bestH; chosenPath = bestPath; return true;
            }
        }
        catch { /* ignore */ }
        return false;
    }

    // Extremely small JPEG luminance extractor placeholder.
    // In a real implementation, replace with a robust decoder (e.g., SkiaSharp) and pass decoded grayscale.
    private static class SimpleJpeg
    {
        public static byte[] Luminance(byte[] data, out int width, out int height)
        {
            // Procedural fallback: generate a gradient if decode is not implemented
            width = 120; height = 40;
            var buf = new byte[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // radial gradient
                    double nx = (x - width / 2.0) / (width / 2.0);
                    double ny = (y - height / 2.0) / (height / 2.0);
                    double d = Math.Sqrt(nx * nx + ny * ny);
                    double v = Math.Clamp(1.0 - d, 0.0, 1.0);
                    buf[y * width + x] = (byte)Math.Round(v * 255);
                }
            }
            return buf;
        }
    }

    private static byte[] GenerateFallbackGradient(int w, int h)
    {
        var buf = new byte[w * h * 3];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int i = (y * w + x) * 3;
                byte v = (byte)Math.Round(255.0 * x / Math.Max(1, w - 1));
                buf[i] = v; buf[i + 1] = v; buf[i + 2] = v;
            }
        }
        return buf;
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
