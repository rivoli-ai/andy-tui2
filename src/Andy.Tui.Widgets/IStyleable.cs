using ST = Andy.Tui.Style;

namespace Andy.Tui.Widgets;

/// <summary>
/// Implemented by widgets that can be driven by a computed <see cref="ST.ResolvedStyle"/>
/// (produced by <see cref="ST.StyleResolver"/> after applying the CSS cascade — type,
/// id, class, and pseudo-state rules). <see cref="ApplyStyle"/> overrides the widget's
/// foreground/background from the resolved <c>color</c>/<c>background-color</c>.
/// Transparent resolved colors are left intact so they preserve the widget's existing
/// (theme-seeded) color rather than forcing a value.
/// </summary>
public interface IStyleable
{
    /// <summary>Apply a resolved style's colors to this widget, ignoring transparent values.</summary>
    void ApplyStyle(in ST.ResolvedStyle style);
}
