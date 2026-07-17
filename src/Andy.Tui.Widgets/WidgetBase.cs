using System;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;
using IN = Andy.Tui.Input;

namespace Andy.Tui.Widgets;

/// <summary>
/// Base implementation of <see cref="IWidget"/> that supplies the shared runtime
/// behaviour — visibility, enabled and focus state, style hooks, and invalidation — so
/// that concrete widgets only implement <see cref="RenderCore"/> and <see cref="MeasureCore"/>.
/// This is the real abstraction that custom widgets inherit from.
/// </summary>
public abstract class WidgetBase : IWidget
{
    /// <inheritdoc />
    public string? Key { get; private set; }

    /// <inheritdoc />
    public bool IsVisible { get; private set; } = true;

    /// <inheritdoc />
    public bool IsEnabled { get; private set; } = true;

    /// <inheritdoc />
    public virtual bool Focusable => false;

    /// <inheritdoc />
    public bool IsFocused { get; private set; }

    /// <inheritdoc />
    public WidgetStyle? Style { get; private set; }

    /// <inheritdoc />
    public event Action? Invalidated;

    /// <summary>Assigns a stable identity and returns this widget for fluent construction.</summary>
    public WidgetBase WithKey(string? key)
    {
        Key = key;
        return this;
    }

    /// <inheritdoc />
    public void SetVisible(bool visible)
    {
        if (IsVisible == visible) return;
        IsVisible = visible;
        Invalidate();
    }

    /// <inheritdoc />
    public void SetEnabled(bool enabled)
    {
        if (IsEnabled == enabled) return;
        IsEnabled = enabled;
        OnEnabledChanged();
        Invalidate();
    }

    /// <inheritdoc />
    public void SetFocused(bool focused)
    {
        if (!Focusable) focused = false;
        if (IsFocused == focused) return;
        IsFocused = focused;
        OnFocusChanged();
        Invalidate();
    }

    /// <inheritdoc />
    public void SetStyle(WidgetStyle? style)
    {
        Style = style;
        OnStyleChanged();
        Invalidate();
    }

    /// <inheritdoc />
    public void Invalidate() => Invalidated?.Invoke();

    /// <inheritdoc />
    public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        if (!IsVisible) return;
        if (rect.Width <= 0 || rect.Height <= 0) return;
        RenderCore(in rect, baseDl, builder);
    }

    /// <inheritdoc />
    public L.Size Measure(L.Size available) => MeasureCore(available);

    /// <inheritdoc />
    public bool HandleInput(IN.IInputEvent ev)
    {
        if (!IsVisible || !IsEnabled) return false;
        return HandleInputCore(ev);
    }

    /// <summary>Paints the widget. Called only when visible with a positive-area rectangle.</summary>
    protected abstract void RenderCore(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder);

    /// <summary>Reports the widget's desired size. Defaults to a single cell.</summary>
    protected virtual L.Size MeasureCore(L.Size available) => new(1, 1);

    /// <summary>Handles an input event. Called only when visible and enabled. Defaults to no-op.</summary>
    protected virtual bool HandleInputCore(IN.IInputEvent ev) => false;

    /// <summary>Hook invoked after the enabled state changes.</summary>
    protected virtual void OnEnabledChanged() { }

    /// <summary>Hook invoked after the focus state changes.</summary>
    protected virtual void OnFocusChanged() { }

    /// <summary>Hook invoked after the style overrides change.</summary>
    protected virtual void OnStyleChanged() { }

    /// <summary>Resolves the effective foreground, honouring any style override.</summary>
    protected DL.Rgb24 ResolveForeground(DL.Rgb24 fallback) => Style?.Foreground ?? fallback;

    /// <summary>Resolves the effective background, honouring any style override.</summary>
    protected DL.Rgb24 ResolveBackground(DL.Rgb24 fallback) => Style?.Background ?? fallback;
}
