namespace Andy.Tui.Style;

/// <summary>
/// Ports of the most popular vim / terminal / TUI color schemes. Each theme is
/// built from its canonical core palette (backgrounds, foreground, the six ANSI
/// hues, and an accent) which is mapped onto the library's <see cref="ThemeToken"/>
/// roles by <see cref="Build"/>, so role assignment stays consistent across themes.
/// </summary>
public static class PopularThemes
{
    private static RgbaColor H(string hex)
    {
        // "#rrggbb" or "rrggbb"
        int o = hex.Length > 0 && hex[0] == '#' ? 1 : 0;
        byte r = Convert.ToByte(hex.Substring(o, 2), 16);
        byte g = Convert.ToByte(hex.Substring(o + 2, 2), 16);
        byte b = Convert.ToByte(hex.Substring(o + 4, 2), 16);
        return RgbaColor.FromRgb(r, g, b);
    }

    // Core palette: bg = base background, bg1 = raised surface, bg2 = overlay/border
    // level, sel = selection/active highlight, fg = primary text, dim = muted/comment,
    // accent + the six ANSI hues (red/green/yellow/blue/magenta/cyan).
    private static Theme Build(string name,
        string bg, string bg1, string bg2, string sel, string fg, string dim, string accent,
        string red, string green, string yellow, string blue, string magenta, string cyan)
        => new(name, new Dictionary<ThemeToken, RgbaColor>
        {
            [ThemeToken.Background] = H(bg),
            [ThemeToken.Surface] = H(bg1),
            [ThemeToken.SurfaceSunken] = H(bg),
            [ThemeToken.SurfaceHover] = H(bg2),
            [ThemeToken.SurfaceActive] = H(sel),
            [ThemeToken.SurfaceDisabled] = H(bg1),
            [ThemeToken.SurfaceSelected] = H(sel),
            [ThemeToken.Foreground] = H(fg),
            [ThemeToken.ForegroundMuted] = H(dim),
            [ThemeToken.ForegroundDisabled] = H(dim),
            [ThemeToken.Accent] = H(accent),
            [ThemeToken.AccentForeground] = H(bg),
            [ThemeToken.Border] = H(bg2),
            [ThemeToken.BorderFocus] = H(accent),
            [ThemeToken.Success] = H(green),
            [ThemeToken.Warning] = H(yellow),
            [ThemeToken.Error] = H(red),
            [ThemeToken.Info] = H(blue),
            [ThemeToken.SyntaxKeyword] = H(magenta),
            [ThemeToken.SyntaxComment] = H(dim),
            [ThemeToken.SyntaxString] = H(green),
            [ThemeToken.SyntaxNumber] = H(yellow),
            [ThemeToken.SyntaxPreproc] = H(cyan),
        });

