using Andy.Tui.DisplayList;

namespace Andy.Tui.Style;

/// <summary>
/// A named palette mapping <see cref="ThemeToken"/> semantic roles to colors.
/// A theme is immutable; derive variants with <see cref="With"/>.
/// </summary>
public sealed class Theme
{
    private readonly IReadOnlyDictionary<ThemeToken, RgbaColor> _colors;

    public string Name { get; }

    public Theme(string name, IReadOnlyDictionary<ThemeToken, RgbaColor> colors)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _colors = colors ?? throw new ArgumentNullException(nameof(colors));
    }

    /// <summary>
    /// The color for <paramref name="token"/>, or <see cref="RgbaColor.Transparent"/>
    /// (which renders as the terminal default) when the theme leaves it undefined.
    /// </summary>
    public RgbaColor Get(ThemeToken token) =>
        _colors.TryGetValue(token, out var c) ? c : RgbaColor.Transparent;

    /// <summary>True when the theme explicitly defines <paramref name="token"/>.</summary>
    public bool Has(ThemeToken token) => _colors.ContainsKey(token);

    /// <summary>
    /// Render-layer convenience for the widget path. Returns the token's opaque
    /// <see cref="Rgb24"/>, or <paramref name="fallback"/> when the token is undefined
    /// or transparent (so a transparent token preserves the widget's historic color).
    /// </summary>
    public Rgb24 GetRgb(ThemeToken token, Rgb24 fallback) => Get(token).ToRgb24() ?? fallback;

    /// <summary>Derive a new theme with one token overridden.</summary>
    public Theme With(ThemeToken token, RgbaColor color)
    {
        var copy = new Dictionary<ThemeToken, RgbaColor>(_colors) { [token] = color };
        return new Theme(Name, copy);
    }

    /// <summary>Derive a new theme renamed but otherwise identical.</summary>
    public Theme Rename(string name) => new(name, _colors);
}
