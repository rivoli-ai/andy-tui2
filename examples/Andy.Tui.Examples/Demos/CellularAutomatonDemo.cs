using System;
using System.Collections.Generic;
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
/// Stays perpetually "active" (never redrawing the same glyph in the same place on a
/// regular cadence) via rules that don't settle, 30s auto-rotation through a rule
/// table, periodic perturbation, and reseed-on-stagnation.
///
/// Rules: Cyclic CA, Brian's Brain, Life (B3/S23), HighLife, Day &amp; Night, Seeds,
/// Maze, Forest Fire, Greenberg-Hastings (excitable waves), Langton's Ant, and
/// elementary 1-D rules (30/90/110/184). Life can be seeded with famous patterns
/// (Gosper glider gun, pulsars, acorn, R-pentomino, glider fleet) — cycle with P.
///
/// Glyphs come from a selectable theme (cycle with [ / ]): circle/ block/ star ramps,
/// plus emoji themes (Forest, Plants, Farm, Wild, Birds, Construction, Computers).
/// Emoji themes render on a double-width grid so they stay aligned.
///
/// Color comes from a selectable palette (cycle with &lt; / &gt;): smooth gradients
/// that blend bands, with a cross-fade on switch and a centered name toast.
/// </summary>
public static class CellularAutomatonDemo
{
    private enum Kind { Cyclic, BriansBrain, LifeFamily, ForestFire, Greenberg, LangtonAnt, Elementary, ReactionDiffusion }

    private sealed class Rule
    {
        public string Name = "";
        public Kind Kind;
        public int Param;                     // Elementary: 8-bit rule number
        public bool[] Birth = new bool[9];    // LifeFamily
        public bool[] Survive = new bool[9];

        public static Rule Simple(string name, Kind kind) => new() { Name = name, Kind = kind };
        public static Rule Elem(string name, int rule) => new() { Name = name, Kind = Kind.Elementary, Param = rule };

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
        Rule.Simple("Langton's Ant", Kind.LangtonAnt),
        Rule.Elem("Rule 30", 30),
        Rule.Elem("Rule 90 (Sierpinski)", 90),
        Rule.Elem("Rule 110", 110),
        Rule.Elem("Rule 184 (traffic)", 184),
        Rule.Simple("Reaction-Diffusion", Kind.ReactionDiffusion),
    };

    private sealed class GlyphTheme
    {
        public string Name { get; }
        public int CellW { get; }           // terminal columns per cell (1 or 2 for emoji)
        public string[] Ramp { get; }       // ordered faint -> bold / small -> large
        public string AntGlyph { get; }
        public GlyphTheme(string name, int cellW, string antGlyph, params string[] ramp)
        { Name = name; CellW = cellW; AntGlyph = antGlyph; Ramp = ramp; }
    }

    private static readonly GlyphTheme[] GlyphThemes =
    {
        new GlyphTheme("Circles", 1, "✦", "·", "∘", "○", "◌", "◍", "◎", "◉", "●"),
        new GlyphTheme("Blocks", 1, "✦", "▁", "▂", "▃", "▄", "▅", "▆", "▇", "█"),
        new GlyphTheme("Stars", 1, "✶", "·", "⋆", "✦", "✧", "★", "✶", "✷", "✹"),
        new GlyphTheme("Forest", 2, "🐜", "🌱", "🌿", "🍀", "🌾", "🌳", "🌲", "🎋", "🪴"),
        new GlyphTheme("Plants", 2, "🐝", "🌷", "🌸", "🌹", "🌺", "🌻", "🌼", "💐", "🪷"),
        new GlyphTheme("Farm", 2, "🐕", "🐤", "🐣", "🐔", "🐓", "🐑", "🐐", "🐖", "🐄"),
        new GlyphTheme("Wild", 2, "🐾", "🦊", "🐺", "🦌", "🐻", "🦁", "🐘", "🦏", "🐅"),
        new GlyphTheme("Birds", 2, "🪶", "🐦", "🐧", "🦆", "🦢", "🦅", "🦉", "🦜", "🐓"),
        new GlyphTheme("Construction", 2, "🚧", "🧱", "🔩", "🔧", "🔨", "🚧", "🚜", "🏭", "🪨"),
        new GlyphTheme("Computers", 2, "🔌", "💾", "💻", "💿", "📀", "🔌", "🔋", "📡", "📟"),
        new GlyphTheme("Moon", 2, "🌝", "🌑", "🌒", "🌓", "🌔", "🌕", "🌖", "🌗", "🌘"),
        new GlyphTheme("Sea", 2, "🦈", "🐟", "🐠", "🐡", "🦑", "🐙", "🦀", "🐚", "🐳"),
        new GlyphTheme("Food", 2, "🍒", "🍎", "🍊", "🍋", "🍏", "🍐", "🍓", "🫐", "🍇"),
        new GlyphTheme("Bugs", 2, "🐝", "🐛", "🐜", "🐝", "🦋", "🐞", "🦗", "🦟", "🐌"),
        new GlyphTheme("Weather", 1, "✦", "·", "☁", "☂", "☃", "❄", "☀", "★", "☄"),
    };

