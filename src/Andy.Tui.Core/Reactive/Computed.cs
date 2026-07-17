using System;
using System.Collections.Generic;

namespace Andy.Tui.Core.Reactive;

/// <summary>
/// A lazily evaluated value derived from other signals and computeds.
/// </summary>
/// <remarks>
/// <para>
/// When no explicit <c>subscribeDependencies</c> callback is supplied the
/// computed tracks its dependencies automatically: every signal or computed read
/// while its compute function runs is recorded, and the computed subscribes to
/// exactly those sources. Subscriptions are recomputed on every evaluation so
/// dynamic dependency sets (for example a branch that reads different signals)
/// are handled without leaking stale subscriptions.
/// </para>
/// <para>
/// Recomputation is lazy: an invalidated computed recomputes on the next read.
/// The one exception is <see cref="ValueChanged"/>: when it has subscribers the
/// value is recomputed eagerly on invalidation so the event can deliver the newly
/// computed value. The event fires only when the new value differs from the
/// previous one (per <see cref="EqualityComparer{T}"/>) and always carries the
/// new value.
/// </para>
/// <para>
/// Cyclic dependencies are detected during evaluation and surface as an
/// <see cref="InvalidOperationException"/>. Disposal is idempotent and releases
/// every automatically-tracked subscription. Reactive graphs are thread-affine;
/// see <see cref="DependencyTracker"/>.
/// </para>
/// </remarks>
/// <typeparam name="T">The computed value type.</typeparam>
public sealed class Computed<T> : IReadOnlySignal<T>, IComputed, IReactiveSource, IReactiveDependent, IDisposable
{
    private readonly Func<T> _compute;
    private readonly Action<Action>? _subscribe;
    private readonly bool _autoTrack;
    private T _cachedValue;
    private bool _isValid;
    private bool _isComputing;
    private bool _disposed;

    // Sources this computed is currently subscribed to (automatic mode).
    private HashSet<IReactiveSource> _trackedSources = new();

    // Consumers subscribed to this computed (nested computeds / effects).
    private readonly HashSet<IReactiveDependent> _autoDependents = new();

    /// <summary>
    /// Creates a computed value.
    /// </summary>
    /// <param name="compute">Function that computes the value.</param>
    /// <param name="subscribeDependencies">Optional callback used to subscribe to
    /// dependency change events; call the provided action to invalidate. When
    /// omitted, dependencies are tracked automatically.</param>
    public Computed(Func<T> compute, Action<Action>? subscribeDependencies = null)
    {
        _compute = compute ?? throw new ArgumentNullException(nameof(compute));
        _subscribe = subscribeDependencies;
        _autoTrack = subscribeDependencies is null;
        _cachedValue = default!;
        _isValid = false;

        // Subscribe to dependencies if provided (manual wiring).
        _subscribe?.Invoke(Invalidate);
    }

    /// <inheritdoc />
    public T Value
    {
        get
        {
            // Report this read so an enclosing computed/effect can depend on us.
            DependencyTracker.Current?.OnRead(this);

            if (!_isValid && !_disposed)
            {
                Recompute();
            }

            return _cachedValue;
        }
    }

    /// <summary>
    /// Raised when the computed value changes. The event carries the newly
    /// computed value and fires only when that value differs from the previous one.
    /// </summary>
    public event EventHandler<T>? ValueChanged;

    private void Recompute()
    {
        if (_isComputing)
        {
            throw new InvalidOperationException(
                "Cyclic dependency detected while evaluating a computed value.");
        }

        _isComputing = true;
        try
        {
            if (_autoTrack)
            {
                var newDeps = new HashSet<IReactiveSource>();
                using (DependencyTracker.BeginScope(new CollectingScope(newDeps)))
                {
                    _cachedValue = _compute();
                }

                UpdateSubscriptions(newDeps);
            }
            else
            {
                _cachedValue = _compute();
            }

            _isValid = true;
        }
        finally
        {
            _isComputing = false;
        }
    }

    private void UpdateSubscriptions(HashSet<IReactiveSource> newDeps)
    {
        // A computed must never subscribe to itself (guards accidental self-reads).
        newDeps.Remove(this);

        foreach (var existing in _trackedSources)
        {
            if (!newDeps.Contains(existing))
            {
                existing.RemoveDependent(this);
            }
        }

        foreach (var dep in newDeps)
        {
            if (!_trackedSources.Contains(dep))
            {
                dep.AddDependent(this);
            }
        }

        _trackedSources = newDeps;
    }

    /// <inheritdoc />
    public void Invalidate() => InvalidateInternal();

    /// <inheritdoc />
    void IReactiveDependent.OnDependencyChanged() => InvalidateInternal();

    private void InvalidateInternal()
    {
        if (_disposed || !_isValid)
        {
            return;
        }

        var oldValue = _cachedValue;
        _isValid = false;

        // Propagate invalidation to consumers so the graph stays consistent lazily.
        NotifyAutoDependents();

        // Deliver the new value to direct observers eagerly.
        var handler = ValueChanged;
        if (handler is not null)
        {
            if (!_isValid)
            {
                Recompute();
            }

            if (!EqualityComparer<T>.Default.Equals(oldValue, _cachedValue))
            {
                handler.Invoke(this, _cachedValue);
            }
        }
    }

    private void NotifyAutoDependents()
    {
        if (_autoDependents.Count == 0)
        {
            return;
        }

        var snapshot = new IReactiveDependent[_autoDependents.Count];
        _autoDependents.CopyTo(snapshot);
        foreach (var d in snapshot)
        {
            d.OnDependencyChanged();
        }
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

    /// <summary>
    /// Releases all automatically-tracked subscriptions. Idempotent.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _isValid = false;

        foreach (var source in _trackedSources)
        {
            source.RemoveDependent(this);
        }

        _trackedSources = new HashSet<IReactiveSource>();
        _autoDependents.Clear();
        ValueChanged = null;
    }

    private sealed class CollectingScope : ITrackingScope
    {
        private readonly HashSet<IReactiveSource> _deps;

        public CollectingScope(HashSet<IReactiveSource> deps) => _deps = deps;

        public void OnRead(IReactiveSource source) => _deps.Add(source);
    }
}
