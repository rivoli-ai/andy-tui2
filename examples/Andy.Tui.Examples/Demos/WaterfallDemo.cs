using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using Andy.Tui.Examples.HackerNews;
using DL = Andy.Tui.DisplayList;

namespace Andy.Tui.Examples.Demos;

/// <summary>
/// ASCII "Hacker News Waterfall" — a calm, screensaver-like flow of blue water
/// characters tumbling down a rocky, grass-lined fall, carrying the live Hacker
/// News front page. A terminal reproduction of Peter Trizuliak's experiment
/// (https://trizuliak.com/experiments/hacker-news-waterfall).
///
/// The flow comes from the original's trick: each water column has a hashed phase,
/// and a cell's brightness band = (depth - phase) mod G. As the scroll depth grows
/// the bands march downward, so the blue glyphs appear to fall. Straight rock walls
/// line the river; sheared grass banks at the top and bottom give the isometric lip.
/// Orange story titles drift down through the water; new ones flash a green NEW
/// badge, ones you "open" turn blue. A lone fish &lt;°(((&gt;&lt; you can catch.
/// </summary>
public static class WaterfallDemo
{
    // Palette (matches the original CSS where practical).
    private static readonly DL.Rgb24 Bg = new(3, 7, 15);            // #03070f
    private static readonly DL.Rgb24 Orange = new(255, 102, 0);     // #ff6600 — titles
    private static readonly DL.Rgb24 NewGreen = new(57, 211, 83);   // #39d353 — NEW badge
    private static readonly DL.Rgb24 Visited = new(60, 80, 255);    // ~#0600ff — opened title
    private static readonly DL.Rgb24 Fish = new(120, 233, 240);     // hsl(184,90%,72%)

    private const double Shear = 1.2;        // isometric lip slope (cols per row)
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string RockChars = "▓▒░#%@&";
    private const string GrassChars = "wWvVyYmnxt\"'`,.";

    private sealed class Story
    {
        public int Id;
        public string Title = "";
        public int Points;
        public int Comments;
        public bool IsNew;
        public bool Visited;
        public int Col;     // start column in the river
        public int Order;   // 0..count-1, fixes vertical spacing slot
    }

