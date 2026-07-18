using System;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;
using IN = Andy.Tui.Input;

namespace Andy.Tui.Widgets;

/// <summary>
/// Optional per-widget style overrides applied on top of the ambient theme. A widget
/// with no style resolves its colours from the theme as before; a style hook lets a
/// caller override foreground, background, or attributes without subclassing.
/// </summary>
public readonly record struct WidgetStyle(
    DL.Rgb24? Foreground = null,
    DL.Rgb24? Background = null,
    DL.CellAttrFlags Attrs = DL.CellAttrFlags.None);

/// <summary>
/// The single composable runtime contract shared by the built-in widgets. It unifies
/// measurement, rendering, identity, focusability, input handling, disabled and visible
/// state, style hooks, and invalidation so that any widget can be nested directly in the
/// stack and container widgets without an adapter.
/// </summary>
public interface IWidget : IRenderable
{
    /// <summary>Stable identity used to correlate a widget across reconciliation and reorder.</summary>
    string? Key { get; }

    /// <summary>When false the widget occupies no space and paints nothing.</summary>
    bool IsVisible { get; }

    /// <summary>When false the widget renders in a disabled style and rejects input.</summary>
    bool IsEnabled { get; }

    /// <summary>Whether the widget can receive keyboard focus.</summary>
    bool Focusable { get; }

    /// <summary>Whether the widget currently holds focus.</summary>
    bool IsFocused { get; }

    /// <summary>Optional per-widget style overrides layered over the ambient theme.</summary>
    WidgetStyle? Style { get; }

    /// <summary>Reports the widget's desired size within the supplied available space.</summary>
    L.Size Measure(L.Size available);

    /// <summary>
    /// Offers an input event to the widget. Returns true when the widget consumed it.
    /// Disabled or invisible widgets never consume input.
    /// </summary>
    bool HandleInput(IN.IInputEvent ev);

    /// <summary>Shows or hides the widget, raising <see cref="Invalidated"/> on change.</summary>
    void SetVisible(bool visible);

    /// <summary>Enables or disables the widget, raising <see cref="Invalidated"/> on change.</summary>
    void SetEnabled(bool enabled);

    /// <summary>Sets focus. A non-focusable widget can never become focused.</summary>
    void SetFocused(bool focused);

    /// <summary>Applies (or clears with null) the per-widget style overrides.</summary>
    void SetStyle(WidgetStyle? style);

    /// <summary>Raised whenever state that affects rendering changes.</summary>
    event Action? Invalidated;

    /// <summary>Signals that the widget needs to be repainted.</summary>
    void Invalidate();
}