    private sealed class LifePattern
    {
        public string Name { get; }
        public string[]? Rows { get; }   // null = random soup
        public bool Tile { get; }
        public int Copies { get; }
        public LifePattern(string name, string[]? rows, bool tile, int copies)
        { Name = name; Rows = rows; Tile = tile; Copies = copies; }
    }

    private static readonly LifePattern[] LifeSeeds =
    {
        new LifePattern("Soup", null, false, 0),
        new LifePattern("Glider Gun", new[]
        {
            "........................O...........",
            "......................O.O...........",
            "............OO......OO............OO",
            "...........O...O....OO............OO",
            "OO........O.....O...OO..............",
            "OO........O...O.OO....O.O...........",
            "..........O.....O.......O...........",
            "...........O...O....................",
            "............OO......................",
        }, false, 2),
        new LifePattern("Pulsars", new[]
        {
            "..OOO...OOO..",
            ".............",
            "O....O.O....O",
            "O....O.O....O",
            "O....O.O....O",
            "..OOO...OOO..",
            ".............",
            "..OOO...OOO..",
            "O....O.O....O",
            "O....O.O....O",
            "O....O.O....O",
            ".............",
            "..OOO...OOO..",
        }, true, 0),
        new LifePattern("Glider Fleet", new[] { ".O.", "..O", "OOO" }, false, 14),
        new LifePattern("Spaceships", new[] { ".OOOO", "O...O", "....O", "O..O." }, false, 8),
        new LifePattern("Acorn", new[] { ".O.....", "...O...", "OO..OOO" }, false, 3),
        new LifePattern("R-pentomino", new[] { ".OO", "OO.", ".O." }, false, 4),
    };

    // Brian's Brain / Forest Fire cell states.
    private const int Off = 0, On = 1, Dying = 2;

    private const long AutoRotateMs = 30_000;
    private const int PaletteFadeMs = 600;
    private const long ToastMs = 1_500;
    private const double FireGrow = 0.012;
    private const double FireLightning = 6e-5;

    private sealed class Palette
    {
        public string Name { get; }
        private readonly DL.Rgb24[] _c;
        public Palette(string name, params DL.Rgb24[] colors) { Name = name; _c = colors; }

