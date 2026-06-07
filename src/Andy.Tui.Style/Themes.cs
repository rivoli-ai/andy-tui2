namespace Andy.Tui.Style;

/// <summary>
/// Central registry of every shipped theme: the <see cref="BuiltinThemes"/>
/// (Dark/Light/HighContrast) followed by the <see cref="PopularThemes"/> ports.
/// </summary>
public static class Themes
{
    /// <summary>All shipped themes, in display order (built-ins first).</summary>
    public static IReadOnlyList<Theme> All { get; } =
        BuiltinThemes.All.Concat(PopularThemes.All).ToArray();

    /// <summary>Look up any shipped theme by case-insensitive name; null if unknown.</summary>
    public static Theme? ByName(string name) =>
        All.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
}
