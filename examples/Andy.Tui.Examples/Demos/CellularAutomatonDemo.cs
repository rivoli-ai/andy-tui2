using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using Andy.Tui.Examples;
using DL = Andy.Tui.DisplayList;

namespace Andy.Tui.Examples.Demos;

/// <summary>
/// Full-terminal cellular automaton, intended as a screensaver foundation.
///
/// To stay "active" — i.e. to avoid drawing the same glyph in the same place on a
/// regular cadence — it does three things:
///   * defaults to rules that never settle (cyclic spirals, Seeds, excitable waves);
///   * <b>auto-rotates</b> through a table of rules every 30s;
///   * periodically <b>perturbs</b> the field and <b>reseeds</b> on stagnation/extinction.
///
/// Rules included: Cyclic CA, Brian's Brain, Life (B3/S23), HighLife, Day &amp; Night,
/// Seeds, Maze, Forest Fire, and Greenberg-Hastings (excitable medium). Add more by
/// extending <see cref="Rules"/>.
///
/// Each cell renders as one of a ramp of Unicode circle glyphs depending on its state.
/// Color comes from a selectable <b>palette</b> (cycle with &lt; / &gt;): palettes are
/// smooth gradients so bands blend, and switching palettes cross-fades rather than
/// snapping.
/// </summary>
public static class CellularAutomatonDemo
{
    private enum Kind { Cyclic, BriansBrain, LifeFamily, ForestFire, Greenberg }

    /// <summary>A simulation rule. Life-family rules are parameterized by birth/survival counts.</summary>
    private sealed class Rule
    {
        public string Name = "";
        public Kind Kind;
        public bool[] Birth = new bool[9];   // LifeFamily: born when this many live neighbors
        public bool[] Survive = new bool[9];  // LifeFamily: survives when this many live neighbors

        public static Rule Simple(string name, Kind kind) => new() { Name = name, Kind = kind };

        public static Rule Life(string name, string birth, string survive)
        {
            var r = new Rule { Name = name, Kind = Kind.LifeFamily };
            foreach (var ch in birth) r.Birth[ch - '0'] = true;
            foreach (var ch in survive) r.Survive[ch - '0'] = true;
            return r;
        }
    }

    private static readonly Rule[] Rules =
    {
        Rule.Simple("Cyclic CA", Kind.Cyclic),
        Rule.Simple("Brian's Brain", Kind.BriansBrain),
        Rule.Life("Life", "3", "23"),
        Rule.Life("HighLife", "36", "23"),
        Rule.Life("Day & Night", "3678", "34678"),
        Rule.Life("Seeds", "2", ""),
        Rule.Life("Maze", "3", "12345"),
        Rule.Simple("Forest Fire", Kind.ForestFire),
        Rule.Simple("Greenberg-Hastings", Kind.Greenberg),
    };

    // A ramp of circle glyphs from faint/small to solid. All are single-width BMP glyphs.
    private static readonly string[] CircleRamp = { "·", "∘", "○", "◌", "◍", "◎", "◉", "●" };

    // Brian's Brain / Forest Fire cell states.
    private const int Off = 0, On = 1, Dying = 2; // Brian: Off/On/Dying; Fire: Empty/Tree/Burning

    private const long AutoRotateMs = 30_000;
    private const int PaletteFadeMs = 600;
    private const long PaletteToastMs = 1_400;

    // Forest Fire probabilities.
    private const double FireGrow = 0.012;     // empty -> tree
    private const double FireLightning = 6e-5;  // tree -> burning spontaneously

    /// <summary>A seamless ring of colors, sampled with smooth (circular) interpolation.</summary>
    private sealed class Palette
    {
        public string Name { get; }
        private readonly DL.Rgb24[] _c;
        public Palette(string name, params DL.Rgb24[] colors) { Name = name; _c = colors; }

        public DL.Rgb24 At(double t)
        {
            t -= Math.Floor(t); // fractional part -> [0,1)
            int n = _c.Length;
            double scaled = t * n;
            int i = (int)scaled;
            if (i >= n) i = n - 1;
            double f = scaled - i;
            return Lerp(_c[i], _c[(i + 1) % n], f);
        }
    }

    private static readonly Palette[] Palettes = BuildPalettes();

