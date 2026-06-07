namespace Andy.Tui.Style;

/// <summary>
/// Ambient theme used by widgets that aren't handed a theme explicitly. Widgets
/// read <see cref="Current"/> when constructed, so set the theme before building
/// the widget tree. To switch themes at runtime, call <see cref="Set"/> and rebuild
/// the affected widgets (TUI re-renders are cheap); <see cref="Changed"/> fires to
/// let an app trigger that rebuild.
/// </summary>
public static class ThemeContext
{
    private static Theme _current = BuiltinThemes.Dark;

    /// <summary>The active ambient theme. Defaults to <see cref="BuiltinThemes.Dark"/>.</summary>
    public static Theme Current => _current;

    /// <summary>Raised after <see cref="Set"/> changes the active theme.</summary>
    public static event Action<Theme>? Changed;

    /// <summary>Set the active theme and notify <see cref="Changed"/> subscribers.</summary>
    public static void Set(Theme theme)
    {
        _current = theme ?? throw new ArgumentNullException(nameof(theme));
        Changed?.Invoke(_current);
    }
}
