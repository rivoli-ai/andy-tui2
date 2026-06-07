namespace Andy.Tui.Style;

/// <summary>
/// Built-in themes shipped with the library. <see cref="Dark"/> reproduces the
/// historic hardcoded widget palette exactly, so adopting it is a visual no-op.
/// </summary>
public static class BuiltinThemes
{
    private static RgbaColor C(byte r, byte g, byte b) => RgbaColor.FromRgb(r, g, b);

    public static Theme Dark { get; } = new("dark", new Dictionary<ThemeToken, RgbaColor>
    {
        [ThemeToken.Background] = C(12, 12, 12),
        [ThemeToken.Surface] = C(40, 40, 40),
        [ThemeToken.SurfaceSunken] = C(20, 20, 20),
        [ThemeToken.SurfaceHover] = C(55, 55, 55),
        [ThemeToken.SurfaceActive] = C(80, 80, 120),
        [ThemeToken.SurfaceDisabled] = C(30, 30, 30),
        [ThemeToken.SurfaceSelected] = C(60, 60, 100),
        [ThemeToken.Foreground] = C(220, 220, 220),
        [ThemeToken.ForegroundMuted] = C(150, 150, 150),
        [ThemeToken.ForegroundDisabled] = C(100, 100, 100),
        [ThemeToken.Accent] = C(90, 120, 255),
        [ThemeToken.AccentForeground] = C(255, 255, 255),
        [ThemeToken.Border] = C(100, 100, 100),
        [ThemeToken.BorderFocus] = C(130, 160, 255),
        [ThemeToken.Success] = C(60, 120, 70),
        [ThemeToken.Warning] = C(200, 160, 60),
        [ThemeToken.Error] = C(200, 70, 70),
        [ThemeToken.Info] = C(60, 140, 220),
        [ThemeToken.SyntaxKeyword] = C(200, 120, 220),
        [ThemeToken.SyntaxComment] = C(110, 130, 110),
        [ThemeToken.SyntaxString] = C(200, 170, 110),
        [ThemeToken.SyntaxNumber] = C(180, 200, 140),
        [ThemeToken.SyntaxPreproc] = C(150, 150, 200),
    });

    public static Theme Light { get; } = new("light", new Dictionary<ThemeToken, RgbaColor>
    {
        [ThemeToken.Background] = C(245, 245, 245),
        [ThemeToken.Surface] = C(225, 225, 225),
        [ThemeToken.SurfaceSunken] = C(235, 235, 235),
        [ThemeToken.SurfaceHover] = C(210, 210, 210),
        [ThemeToken.SurfaceActive] = C(180, 190, 230),
        [ThemeToken.SurfaceDisabled] = C(230, 230, 230),
        [ThemeToken.SurfaceSelected] = C(190, 205, 245),
        [ThemeToken.Foreground] = C(30, 30, 30),
        [ThemeToken.ForegroundMuted] = C(90, 90, 90),
        [ThemeToken.ForegroundDisabled] = C(150, 150, 150),
        [ThemeToken.Accent] = C(40, 90, 220),
        [ThemeToken.AccentForeground] = C(255, 255, 255),
        [ThemeToken.Border] = C(160, 160, 160),
        [ThemeToken.BorderFocus] = C(40, 90, 220),
        [ThemeToken.Success] = C(40, 140, 70),
        [ThemeToken.Warning] = C(180, 130, 20),
        [ThemeToken.Error] = C(190, 50, 50),
        [ThemeToken.Info] = C(40, 110, 190),
        [ThemeToken.SyntaxKeyword] = C(150, 40, 170),
        [ThemeToken.SyntaxComment] = C(110, 130, 110),
        [ThemeToken.SyntaxString] = C(160, 90, 30),
        [ThemeToken.SyntaxNumber] = C(80, 120, 40),
        [ThemeToken.SyntaxPreproc] = C(80, 80, 150),
    });

    /// <summary>High-contrast variant for accessibility (pure black/white emphasis).</summary>
    public static Theme HighContrast { get; } = new("high-contrast", new Dictionary<ThemeToken, RgbaColor>
    {
        [ThemeToken.Background] = C(0, 0, 0),
        [ThemeToken.Surface] = C(0, 0, 0),
        [ThemeToken.SurfaceSunken] = C(0, 0, 0),
        [ThemeToken.SurfaceHover] = C(40, 40, 40),
        [ThemeToken.SurfaceActive] = C(0, 60, 120),
        [ThemeToken.SurfaceDisabled] = C(20, 20, 20),
        [ThemeToken.SurfaceSelected] = C(0, 80, 160),
        [ThemeToken.Foreground] = C(255, 255, 255),
        [ThemeToken.ForegroundMuted] = C(200, 200, 200),
        [ThemeToken.ForegroundDisabled] = C(120, 120, 120),
        [ThemeToken.Accent] = C(255, 255, 0),
        [ThemeToken.AccentForeground] = C(0, 0, 0),
        [ThemeToken.Border] = C(255, 255, 255),
        [ThemeToken.BorderFocus] = C(255, 255, 0),
        [ThemeToken.Success] = C(0, 255, 0),
        [ThemeToken.Warning] = C(255, 200, 0),
        [ThemeToken.Error] = C(255, 60, 60),
        [ThemeToken.Info] = C(0, 200, 255),
        [ThemeToken.SyntaxKeyword] = C(255, 255, 0),
        [ThemeToken.SyntaxComment] = C(150, 150, 150),
        [ThemeToken.SyntaxString] = C(0, 255, 0),
        [ThemeToken.SyntaxNumber] = C(0, 255, 255),
        [ThemeToken.SyntaxPreproc] = C(255, 150, 255),
    });

    public static IReadOnlyList<Theme> All { get; } = new[] { Dark, Light, HighContrast };

    /// <summary>Look up a built-in theme by case-insensitive name; null if unknown.</summary>
    public static Theme? ByName(string name) =>
        All.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
}