    private static Palette[] BuildPalettes()
    {
        var rainbow = new DL.Rgb24[12];
        for (int i = 0; i < rainbow.Length; i++) rainbow[i] = HsvToRgb(i * 360.0 / rainbow.Length, 0.85, 1.0);

        return new[]
        {
            new Palette("Rainbow", rainbow),
            new Palette("Fire",
                new DL.Rgb24(20, 0, 0), new DL.Rgb24(90, 0, 0), new DL.Rgb24(160, 20, 0),
                new DL.Rgb24(220, 60, 0), new DL.Rgb24(255, 120, 10), new DL.Rgb24(255, 190, 60),
                new DL.Rgb24(255, 240, 160)),
            new Palette("Ocean",
                new DL.Rgb24(5, 15, 50), new DL.Rgb24(10, 50, 110), new DL.Rgb24(20, 110, 170),
                new DL.Rgb24(40, 170, 200), new DL.Rgb24(150, 225, 235)),
            new Palette("Viridis",
                new DL.Rgb24(68, 1, 84), new DL.Rgb24(59, 82, 139), new DL.Rgb24(33, 144, 140),
                new DL.Rgb24(93, 201, 99), new DL.Rgb24(253, 231, 37)),
            new Palette("Sunset",
                new DL.Rgb24(30, 12, 60), new DL.Rgb24(120, 28, 110), new DL.Rgb24(210, 55, 95),
                new DL.Rgb24(255, 120, 60), new DL.Rgb24(255, 205, 90)),
            new Palette("Ice",
                new DL.Rgb24(8, 18, 55), new DL.Rgb24(35, 80, 160), new DL.Rgb24(110, 175, 230),
                new DL.Rgb24(225, 242, 255)),
            new Palette("Matrix",
                new DL.Rgb24(0, 15, 0), new DL.Rgb24(0, 80, 25), new DL.Rgb24(0, 170, 55),
                new DL.Rgb24(130, 255, 150)),
            new Palette("Neon",
                new DL.Rgb24(255, 0, 128), new DL.Rgb24(150, 0, 255), new DL.Rgb24(0, 190, 255),
                new DL.Rgb24(0, 255, 140), new DL.Rgb24(245, 255, 40)),
        };
    }

    public static async Task Run((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        var scheduler = new Andy.Tui.Core.FrameScheduler();
        var hud = new Andy.Tui.Observability.HudOverlay { Enabled = false };
        scheduler.SetMetricsSink(hud);
        var pty = new StdoutPty();

        var rng = new Random();

        int W = Math.Max(1, viewport.Width);
        int H = Math.Max(1, viewport.Height);

        int ruleIndex = 0;
        int states = 12;     // cyclic-CA state count
        int threshold = 1;   // cyclic-CA advance threshold (Moore neighborhood)
        int ghStates = 8;    // Greenberg-Hastings phase count
        int[] grid = new int[W * H];
        int[] next = new int[W * H];

        bool paused = false;
        bool showFooter = true;
        bool autoRotate = true;
        int stepDelayMs = 70;

        long now = Environment.TickCount64;
        long startMs = now;
        long lastStepMs = now;
        long lastModeSwitchMs = now;
        long lastPerturbMs = now;

        // Palette selection + cross-fade state.
        int paletteIndex = 0;
        int prevPaletteIndex = 0;
        long paletteFadeStartMs = now - PaletteFadeMs * 2; // start fully settled
        long paletteToastUntilMs = 0;

        // Stagnation tracking (used by Life-family).
        int stagnantSteps = 0;
        int lastPop = -1;

        bool dirty = true;

        void Seed()
        {
            var kind = Rules[ruleIndex].Kind;
            for (int i = 0; i < grid.Length; i++)
            {
                grid[i] = kind switch
                {
                    Kind.Cyclic => rng.Next(states),
                    Kind.BriansBrain => rng.Next(3) == 0 ? On : Off,
                    Kind.LifeFamily => rng.Next(100) < 30 ? 1 : 0,
                    Kind.ForestFire => rng.Next(100) < 55 ? On : Off,
                    Kind.Greenberg => rng.Next(100) < 15 ? rng.Next(1, ghStates) : 0,
                    _ => 0,
                };
            }
            stagnantSteps = 0;
            lastPop = -1;
        }

        void OnRuleChanged()
        {
            var kind = Rules[ruleIndex].Kind;
            if (kind == Kind.Cyclic) states = 8 + rng.Next(9);   // 8..16 for palette variety
            if (kind == Kind.Greenberg) ghStates = 6 + rng.Next(6); // 6..11
            Seed();
            dirty = true;
        }

        void NextMode()
        {
            ruleIndex = (ruleIndex + 1) % Rules.Length;
            OnRuleChanged();
        }

        void CyclePalette(int dir)
        {
            prevPaletteIndex = paletteIndex;
            paletteIndex = (paletteIndex + dir + Palettes.Length) % Palettes.Length;
            long t = Environment.TickCount64;
            paletteFadeStartMs = t;
            paletteToastUntilMs = t + PaletteToastMs;
            dirty = true;
        }

        void Perturb()
        {
            // Inject defects so cyclic spirals / waves keep forming and never settle.
            int n = grid.Length / 200 + 5;
            var kind = Rules[ruleIndex].Kind;
            for (int k = 0; k < n; k++)
            {
                int idx = rng.Next(grid.Length);
                grid[idx] = kind == Kind.Greenberg ? 1 : rng.Next(states);
            }
        }

        int CountEq(int[] g, int x, int y, int wanted)
        {
            int c = 0;
            for (int dy = -1; dy <= 1; dy++)
            {
                int ny = (y + dy + H) % H;
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = (x + dx + W) % W;
                    if (g[ny * W + nx] == wanted) c++;
                }
            }
            return c;
        }

        int CountAlive(int[] g, int x, int y)
        {
            int c = 0;
            for (int dy = -1; dy <= 1; dy++)
            {
                int ny = (y + dy + H) % H;
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = (x + dx + W) % W;
                    if (g[ny * W + nx] > 0) c++;
                }
            }
            return c;
        }