    //                                  name              bg        bg1       bg2       sel       fg        dim       accent    red       green     yellow    blue      magenta   cyan
    public static Theme GruvboxDark = Build("gruvbox-dark", "282828", "3c3836", "504945", "665c54", "ebdbb2", "928374", "fe8019", "fb4934", "b8bb26", "fabd2f", "83a598", "d3869b", "8ec07c");
    public static Theme GruvboxLight = Build("gruvbox-light", "fbf1c7", "ebdbb2", "d5c4a1", "bdae93", "3c3836", "7c6f64", "af3a03", "9d0006", "79740e", "b57614", "076678", "8f3f71", "427b58");
    public static Theme SolarizedDark = Build("solarized-dark", "002b36", "073642", "586e75", "094f5e", "839496", "586e75", "268bd2", "dc322f", "859900", "b58900", "268bd2", "d33682", "2aa198");
    public static Theme SolarizedLight = Build("solarized-light", "fdf6e3", "eee8d5", "93a1a1", "d9d2bf", "657b83", "93a1a1", "268bd2", "dc322f", "859900", "b58900", "268bd2", "d33682", "2aa198");
    public static Theme Dracula = Build("dracula", "282a36", "343746", "44475a", "44475a", "f8f8f2", "6272a4", "bd93f9", "ff5555", "50fa7b", "f1fa8c", "8be9fd", "ff79c6", "8be9fd");
    public static Theme Nord = Build("nord", "2e3440", "3b4252", "434c5e", "4c566a", "d8dee9", "616e88", "88c0d0", "bf616a", "a3be8c", "ebcb8b", "81a1c1", "b48ead", "8fbcbb");
    public static Theme Monokai = Build("monokai", "272822", "3e3d32", "49483e", "49483e", "f8f8f2", "75715e", "f92672", "f92672", "a6e22e", "e6db74", "66d9ef", "ae81ff", "66d9ef");
    public static Theme OneDark = Build("one-dark", "282c34", "2c313c", "3b4048", "3e4451", "abb2bf", "5c6370", "61afef", "e06c75", "98c379", "e5c07b", "61afef", "c678dd", "56b6c2");
    public static Theme OneLight = Build("one-light", "fafafa", "f0f0f0", "e5e5e6", "dbdbdc", "383a42", "a0a1a7", "4078f2", "e45649", "50a14f", "c18401", "4078f2", "a626a4", "0184bc");
    public static Theme TokyoNight = Build("tokyo-night", "1a1b26", "1f2335", "292e42", "33467c", "c0caf5", "565f89", "7aa2f7", "f7768e", "9ece6a", "e0af68", "7aa2f7", "bb9af7", "7dcfff");
    public static Theme TokyoNightStorm = Build("tokyo-night-storm", "24283b", "1f2335", "292e42", "2e3c64", "c0caf5", "565f89", "7aa2f7", "f7768e", "9ece6a", "e0af68", "7aa2f7", "bb9af7", "7dcfff");
    public static Theme TokyoNightDay = Build("tokyo-night-day", "e1e2e7", "d5d6db", "c4c8da", "b7c1e3", "343b58", "848cb5", "2e7de9", "f52a65", "587539", "8c6c3e", "2e7de9", "9854f1", "007197");
    public static Theme CatppuccinMocha = Build("catppuccin-mocha", "1e1e2e", "313244", "45475a", "585b70", "cdd6f4", "6c7086", "cba6f7", "f38ba8", "a6e3a1", "f9e2af", "89b4fa", "cba6f7", "94e2d5");
    public static Theme CatppuccinMacchiato = Build("catppuccin-macchiato", "24273a", "363a4f", "494d64", "5b6078", "cad3f5", "6e738d", "c6a0f6", "ed8796", "a6da95", "eed49f", "8aadf4", "c6a0f6", "8bd5ca");
    public static Theme CatppuccinFrappe = Build("catppuccin-frappe", "303446", "414559", "51576d", "626880", "c6d0f5", "737994", "ca9ee6", "e78284", "a6d189", "e5c890", "8caaee", "ca9ee6", "81c8be");
    public static Theme CatppuccinLatte = Build("catppuccin-latte", "eff1f5", "e6e9ef", "dce0e8", "bcc0cc", "4c4f69", "8c8fa1", "1e66f5", "d20f39", "40a02b", "df8e1d", "1e66f5", "8839ef", "179299");
    public static Theme TomorrowNight = Build("tomorrow-night", "1d1f21", "282a2e", "373b41", "373b41", "c5c8c6", "969896", "81a2be", "cc6666", "b5bd68", "f0c674", "81a2be", "b294bb", "8abeb7");
    public static Theme Material = Build("material", "263238", "2e3c43", "314549", "425b67", "eeffff", "546e7a", "82aaff", "f07178", "c3e88d", "ffcb6b", "82aaff", "c792ea", "89ddff");
    public static Theme Palenight = Build("palenight", "292d3e", "32374d", "444267", "444267", "a6accd", "676e95", "c792ea", "f07178", "c3e88d", "ffcb6b", "82aaff", "c792ea", "89ddff");
    public static Theme EverforestDark = Build("everforest-dark", "2d353b", "343f44", "3d484d", "475258", "d3c6aa", "859289", "a7c080", "e67e80", "a7c080", "dbbc7f", "7fbbb3", "d699b6", "83c092");
    public static Theme EverforestLight = Build("everforest-light", "fdf6e3", "f4f0d9", "efebd4", "e6e2cc", "5c6a72", "939f91", "8da101", "f85552", "8da101", "dfa000", "3a94c5", "df69ba", "35a77c");
    public static Theme RosePine = Build("rose-pine", "191724", "1f1d2e", "26233a", "403d52", "e0def4", "908caa", "c4a7e7", "eb6f92", "31748f", "f6c177", "9ccfd8", "c4a7e7", "9ccfd8");
    public static Theme RosePineMoon = Build("rose-pine-moon", "232136", "2a273f", "393552", "44415a", "e0def4", "908caa", "c4a7e7", "eb6f92", "3e8fb0", "f6c177", "9ccfd8", "c4a7e7", "9ccfd8");
    public static Theme RosePineDawn = Build("rose-pine-dawn", "faf4ed", "fffaf3", "f2e9e1", "dfdad9", "575279", "9893a5", "907aa9", "b4637a", "286983", "ea9d34", "56949f", "907aa9", "56949f");
    public static Theme Kanagawa = Build("kanagawa", "1f1f28", "2a2a37", "363646", "2d4f67", "dcd7ba", "727169", "7e9cd8", "c34043", "76946a", "dca561", "7e9cd8", "957fb8", "7aa89f");
    public static Theme AyuDark = Build("ayu-dark", "0a0e14", "0d1016", "1c212b", "1d2433", "b3b1ad", "5c6773", "e6b450", "f07178", "c2d94c", "ffb454", "59c2ff", "d2a6ff", "95e6cb");
    public static Theme AyuMirage = Build("ayu-mirage", "1f2430", "232834", "2d3340", "33415e", "cccac2", "5c6773", "ffcc66", "f28779", "bae67e", "ffd580", "73d0ff", "d4bfff", "95e6cb");
    public static Theme AyuLight = Build("ayu-light", "fafafa", "f3f4f5", "e7e8e9", "d1e4f4", "5c6166", "abb0b6", "fa8d3e", "f07171", "86b300", "f2ae49", "399ee6", "a37acc", "4cbf99");
    public static Theme Zenburn = Build("zenburn", "3f3f3f", "4f4f4f", "6f6f6f", "5f5f5f", "dcdccc", "7f9f7f", "f0dfaf", "cc9393", "7f9f7f", "f0dfaf", "8cd0d3", "dc8cc3", "93e0e3");
    public static Theme NightOwl = Build("night-owl", "011627", "0b2942", "1d3b53", "1d3b53", "d6deeb", "637777", "82aaff", "ef5350", "addb67", "ecc48d", "82aaff", "c792ea", "7fdbca");
    public static Theme OceanicNext = Build("oceanic-next", "1b2b34", "22313a", "343d46", "4f5b66", "cdd3de", "65737e", "6699cc", "ec5f67", "99c794", "fac863", "6699cc", "c594c5", "5fb3b3");
    public static Theme Cobalt2 = Build("cobalt2", "193549", "1f4662", "15232d", "0d3a58", "ffffff", "8aa0ad", "ffc600", "ff628c", "3ad900", "ffc600", "0088ff", "fb94ff", "80fcff");

    /// <summary>All 32 ported themes, in display order.</summary>
    public static IReadOnlyList<Theme> All { get; } = new[]
    {
        GruvboxDark, GruvboxLight, SolarizedDark, SolarizedLight, Dracula, Nord, Monokai,
        OneDark, OneLight, TokyoNight, TokyoNightStorm, TokyoNightDay,
        CatppuccinMocha, CatppuccinMacchiato, CatppuccinFrappe, CatppuccinLatte,
        TomorrowNight, Material, Palenight, EverforestDark, EverforestLight,
        RosePine, RosePineMoon, RosePineDawn, Kanagawa,
        AyuDark, AyuMirage, AyuLight, Zenburn, NightOwl, OceanicNext, Cobalt2,
    };
}
