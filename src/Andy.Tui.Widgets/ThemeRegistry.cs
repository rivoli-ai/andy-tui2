using System;
using System.Collections.Generic;
using ST = Andy.Tui.Style;

namespace Andy.Tui.Widgets;

/// <summary>
/// Routes runtime theme changes to live widgets. Registered <see cref="IThemeable"/>
/// widgets are re-seeded the moment <see cref="ST.ThemeContext.Set"/> changes the ambient
/// theme, so a single theme switch restyles an existing widget tree in the next frame
/// without the application reconstructing any of its state. Registrations are held via
/// weak references, so registering a widget never keeps it alive.
/// </summary>
public static class ThemeRegistry
{
    private static readonly object _gate = new();
    private static readonly List<WeakReference<IThemeable>> _widgets = new();

    static ThemeRegistry()
    {
        ST.ThemeContext.Changed += OnThemeChanged;
    }

    /// <summary>
    /// Track <paramref name="widget"/> so it is re-themed on every future ambient theme
    /// change, and apply the current theme to it immediately.
    /// </summary>
    public static void Register(IThemeable widget)
    {
        if (widget is null) throw new ArgumentNullException(nameof(widget));
        lock (_gate)
        {
            Prune();
            _widgets.Add(new WeakReference<IThemeable>(widget));
        }
        widget.ApplyTheme(ST.ThemeContext.Current);
    }

    /// <summary>Stop tracking <paramref name="widget"/>.</summary>
    public static void Unregister(IThemeable widget)
    {
        if (widget is null) return;
        lock (_gate)
        {
            _widgets.RemoveAll(wr => !wr.TryGetTarget(out var t) || ReferenceEquals(t, widget));
        }
    }

    /// <summary>Number of live (not garbage-collected) registrations.</summary>
    public static int Count
    {
        get { lock (_gate) { Prune(); return _widgets.Count; } }
    }

    /// <summary>Apply <paramref name="theme"/> to a batch of widgets in one pass.</summary>
    public static void Apply(ST.Theme theme, IEnumerable<IThemeable> widgets)
    {
        if (theme is null) throw new ArgumentNullException(nameof(theme));
        if (widgets is null) throw new ArgumentNullException(nameof(widgets));
        foreach (var w in widgets)
        {
            w?.ApplyTheme(theme);
        }
    }

    /// <summary>Apply <paramref name="theme"/> to the given widgets in one pass.</summary>
    public static void Apply(ST.Theme theme, params IThemeable[] widgets) =>
        Apply(theme, (IEnumerable<IThemeable>)widgets);

    private static void OnThemeChanged(ST.Theme theme)
    {
        List<IThemeable> live = new();
        lock (_gate)
        {
            Prune();
            foreach (var wr in _widgets)
            {
                if (wr.TryGetTarget(out var t)) live.Add(t);
            }
        }
        // Apply outside the lock so widget code can't deadlock on the registry.
        foreach (var w in live) w.ApplyTheme(theme);
    }

    private static void Prune() => _widgets.RemoveAll(wr => !wr.TryGetTarget(out _));
}