        void Step()
        {
            var rule = Rules[ruleIndex];
            switch (rule.Kind)
            {
                case Kind.Cyclic:
                    for (int y = 0; y < H; y++)
                        for (int x = 0; x < W; x++)
                        {
                            int i = y * W + x;
                            int cur = grid[i];
                            int want = (cur + 1) % states;
                            next[i] = CountEq(grid, x, y, want) >= threshold ? want : cur;
                        }
                    break;

                case Kind.BriansBrain:
                    for (int y = 0; y < H; y++)
                        for (int x = 0; x < W; x++)
                        {
                            int i = y * W + x;
                            next[i] = grid[i] switch
                            {
                                On => Dying,
                                Dying => Off,
                                _ => CountEq(grid, x, y, On) == 2 ? On : Off,
                            };
                        }
                    break;

                case Kind.LifeFamily:
                {
                    int pop = 0;
                    for (int y = 0; y < H; y++)
                        for (int x = 0; x < W; x++)
                        {
                            int i = y * W + x;
                            bool alive = grid[i] > 0;
                            int n = CountAlive(grid, x, y);
                            bool live = alive ? rule.Survive[n] : rule.Birth[n];
                            next[i] = live ? (alive ? Math.Min(grid[i] + 1, 9999) : 1) : 0;
                            if (next[i] > 0) pop++;
                        }
                    if (pop == 0) { Seed(); return; }
                    if (pop == lastPop) stagnantSteps++; else stagnantSteps = 0;
                    lastPop = pop;
                    if (stagnantSteps > 50) { Seed(); return; }
                    break;
                }

                case Kind.ForestFire:
                    for (int y = 0; y < H; y++)
                        for (int x = 0; x < W; x++)
                        {
                            int i = y * W + x;
                            int cur = grid[i];
                            if (cur == Dying) next[i] = Off;                 // burned out
                            else if (cur == On)                              // tree
                                next[i] = (CountEq(grid, x, y, Dying) > 0 || rng.NextDouble() < FireLightning) ? Dying : On;
                            else                                             // empty
                                next[i] = rng.NextDouble() < FireGrow ? On : Off;
                        }
                    break;

                case Kind.Greenberg:
                {
                    int active = 0;
                    for (int y = 0; y < H; y++)
                        for (int x = 0; x < W; x++)
                        {
                            int i = y * W + x;
                            int cur = grid[i];
                            int nv;
                            if (cur == 0) nv = CountEq(grid, x, y, 1) >= 1 ? 1 : 0; // excite
                            else if (cur >= ghStates - 1) nv = 0;                    // refractory -> rest
                            else nv = cur + 1;                                       // advance phase
                            next[i] = nv;
                            if (nv > 0) active++;
                        }
                    if (active == 0) { Seed(); return; }
                    break;
                }
            }

            var tmp = grid; grid = next; next = tmp;
        }