        public DL.Rgb24 At(double t)
        {
            t -= Math.Floor(t);
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

    public static async Task Run((int Width, int Height) viewport, TerminalCapabilities caps, bool screensaver = false)
    {
        var scheduler = new Andy.Tui.Core.FrameScheduler();
        var hud = new Andy.Tui.Observability.HudOverlay { Enabled = false };
        scheduler.SetMetricsSink(hud);
        var pty = new StdoutPty();

        var rng = new Random();

        int W = Math.Max(1, viewport.Width);
        int H = Math.Max(1, viewport.Height);

        int ruleIndex = 0;
        int themeIndex = 0;
        int lifeSeedIndex = 0;
        int states = 12;     // cyclic-CA states
        int threshold = 1;   // cyclic-CA threshold
        int ghStates = 8;    // Greenberg-Hastings phases

        int cellW = GlyphThemes[themeIndex].CellW;
        int gw = Math.Max(1, W / cellW), gh = Math.Max(1, H);
        int[] grid = new int[gw * gh];
        int[] next = new int[gw * gh];
        // Reaction-Diffusion (Gray-Scott) chemical fields.
        float[] rdU = new float[gw * gh], rdV = new float[gw * gh], rdU2 = new float[gw * gh], rdV2 = new float[gw * gh];
        var ants = new List<(int x, int y, int dir)>();
        int gen = 0; // counter for Langton trails / elementary generations

        bool paused = false;
        bool showFooter = !screensaver;
        bool autoRotate = true;
        int stepDelayMs = 70;

        long now = Environment.TickCount64;
        long startMs = now, lastStepMs = now, lastModeSwitchMs = now, lastPerturbMs = now;

        int paletteIndex = 0, prevPaletteIndex = 0;
        long paletteFadeStartMs = now - PaletteFadeMs * 2;
        long toastUntilMs = 0;
        string toastText = "";

        int stagnantSteps = 0, lastPop = -1;
        bool dirty = true;

        void Toast(string text) { toastText = text; toastUntilMs = Environment.TickCount64 + ToastMs; dirty = true; }

        void PlaceLifeSeed()
        {
            Array.Clear(grid, 0, grid.Length);
            var p = LifeSeeds[lifeSeedIndex];
            if (p.Rows is null)
            {
                for (int i = 0; i < grid.Length; i++) grid[i] = rng.Next(100) < 30 ? 1 : 0;
                return;
            }
            int ph = p.Rows.Length, pw = 0;
            foreach (var row in p.Rows) pw = Math.Max(pw, row.Length);

            void Stamp(int ox, int oy)
            {
                for (int ry = 0; ry < ph; ry++)
                    for (int rx = 0; rx < p.Rows[ry].Length; rx++)
                        if (p.Rows[ry][rx] == 'O')
                        {
                            int gx = ox + rx, gy = oy + ry;
                            if (gx >= 0 && gx < gw && gy >= 0 && gy < gh) grid[gy * gw + gx] = 1;
                        }
            }

            if (p.Tile)
                for (int oy = 1; oy < gh; oy += ph + 2)
                    for (int ox = 1; ox < gw; ox += pw + 2)
                        Stamp(ox, oy);
            else
                for (int k = 0; k < p.Copies; k++)
                    Stamp(rng.Next(Math.Max(1, gw - pw)), rng.Next(Math.Max(1, gh - ph)));
        }

        void Seed()
        {
            var kind = Rules[ruleIndex].Kind;
            switch (kind)
            {
                case Kind.LifeFamily:
                    PlaceLifeSeed();
                    break;
                case Kind.Cyclic:
                    for (int i = 0; i < grid.Length; i++) grid[i] = rng.Next(states);
                    break;
                case Kind.BriansBrain:
                    for (int i = 0; i < grid.Length; i++) grid[i] = rng.Next(3) == 0 ? On : Off;
                    break;
                case Kind.ForestFire:
                    for (int i = 0; i < grid.Length; i++) grid[i] = rng.Next(100) < 55 ? On : Off;
                    break;
                case Kind.Greenberg:
                    for (int i = 0; i < grid.Length; i++) grid[i] = rng.Next(100) < 15 ? rng.Next(1, ghStates) : 0;
                    break;
                case Kind.LangtonAnt:
                    Array.Clear(grid, 0, grid.Length);
                    ants.Clear();
                    int nAnts = Math.Max(2, (gw * gh) / 3000);
                    for (int a = 0; a < nAnts; a++) ants.Add((rng.Next(gw), rng.Next(gh), rng.Next(4)));
                    gen = 0;
                    break;
                case Kind.Elementary:
                    Array.Clear(grid, 0, grid.Length);
                    gen = 0;
                    grid[(gh - 1) * gw + gw / 2] = ++gen; // single seed on the bottom row
                    break;
                case Kind.ReactionDiffusion:
                    for (int i = 0; i < rdU.Length; i++) { rdU[i] = 1f; rdV[i] = 0f; }
                    int blobs = grid.Length / 400 + 8;
                    for (int bI = 0; bI < blobs; bI++)
                    {
                        int cx = rng.Next(gw), cy = rng.Next(gh);
                        for (int dy = -2; dy <= 2; dy++)
                            for (int dx = -2; dx <= 2; dx++)
                            {
                                int j = ((cy + dy + gh) % gh) * gw + (cx + dx + gw) % gw;
                                rdU[j] = 0.5f; rdV[j] = 0.9f;
                            }
                    }
                    break;
            }
            stagnantSteps = 0;
            lastPop = -1;
        }

        void Realloc()
        {
            cellW = GlyphThemes[themeIndex].CellW;
            gw = Math.Max(1, W / cellW);
            gh = Math.Max(1, H);
            grid = new int[gw * gh];
            next = new int[gw * gh];
            rdU = new float[gw * gh]; rdV = new float[gw * gh];
            rdU2 = new float[gw * gh]; rdV2 = new float[gw * gh];
            Seed();
            dirty = true;
        }

        void OnRuleChanged()
        {
            var kind = Rules[ruleIndex].Kind;
            if (kind == Kind.Cyclic) states = 8 + rng.Next(9);
            if (kind == Kind.Greenberg) ghStates = 6 + rng.Next(6);
            Seed();
            dirty = true;
            Toast(Rules[ruleIndex].Name);
        }

        void NextMode() { ruleIndex = (ruleIndex + 1) % Rules.Length; OnRuleChanged(); }

        void CyclePalette(int dir)
        {
            prevPaletteIndex = paletteIndex;
            paletteIndex = (paletteIndex + dir + Palettes.Length) % Palettes.Length;
            paletteFadeStartMs = Environment.TickCount64;
            Toast("Palette: " + Palettes[paletteIndex].Name);
        }

        void CycleTheme(int dir)
        {
            int old = GlyphThemes[themeIndex].CellW;
            themeIndex = (themeIndex + dir + GlyphThemes.Length) % GlyphThemes.Length;
            if (GlyphThemes[themeIndex].CellW != old) Realloc();
            Toast("Theme: " + GlyphThemes[themeIndex].Name);
        }

        void CycleSeed(int dir)
        {
            lifeSeedIndex = (lifeSeedIndex + dir + LifeSeeds.Length) % LifeSeeds.Length;
            if (Rules[ruleIndex].Kind == Kind.LifeFamily) Seed();
            Toast("Seed: " + LifeSeeds[lifeSeedIndex].Name);
        }

        void Perturb()
        {
            int n = grid.Length / 200 + 5;
            bool gh_ = Rules[ruleIndex].Kind == Kind.Greenberg;
            for (int k = 0; k < n; k++)
                grid[rng.Next(grid.Length)] = gh_ ? 1 : rng.Next(states);
        }

        int CountEq(int x, int y, int wanted)
        {
            int c = 0;
            for (int dy = -1; dy <= 1; dy++)
            {
                int ny = (y + dy + gh) % gh;
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = (x + dx + gw) % gw;
                    if (grid[ny * gw + nx] == wanted) c++;
                }
            }
            return c;
        }

        int CountAlive(int x, int y)
        {
            int c = 0;
            for (int dy = -1; dy <= 1; dy++)
            {
                int ny = (y + dy + gh) % gh;
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = (x + dx + gw) % gw;
                    if (grid[ny * gw + nx] > 0) c++;
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
                    for (int y = 0; y < gh; y++)
                        for (int x = 0; x < gw; x++)
                        {
                            int i = y * gw + x, cur = grid[i], want = (cur + 1) % states;
                            next[i] = CountEq(x, y, want) >= threshold ? want : cur;
                        }
                    break;

                case Kind.BriansBrain:
                    for (int y = 0; y < gh; y++)
                        for (int x = 0; x < gw; x++)
                        {
                            int i = y * gw + x;
                            next[i] = grid[i] switch
                            {
                                On => Dying,
                                Dying => Off,
                                _ => CountEq(x, y, On) == 2 ? On : Off,
                            };
                        }
                    break;

                case Kind.LifeFamily:
                {
                    int pop = 0;
                    for (int y = 0; y < gh; y++)
                        for (int x = 0; x < gw; x++)
                        {
                            int i = y * gw + x;
                            bool alive = grid[i] > 0;
                            int n = CountAlive(x, y);
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
                    for (int y = 0; y < gh; y++)
                        for (int x = 0; x < gw; x++)
                        {
                            int i = y * gw + x, cur = grid[i];
                            if (cur == Dying) next[i] = Off;
                            else if (cur == On)
                                next[i] = (CountEq(x, y, Dying) > 0 || rng.NextDouble() < FireLightning) ? Dying : On;
                            else next[i] = rng.NextDouble() < FireGrow ? On : Off;
                        }
                    break;

                case Kind.Greenberg:
                {
                    int active = 0;
                    for (int y = 0; y < gh; y++)
                        for (int x = 0; x < gw; x++)
                        {
                            int i = y * gw + x, cur = grid[i], nv;
                            if (cur == 0) nv = CountEq(x, y, 1) >= 1 ? 1 : 0;
                            else if (cur >= ghStates - 1) nv = 0;
                            else nv = cur + 1;
                            next[i] = nv;
                            if (nv > 0) active++;
                        }
                    if (active == 0) { Seed(); return; }
                    break;
                }

                case Kind.LangtonAnt:
                {
                    int sub = Math.Max(40, gw);
                    for (int s = 0; s < sub; s++)
                        for (int a = 0; a < ants.Count; a++)
                        {
                            var (ax, ay, dir) = ants[a];
                            int gi = ay * gw + ax;
                            if (grid[gi] > 0) { dir = (dir + 3) % 4; grid[gi] = 0; }       // on  -> turn left, erase
                            else { dir = (dir + 1) % 4; grid[gi] = ++gen; }                // off -> turn right, paint
                            switch (dir) { case 0: ay--; break; case 1: ax++; break; case 2: ay++; break; default: ax--; break; }
                            ax = (ax + gw) % gw; ay = (ay + gh) % gh;
                            ants[a] = (ax, ay, dir);
                        }
                    return; // mutates grid in place
                }

                case Kind.Elementary:
                {
                    if (gh < 2) return;
                    Array.Copy(grid, gw, grid, 0, (gh - 1) * gw); // scroll rows up by one
                    int prev = (gh - 2) * gw, bottom = (gh - 1) * gw;
                    gen++;
                    int on = 0;
                    for (int x = 0; x < gw; x++)
                    {
                        int l = grid[prev + (x - 1 + gw) % gw] > 0 ? 1 : 0;
                        int c = grid[prev + x] > 0 ? 1 : 0;
                        int r = grid[prev + (x + 1) % gw] > 0 ? 1 : 0;
                        int idx = (l << 2) | (c << 1) | r;
                        bool alive = ((rule.Param >> idx) & 1) != 0;
                        grid[bottom + x] = alive ? gen : 0;
                        if (alive) on++;
                    }
                    if (on == 0) Seed();
                    return; // mutates grid in place
                }

                case Kind.ReactionDiffusion:
                {
                    // Gray-Scott. "Mitosis" params keep dividing — never settles.
                    const float Du = 0.16f, Dv = 0.08f, F = 0.0367f, k = 0.0649f;
                    for (int iter = 0; iter < 8; iter++)
                    {
                        for (int y = 0; y < gh; y++)
                        {
                            int ym = ((y - 1 + gh) % gh) * gw, yp = ((y + 1) % gh) * gw, y0 = y * gw;
                            for (int x = 0; x < gw; x++)
                            {
                                int xm = (x - 1 + gw) % gw, xp = (x + 1) % gw;
                                float uu = rdU[y0 + x], vv = rdV[y0 + x];
                                float lapU = (rdU[y0 + xm] + rdU[y0 + xp] + rdU[ym + x] + rdU[yp + x]) * 0.2f
                                           + (rdU[ym + xm] + rdU[ym + xp] + rdU[yp + xm] + rdU[yp + xp]) * 0.05f - uu;
                                float lapV = (rdV[y0 + xm] + rdV[y0 + xp] + rdV[ym + x] + rdV[yp + x]) * 0.2f
                                           + (rdV[ym + xm] + rdV[ym + xp] + rdV[yp + xm] + rdV[yp + xp]) * 0.05f - vv;
                                float uvv = uu * vv * vv;
                                rdU2[y0 + x] = uu + (Du * lapU - uvv + F * (1f - uu));
                                rdV2[y0 + x] = vv + (Dv * lapV + uvv - (F + k) * vv);
                            }
                        }
                        var tu = rdU; rdU = rdU2; rdU2 = tu;
                        var tv = rdV; rdV = rdV2; rdV2 = tv;
                    }
                    return; // operates on float fields, not the int grid
                }
            }

            var tmp = grid; grid = next; next = tmp;
        }

        string Ramp(int idx)
        {
            var r = GlyphThemes[themeIndex].Ramp;
            if (idx < 0) idx = 0; else if (idx >= r.Length) idx = r.Length - 1;
            return r[idx];
        }

        (string? glyph, double t) CellVisual(int value, double rot)
        {
            int rampLen = GlyphThemes[themeIndex].Ramp.Length;
            switch (Rules[ruleIndex].Kind)
            {
                case Kind.Cyclic:
                    return (Ramp((int)((long)value * rampLen / Math.Max(1, states))), value / (double)states + rot);
                case Kind.BriansBrain:
                    return value switch
                    {
                        On => (Ramp(rampLen - 1), 0.82),
                        Dying => (Ramp(rampLen / 2), 0.42),
                        _ => (null, 0.0),
                    };
                case Kind.ForestFire:
                    return value switch
                    {
                        On => (Ramp(rampLen / 2), 0.45),
                        Dying => (Ramp(rampLen - 1), 0.95),
                        _ => (null, 0.0),
                    };
                case Kind.Greenberg:
                    if (value <= 0) return (null, 0.0);
                    return (Ramp((int)((long)value * rampLen / Math.Max(1, ghStates))), value / (double)ghStates);
                case Kind.LangtonAnt:
                    if (value <= 0) return (null, 0.0);
                    return (Ramp(value % rampLen), (value >> 3) * 0.03); // quantized color keeps runs batchable
                case Kind.Elementary:
                    if (value <= 0) return (null, 0.0);
                    return (Ramp(rampLen - 1), value * 0.02);
                case Kind.LifeFamily:
                default:
                    if (value <= 0) return (null, 0.0);
                    return (Ramp(Math.Min(value + 1, rampLen - 1)), 0.08 + value * 0.05);
            }
        }

        DL.DisplayList BuildDisplayList()
        {
            long tnow = Environment.TickCount64;
            var kind = Rules[ruleIndex].Kind;
            double rot = kind == Kind.Cyclic ? (tnow - startMs) / 8000.0 : 0.0;
            double fade = PaletteFadeMs <= 0 ? 1.0 : Math.Clamp((tnow - paletteFadeStartMs) / (double)PaletteFadeMs, 0.0, 1.0);

            DL.Rgb24 ColorAt(double t)
            {
                var cur = Palettes[paletteIndex].At(t);
                return fade >= 1.0 ? cur : Lerp(Palettes[prevPaletteIndex].At(t), cur, fade);
            }

            var b = new DL.DisplayListBuilder();
            b.PushClip(new DL.ClipPush(0, 0, W, H));
            b.DrawRect(new DL.Rect(0, 0, W, H, new DL.Rgb24(0, 0, 0)));

            var glyphs = new string?[gw];
            var fgs = new DL.Rgb24[gw];
            var sb = new StringBuilder();

            int rampLen = GlyphThemes[themeIndex].Ramp.Length;
            for (int cy = 0; cy < gh; cy++)
            {
                for (int cx = 0; cx < gw; cx++)
                {
                    string? g; double t;
                    if (kind == Kind.ReactionDiffusion)
                    {
                        float vv = rdV[cy * gw + cx];
                        if (vv < 0.12f) { g = null; t = 0.0; }
                        else { t = Math.Clamp(vv * 2.0, 0.0, 1.0); g = Ramp((int)(t * (rampLen - 1))); }
                    }
                    else (g, t) = CellVisual(grid[cy * gw + cx], rot);
                    glyphs[cx] = g;
                    fgs[cx] = g is null ? default : ColorAt(t);
                }

                int x2 = 0;
                while (x2 < gw)
                {
                    if (glyphs[x2] is null) { x2++; continue; }
                    int start = x2;
                    var color = fgs[x2];
                    sb.Clear();
                    while (x2 < gw && glyphs[x2] is string gg && fgs[x2].Equals(color))
                    {
                        sb.Append(gg);
                        x2++;
                    }
                    b.DrawText(new DL.TextRun(start * cellW, cy, sb.ToString(), color, null, DL.CellAttrFlags.None));
                }
            }

            // Langton ants on top.
            if (kind == Kind.LangtonAnt)
            {
                string antGlyph = GlyphThemes[themeIndex].AntGlyph;
                foreach (var (ax, ay, _) in ants)
                    b.DrawText(new DL.TextRun(ax * cellW, ay, antGlyph, new DL.Rgb24(255, 255, 255), null, DL.CellAttrFlags.Bold));
            }

            if (tnow < toastUntilMs && H >= 1)
            {
                string label = "‹ " + toastText + " ›";
                int tx = Math.Max(0, (W - label.Length) / 2);
                int ty = H / 2;
                b.DrawRect(new DL.Rect(Math.Max(0, tx - 1), ty, Math.Min(W, label.Length + 2), 1, new DL.Rgb24(22, 22, 30)));
                b.DrawText(new DL.TextRun(tx, ty, label, new DL.Rgb24(235, 235, 245), null, DL.CellAttrFlags.Bold));
            }

            if (showFooter && H >= 2)
            {
                string ruleName = kind == Kind.Cyclic ? $"Cyclic CA (states {states})" : Rules[ruleIndex].Name;
                string seedInfo = kind == Kind.LifeFamily ? $" · Seed {LifeSeeds[lifeSeedIndex].Name}" : "";
                string l1 = $" {ruleName}{(paused ? " [PAUSED]" : "")} · Palette {Palettes[paletteIndex].Name} · Theme {GlyphThemes[themeIndex].Name}{seedInfo}";
                string l2 = " M rule  < > palette  [ ] theme  P seed  Space pause  R reseed  A auto  +/- speed  H hide  ESC quit";
                if (l1.Length > W) l1 = l1.Substring(0, W);
                if (l2.Length > W) l2 = l2.Substring(0, W);
                b.DrawRect(new DL.Rect(0, H - 2, W, 2, new DL.Rgb24(12, 12, 18)));
                b.DrawText(new DL.TextRun(0, H - 2, l1, new DL.Rgb24(200, 200, 130), null, DL.CellAttrFlags.Bold));
                b.DrawText(new DL.TextRun(0, H - 1, l2, new DL.Rgb24(150, 150, 165), null, DL.CellAttrFlags.None));
            }

            b.Pop();
            return b.Build();
        }

        Realloc();

        Console.Write("\u001b[?1049h\u001b[?25l\u001b[?7l");
        try
        {
            DL.DisplayList? cached = null;
            bool running = true;
            while (running)
            {
                int cw = Console.WindowWidth, ch = Console.WindowHeight;
                if (cw != W || ch != H)
                {
                    scheduler.SetForceFullClear(true);
                    viewport = (cw, ch);
                    W = Math.Max(1, cw); H = Math.Max(1, ch);
                    Realloc();
                }
                else scheduler.SetForceFullClear(false);

                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape) { running = false; break; }
                    // In standalone screensaver mode, any key wakes/exits.
                    if (screensaver) { running = false; break; }
                    char c = char.ToLowerInvariant(k.KeyChar);
                    if (k.Key == ConsoleKey.Spacebar || c == ' ') { paused = !paused; dirty = true; }
                    else if (c == 'q') { running = false; break; }
                    else if (c == 'r') { Seed(); dirty = true; }
                    else if (c == 'm') { NextMode(); lastModeSwitchMs = Environment.TickCount64; }
                    else if (c == 'a') { autoRotate = !autoRotate; dirty = true; }
                    else if (c == 'h') { showFooter = !showFooter; dirty = true; }
                    else if (c == 'p') CycleSeed(+1);
                    else if (c == '<' || c == ',') CyclePalette(-1);
                    else if (c == '>' || c == '.') CyclePalette(+1);
                    else if (c == '[') CycleTheme(-1);
                    else if (c == ']') CycleTheme(+1);
                    else if (c == '+' || c == '=' || k.Key == ConsoleKey.OemPlus || k.Key == ConsoleKey.Add)
                        stepDelayMs = Math.Max(10, stepDelayMs - 10);
                    else if (c == '-' || c == '_' || k.Key == ConsoleKey.OemMinus || k.Key == ConsoleKey.Subtract)
                        stepDelayMs = Math.Min(500, stepDelayMs + 10);
                    else if (k.Key == ConsoleKey.F2) hud.Enabled = !hud.Enabled;
                }
                if (!running) break;

                now = Environment.TickCount64;
                if (autoRotate && now - lastModeSwitchMs >= AutoRotateMs) { NextMode(); lastModeSwitchMs = now; }

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

                bool fading = now - paletteFadeStartMs < PaletteFadeMs;
                bool toasting = now < toastUntilMs;
                if (fading || toasting) dirty = true;

                if (dirty || cached is null) { cached = BuildDisplayList(); dirty = false; }

                DL.DisplayList frame = cached;
                if (hud.Enabled)
                {
                    var overlay = new DL.DisplayListBuilder();
                    hud.ViewportCols = viewport.Width; hud.ViewportRows = viewport.Height;
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
