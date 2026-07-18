using ST = Andy.Tui.Style;

namespace Andy.Tui.Widgets;

/// <summary>
/// Implemented by widgets whose palette is derived from a <see cref="ST.Theme"/>.
/// Calling <see cref="ApplyTheme"/> re-seeds the widget's colors from the given theme
/// in place, so a runtime theme switch restyles an existing widget tree without
/// reconstructing it. Widgets seed from <see cref="ST.ThemeContext.Current"/> at
/// construction; register the instance with <see cref="ThemeRegistry"/> to have it
/// re-seeded automatically whenever the ambient theme changes.
/// </summary>
public interface IThemeable
{
    /// <summary>Re-seed this widget's palette from <paramref name="theme"/> in place.</summary>
    void ApplyTheme(ST.Theme theme);
}