        // Maps a cell value to a glyph (null = draw nothing) and a normalized palette
        // position t in [0,1). For the cyclic CA, <paramref name="rot"/> slowly rotates
        // the whole ring so the palette drifts over time.
        (string? glyph, double t) CellVisual(int value, double rot)
        {
            switch (Rules[ruleIndex].Kind)
            {
                case Kind.Cyclic:
                {
                    int idx = (int)((long)value * CircleRamp.Length / Math.Max(1, states));
                    if (idx >= CircleRamp.Length) idx = CircleRamp.Length - 1;
                    return (CircleRamp[idx], value / (double)states + rot);
                }
                case Kind.BriansBrain:
                    return value switch
                    {
                        On => ("●", 0.82),
                        Dying => ("○", 0.42),
                        _ => (null, 0.0),
                    };
                case Kind.ForestFire:
                    return value switch
                    {
                        On => ("◍", 0.45),       // tree
                        Dying => ("●", 0.95),    // burning
                        _ => (null, 0.0),         // empty
                    };
                case Kind.Greenberg:
                {
                    if (value <= 0) return (null, 0.0); // resting
                    int idx = (int)((long)value * CircleRamp.Length / Math.Max(1, ghStates));
                    if (idx >= CircleRamp.Length) idx = CircleRamp.Length - 1;
                    return (CircleRamp[idx], value / (double)ghStates);
                }
                case Kind.LifeFamily:
                default:
                    if (value <= 0) return (null, 0.0);
                    int ri = Math.Min(value + 1, CircleRamp.Length - 1);
                    return (CircleRamp[ri], 0.08 + value * 0.05); // hue drifts as the cell ages
            }
        }

        DL.DisplayList BuildDisplayList()
        {
            long tnow = Environment.TickCount64;
            double rot = Rules[ruleIndex].Kind == Kind.Cyclic ? (tnow - startMs) / 8000.0 : 0.0; // full ring every ~8s
            double fade = PaletteFadeMs <= 0 ? 1.0 : Math.Clamp((tnow - paletteFadeStartMs) / (double)PaletteFadeMs, 0.0, 1.0);

            DL.Rgb24 ColorAt(double t)
            {
                var cur = Palettes[paletteIndex].At(t);
                return fade >= 1.0 ? cur : Lerp(Palettes[prevPaletteIndex].At(t), cur, fade);
            }

            var b = new DL.DisplayListBuilder();
            b.PushClip(new DL.ClipPush(0, 0, W, H));
            b.DrawRect(new DL.Rect(0, 0, W, H, new DL.Rgb24(0, 0, 0)));

            var glyphs = new string?[W];
            var fgs = new DL.Rgb24[W];
            var sb = new StringBuilder();

            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    var (g, t) = CellVisual(grid[y * W + x], rot);
                    glyphs[x] = g;
                    fgs[x] = g is null ? default : ColorAt(t);
                }

