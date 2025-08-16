using System;
using System.Collections.Generic;

namespace Andy.Tui.Core.Reactive;

/// <summary>
/// A read-only signal (observable value) that exposes its current value and a change event.
/// </summary>
/// <typeparam name="T">The value type stored in the signal.</typeparam>
public interface IReadOnlySignal<T>
{
    /// <summary>
    /// Gets the current value of the signal.
    /// </summary>
    T Value { get; }

    /// <summary>
    /// Raised when the value changes to a different value (per EqualityComparer&lt;T&gt;.Default).
    /// </summary>
    event EventHandler<T>? ValueChanged;
}

/// <summary>
/// A mutable signal that tracks dependents and notifies subscribers on value changes.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
public sealed class Signal<T> : IReadOnlySignal<T>
{
    private T _value;

    /// <summary>
    /// Creates a new signal with the given initial value.
    /// </summary>
    public Signal(T initialValue)
    {
        _value = initialValue;
    }

    /// <inheritdoc />
    public T Value
    {
        get => _value;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(_value, value))
            {
                _value = value;
                ValueChanged?.Invoke(this, _value);
                NotifyDependents();
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler<T>? ValueChanged;

    private readonly HashSet<IComputed> _dependents = new();

    /// <summary>
    /// Registers a computed dependent so it can be invalidated on changes.
    /// </summary>
    internal void RegisterDependent(IComputed computed)
    {
        _dependents.Add(computed);
    }

    private void NotifyDependents()
    {
        foreach (var d in _dependents)
        {
            d.Invalidate();
        }
    }
}

/// <summary>
/// Internal contract implemented by computed signals to support invalidation.
/// </summary>
internal interface IComputed
{
    void Invalidate();
}
