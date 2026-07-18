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
public sealed class Signal<T> : IReadOnlySignal<T>, IReactiveSource
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
        get
        {
            // Report this read to the active tracking scope (if any) so that
            // computeds and effects can discover their dependencies automatically.
            DependencyTracker.Current?.OnRead(this);
            return _value;
        }
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

    /// <summary>
    /// Reads the current value without registering a dependency in the active
    /// tracking scope. Use this to read a signal from inside a computed or effect
    /// without subscribing to it.
    /// </summary>
    public T Peek() => _value;

    /// <inheritdoc />
    public event EventHandler<T>? ValueChanged;

    // Legacy, explicitly-registered computed dependents (manual wiring).
    private readonly HashSet<IComputed> _dependents = new();

    // Automatically-tracked dependents discovered via read tracking.
    private readonly HashSet<IReactiveDependent> _autoDependents = new();

    /// <summary>
    /// Registers a computed dependent so it can be invalidated on changes.
    /// </summary>
    internal void RegisterDependent(IComputed computed)
    {
        _dependents.Add(computed);
    }

    /// <inheritdoc />
    void IReactiveSource.AddDependent(IReactiveDependent dependent)
    {
        _autoDependents.Add(dependent);
    }

    /// <inheritdoc />
    void IReactiveSource.RemoveDependent(IReactiveDependent dependent)
    {
        _autoDependents.Remove(dependent);
    }

    private void NotifyDependents()
    {
        if (_dependents.Count > 0)
        {
            // Snapshot to tolerate dependents mutating the set during notification.
            var manual = new IComputed[_dependents.Count];
            _dependents.CopyTo(manual);
            foreach (var d in manual)
            {
                d.Invalidate();
            }
        }

        if (_autoDependents.Count > 0)
        {
            var auto = new IReactiveDependent[_autoDependents.Count];
            _autoDependents.CopyTo(auto);
            foreach (var d in auto)
            {
                d.OnDependencyChanged();
            }
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