                // Run-length batch consecutive non-blank cells that share a color.
                int x2 = 0;
                while (x2 < W)
                {
                    if (glyphs[x2] is null) { x2++; continue; }
                    int start = x2;
                    var color = fgs[x2];
                    sb.Clear();
                    while (x2 < W && glyphs[x2] is string gg && fgs[x2].Equals(color))
                    {
                        sb.Append(gg);
                        x2++;
                    }
                    b.DrawText(new DL.TextRun(start, y, sb.ToString(), color, null, DL.CellAttrFlags.None));
                }
            }

            // Brief centered toast naming the palette (visible even with footer hidden).
            if (tnow < paletteToastUntilMs && H >= 1)
            {
                string label = "‹ " + Palettes[paletteIndex].Name + " ›";
                int tx = Math.Max(0, (W - label.Length) / 2);
                int ty = H / 2;
                b.DrawRect(new DL.Rect(Math.Max(0, tx - 1), ty, Math.Min(W, label.Length + 2), 1, new DL.Rgb24(22, 22, 30)));
                b.DrawText(new DL.TextRun(tx, ty, label, new DL.Rgb24(235, 235, 245), null, DL.CellAttrFlags.Bold));
            }

            if (showFooter && H >= 1)
            {
                string name = Rules[ruleIndex].Kind == Kind.Cyclic
                    ? $"Cyclic CA (states {states})"
                    : Rules[ruleIndex].Name;
                string pauseTag = paused ? " [PAUSED]" : "";
                string footer = $" {name}{pauseTag} · Palette: {Palettes[paletteIndex].Name} · M mode  </> palette  Space pause  R reseed  +/- speed  H hide  ESC quit ";
                if (footer.Length > W) footer = footer.Substring(0, W);
                b.DrawRect(new DL.Rect(0, H - 1, W, 1, new DL.Rgb24(12, 12, 18)));
                b.DrawText(new DL.TextRun(0, H - 1, footer, new DL.Rgb24(170, 170, 185), null, DL.CellAttrFlags.None));
            }

            b.Pop();
            return b.Build();
        }

        Seed();

        Console.Write("\u001b[?1049h\u001b[?25l\u001b[?7l");
        try
        {
            DL.DisplayList? cached = null;
            bool running = true;
            while (running)
            {
                var newVp = TerminalHelpers.PollResize(viewport, scheduler);
                if (newVp != viewport)
                {
                    viewport = newVp;
                    W = Math.Max(1, viewport.Width);
                    H = Math.Max(1, viewport.Height);
                    grid = new int[W * H];
                    next = new int[W * H];
                    Seed();
                    dirty = true;
                }

                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape) { running = false; break; }
                    char c = char.ToLowerInvariant(k.KeyChar);
                    if (k.Key == ConsoleKey.Spacebar || c == ' ') { paused = !paused; dirty = true; }
                    else if (c == 'q') { running = false; break; }
                    else if (c == 'r') { Seed(); dirty = true; }
                    else if (c == 'm') { NextMode(); lastModeSwitchMs = Environment.TickCount64; }
                    else if (c == 'a') { autoRotate = !autoRotate; dirty = true; }
                    else if (c == 'h') { showFooter = !showFooter; dirty = true; }
                    else if (c == '<' || c == ',') CyclePalette(-1);
                    else if (c == '>' || c == '.') CyclePalette(+1);
                    else if (c == '+' || c == '=' || k.Key == ConsoleKey.OemPlus || k.Key == ConsoleKey.Add)
                        stepDelayMs = Math.Max(10, stepDelayMs - 10);
                    else if (c == '-' || c == '_' || k.Key == ConsoleKey.OemMinus || k.Key == ConsoleKey.Subtract)
                        stepDelayMs = Math.Min(500, stepDelayMs + 10);
                    else if (k.Key == ConsoleKey.F2) hud.Enabled = !hud.Enabled;
                }
                if (!running) break;

                now = Environment.TickCount64;
                if (autoRotate && now - lastModeSwitchMs >= AutoRotateMs)
                {
                    NextMode();
                    lastModeSwitchMs = now;
                }

                if (!paused && now - lastStepMs >= stepDelayMs)
                {
                    lastStepMs = now;
                    Step();
                    dirty = true;
                    var kind = Rules[ruleIndex].Kind;
                    if ((kind == Kind.Cyclic || kind == Kind.Greenberg) && now - lastPerturbMs >= 4000)
                    {
                        Perturb();
                        lastPerturbMs = now;
                    }
                }

                // While a palette cross-fade or its toast is active, keep redrawing so the
                // blend animates frame-by-frame rather than only on simulation steps.
                bool fading = now - paletteFadeStartMs < PaletteFadeMs;
                bool toasting = now < paletteToastUntilMs;
                if (fading || toasting) dirty = true;

                if (dirty || cached is null)
                {
                    cached = BuildDisplayList();
                    dirty = false;
                }

                DL.DisplayList frame = cached;
                if (hud.Enabled)
                {
                    var overlay = new DL.DisplayListBuilder();
                    hud.ViewportCols = viewport.Width;
                    hud.ViewportRows = viewport.Height;
                    hud.Contribute(cached, overlay);
                    frame = Combine(cached, overlay.Build());
                }

                await scheduler.RenderOnceAsync(frame, viewport, caps, pty, CancellationToken.None);
                await Task.Delay(16);
            }
        }
        finally
        {
            Console.Write("\u001b[?7h\u001b[?25h\u001b[?1049l");
        }
    }

    private static DL.Rgb24 Lerp(DL.Rgb24 a, DL.Rgb24 b, double f)
    {
        return new DL.Rgb24(
            (byte)Math.Clamp(a.R + (b.R - a.R) * f, 0, 255),
            (byte)Math.Clamp(a.G + (b.G - a.G) * f, 0, 255),
            (byte)Math.Clamp(a.B + (b.B - a.B) * f, 0, 255));
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