    public static async Task Run((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        var scheduler = new Andy.Tui.Core.FrameScheduler();
        var hud = new Andy.Tui.Observability.HudOverlay { Enabled = false };
        scheduler.SetMetricsSink(hud);
        var pty = new StdoutPty();

        var rng = new Random();
        var api = new HackerNewsApiClient();

        // Shared, swapped atomically by the loader.
        var stories = OfflineStories(rng);
        var prevIds = new HashSet<int>();
        string status = "offline — press r to fetch live HN";
        var gate = new object();

        void StartLoad()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    lock (gate) status = "fetching…";
                    var ids = await api.GetTopStoriesAsync(30);
                    var items = await api.GetItemsAsync(ids);
                    var loaded = new List<Story>();
                    int order = 0;
                    foreach (var it in items.Where(i => i.Type == "story" && !string.IsNullOrWhiteSpace(i.Title)))
                    {
                        loaded.Add(new Story
                        {
                            Id = it.Id,
                            Title = it.Title!,
                            Points = it.Score ?? 0,
                            Comments = it.Descendants ?? 0,
                            Order = order++,
                        });
                    }
                    if (loaded.Count == 0) { lock (gate) status = "no stories"; return; }
                    lock (gate)
                    {
                        bool hadPrev = prevIds.Count > 0;
                        foreach (var s in loaded) s.IsNew = hadPrev && !prevIds.Contains(s.Id);
                        prevIds = new HashSet<int>(loaded.Select(s => s.Id));
                        stories = loaded;
                        status = $"live · {loaded.Count} stories from news.ycombinator.com";
                    }
                }
                catch (Exception)
                {
                    lock (gate) status = "fetch failed — showing offline set";
                }
            });
        }

        StartLoad();

        // Grid buffers (reallocated on resize).
        int W = Math.Max(8, viewport.Width), H = Math.Max(6, viewport.Height);
        char[] glyph = Array.Empty<char>();
        DL.Rgb24[] fg = Array.Empty<DL.Rgb24>();
        byte[] attr = Array.Empty<byte>();
        DL.Rgb24[] blue = Array.Empty<DL.Rgb24>();
        int gLevels = 8;

        void Realloc()
        {
            glyph = new char[W * H];
            fg = new DL.Rgb24[W * H];
            attr = new byte[W * H];
            gLevels = Math.Max(4, (int)Math.Round((0.88 * H - 0.12 * H) / 2.0));
            // 12 hues (208..219) x gLevels brightness shades — the flowing blue gradient.
            blue = new DL.Rgb24[12 * gLevels];
            for (int hue = 0; hue < 12; hue++)
                for (int s = 0; s < gLevels; s++)
                {
                    double light = 14 + 62 * (gLevels == 1 ? 0 : (double)s / (gLevels - 1));
                    blue[hue * gLevels + s] = HslToRgb(208 + hue, 0.86, light / 100.0);
                }
        }
        Realloc();

        Console.Write("[?1049h[?25l[?7l");
        try
        {
            bool running = true;
            double scroll = 0;         // title drift (rows)
            double waterScroll = 0;    // water colour-band drift — slower, for a calm flow
            double speed = 3;          // rows per second
            int caught = 0;
            long startMs = Environment.TickCount64;
            long lastMs = startMs;

            // Fish state.
            double fishX = -10; int fishY = H / 2; bool fishRight = true;
            void RespawnFish()
            {
                fishRight = rng.Next(2) == 0;
                fishY = rng.Next(Math.Max(1, (int)(0.15 * H)), Math.Max(2, (int)(0.85 * H)));
                fishX = fishRight ? -8 : W + 8;
            }
            RespawnFish();

            while (running)
            {
                var rs = TerminalHelpers.PollResize(viewport, scheduler);
                if (rs.Width != viewport.Width || rs.Height != viewport.Height)
                {
                    viewport = rs;
                    W = Math.Max(8, viewport.Width); H = Math.Max(6, viewport.Height);
                    Realloc();
                }

                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape || k.Key == ConsoleKey.Q) { running = false; break; }
                    switch (k.Key)
                    {
                        case ConsoleKey.R: StartLoad(); break;
                        case ConsoleKey.F2: hud.Enabled = !hud.Enabled; break;
                        case ConsoleKey.OemPlus:
                        case ConsoleKey.Add:
                        case ConsoleKey.RightArrow: speed = Math.Min(30, speed + 1); break;
                        case ConsoleKey.OemMinus:
                        case ConsoleKey.Subtract:
                        case ConsoleKey.LeftArrow: speed = Math.Max(1, speed - 1); break;
                        case ConsoleKey.F:
                        case ConsoleKey.Spacebar:
                            // Cast: if the fish is on screen in the channel, catch it.
                            if (fishX > 1 && fishX < W - 1) { caught++; RespawnFish(); }
                            break;
                    }
                }

                long now = Environment.TickCount64;
                double dt = Math.Min(0.1, (now - lastMs) / 1000.0);
                lastMs = now;
                scroll += speed * dt;
                waterScroll += speed * 0.45 * dt; // water flows visibly slower than the titles
                int depth0 = (int)waterScroll;

                // Geometry.
                int topBank = (int)Math.Round(0.12 * H);
                int botBank = (int)Math.Round(0.88 * H);
                int J = Math.Max(3, (int)Math.Round(0.05 * W));

                // --- Fill the grid: rock walls, sheared grass banks, flowing blue water. ---
                for (int y = 0; y < H; y++)
                {
                    bool inTop = y < topBank, inBot = y >= botBank;
                    for (int x = 0; x < W; x++)
                    {
                        int idx = y * W + x;
                        Kind kind = Classify(x, y, W, topBank, botBank, J, inTop, inBot);
                        if (kind == Kind.Rock)
                        {
                            uint hr = Hash2(x, y);
                            glyph[idx] = RockChars[(int)(hr % (uint)RockChars.Length)];
                            byte g = (byte)(70 + hr % 35);
                            fg[idx] = new DL.Rgb24(g, (byte)(g + 6), (byte)(g + 12));
                            attr[idx] = 0;
                        }
                        else if (kind == Kind.Grass)
                        {
                            uint hg = Hash2(x * 7, y * 13 + 99);
                            glyph[idx] = GrassChars[(int)(hg % (uint)GrassChars.Length)];
                            fg[idx] = HslToRgb(108 + (int)(hg % 26), 0.62, (34 + hg % 18) / 100.0);
                            attr[idx] = 0;
                        }
                        else // water
                        {
                            int depth = y - depth0; // bands descend as time advances
                            uint h = Hash2(x, depth);
                            int phase = (int)(Hash2(x, 24301) % (uint)gLevels);
                            int shade = ((depth - phase) % gLevels + gLevels) % gLevels;
                            int hue = (int)((h >> 8) % 12);
                            glyph[idx] = Alphabet[(int)(h % (uint)Alphabet.Length)];
                            fg[idx] = blue[hue * gLevels + shade];
                            attr[idx] = 0;
                        }
                    }
                }

                // --- Overlay HN titles drifting down through the river. ---
                var snap = stories;
                int count = Math.Max(1, snap.Count);
                int spacing = Math.Max(3, (H + 4) / count);
                int span = H + 2 * spacing;
                foreach (var s in snap)
                {
                    // Each story keeps a stable column the first time we see it.
                    if (s.Col == 0) s.Col = J + 2 + rng.Next(Math.Max(1, W - 2 * J - 4));
                    double pos = (s.Order * spacing + scroll) % span;
                    int y = (int)Math.Floor(pos) - spacing;
                    if (y < 0 || y >= H) continue;

                    var (text, segNew) = TitleText(s);
                    int shX = ShearAt(y, topBank, botBank);
                    bool bold = s.Points >= 150;
                    for (int k = 0; k < text.Length; k++)
                    {
                        int x = s.Col + k + shX;
                        if (x < 0 || x >= W) continue;
                        if (Classify(x, y, W, topBank, botBank, J, y < topBank, y >= botBank) != Kind.Water) continue;
                        int idx = y * W + x;
                        glyph[idx] = text[k];
                        fg[idx] = s.Visited ? Visited : (k >= segNew ? NewGreen : Orange);
                        attr[idx] = (byte)(bold ? 1 : 0);
                    }
                }

                // --- Fish. ---
                fishX += (fishRight ? 1 : -1) * speed * 1.6 * dt;
                if (fishX > W + 10 || fishX < -10) RespawnFish();
                string fishSprite = fishRight ? "><(((°>" : "<°)))><";
                int fy = Math.Clamp(fishY, 0, H - 1);
                for (int k = 0; k < fishSprite.Length; k++)
                {
                    int x = (int)Math.Round(fishX) + k;
                    if (x < 0 || x >= W) continue;
                    int idx = fy * W + x;
                    glyph[idx] = fishSprite[k];
                    fg[idx] = Fish;
                    attr[idx] = 1;
                }

                // --- Emit: one bg rect + per-row batched text runs. ---
                var b = new DL.DisplayListBuilder();
                b.PushClip(new DL.ClipPush(0, 0, W, H));
                b.DrawRect(new DL.Rect(0, 0, W, H, Bg));
                var sb = new StringBuilder(W);
                for (int y = 0; y < H; y++)
                {
                    int runStart = 0;
                    DL.Rgb24 runFg = fg[y * W];
                    byte runAttr = attr[y * W];
                    sb.Clear();
                    for (int x = 0; x < W; x++)
                    {
                        int idx = y * W + x;
                        if (x > 0 && (!fg[idx].Equals(runFg) || attr[idx] != runAttr))
                        {
                            b.DrawText(new DL.TextRun(runStart, y, sb.ToString(), runFg, null,
                                runAttr == 1 ? DL.CellAttrFlags.Bold : DL.CellAttrFlags.None));
                            sb.Clear();
                            runStart = x; runFg = fg[idx]; runAttr = attr[idx];
                        }
                        sb.Append(glyph[idx]);
                    }
                    b.DrawText(new DL.TextRun(runStart, y, sb.ToString(), runFg, null,
                        runAttr == 1 ? DL.CellAttrFlags.Bold : DL.CellAttrFlags.None));
                }

                // Status line (legible over the water).
                string st;
                lock (gate) st = status;
                string bar = $" Hacker News Waterfall · {st} · speed {speed:0}  🐟 {caught} ";
                string keys = " [r]eload  ←/→ speed  [f]/space catch  ESC quit ";
                b.DrawText(new DL.TextRun(0, 0, Fit(bar, W), Orange, Bg, DL.CellAttrFlags.Bold));
                if (H > 1)
                    b.DrawText(new DL.TextRun(0, H - 1, Fit(keys, W), new DL.Rgb24(170, 180, 200), Bg, DL.CellAttrFlags.None));

                b.Pop();
                var baseDl = b.Build();
                var overlay = new DL.DisplayListBuilder();
                hud.ViewportCols = W; hud.ViewportRows = H;
                hud.Contribute(baseDl, overlay);
                await scheduler.RenderOnceAsync(Combine(baseDl, overlay.Build()), (W, H), caps, pty, CancellationToken.None);
                await Task.Delay(33); // ~30 FPS
            }
        }
        finally
        {
            Console.Write("[?7h[?25h[?1049l");
        }
    }

    private enum Kind { Water, Rock, Grass }

    private static Kind Classify(int x, int y, int W, int topBank, int botBank, int J, bool inTop, bool inBot)
    {
        if (inTop || inBot)
        {
            int bank = inTop ? topBank : botBank;
            double r = x - Shear * y;
            double lo = J - Shear * bank, hi = W - J - Shear * bank;
            if (r < lo || r > hi) return Kind.Grass;
            return Kind.Water;
        }
        if (x < J || x >= W - J) return Kind.Rock;
        return Kind.Water;
    }

    private static int ShearAt(int y, int topBank, int botBank)
    {
        if (y < topBank) return (int)Math.Round((y - topBank) * Shear);
        if (y >= botBank) return (int)Math.Round((y - botBank) * Shear);
        return 0;
    }

    private static (string text, int segNewStart) TitleText(Story s)
    {
        string suffix = s.IsNew ? "| NEW " : $"| {s.Points}/{s.Comments} ";
        string head = " " + s.Title + " ";
        return (head + suffix, head.Length);
    }

    private static string Fit(string s, int w)
    {
        if (w <= 0) return "";
        if (s.Length >= w) return s.Substring(0, w);
        return s + new string(' ', w - s.Length);
    }

    // Port of the experiment's integer hash (mirrors JS Math.imul behaviour).
    private static uint Hash2(int e, int t)
    {
        unchecked
        {
            uint r = (uint)((int)((uint)e * 374761393u) + (int)((uint)t * 668265263u)) ^ 2654435769u;
            r = (r ^ (r >> 13)) * 1274126177u;
            return r ^ (r >> 16);
        }
    }

    private static DL.Rgb24 HslToRgb(double h, double s, double l)
    {
        h = ((h % 360) + 360) % 360;
        double c = (1 - Math.Abs(2 * l - 1)) * s;
        double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
        double m = l - c / 2;
        double r, g, b;
        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }
        return new DL.Rgb24(
            (byte)Math.Clamp((r + m) * 255, 0, 255),
            (byte)Math.Clamp((g + m) * 255, 0, 255),
            (byte)Math.Clamp((b + m) * 255, 0, 255));
    }

    private static List<Story> OfflineStories(Random rng)
    {
        string[] titles =
        {
            "Show HN: I built a terminal renderer in C#",
            "The calm engineering of ASCII waterfalls",
            "Why monospace text measurement is harder than it looks",
            "A gentle introduction to cellular automata",
            "Ask HN: What are you building this weekend?",
            "Rendering 60fps in the terminal without tears",
            "The lost art of the screensaver",
            "How firebase serves the Hacker News API",
            "Isometric projection with a single shear",
            "Catching fish in a river of falling letters",
        };
        var list = new List<Story>();
        for (int i = 0; i < titles.Length; i++)
            list.Add(new Story
            {
                Id = -(i + 1),
                Title = titles[i],
                Points = rng.Next(20, 600),
                Comments = rng.Next(0, 300),
                Order = i,
            });
        return list;
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
