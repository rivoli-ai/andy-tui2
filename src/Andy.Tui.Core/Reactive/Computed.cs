using System;

namespace Andy.Tui.Core.Reactive;

/// <summary>
/// A lazily evaluated value derived from other signals. Caches the last computed
/// value until invalidated by subscribed dependency changes.
/// </summary>
/// <typeparam name="T">The computed value type.</typeparam>
public sealed class Computed<T> : IReadOnlySignal<T>, IComputed, IDisposable
{
    private readonly Func<T> _compute;
    private readonly Action<Action>? _subscribe;
    private T _cachedValue;
    private bool _isValid;

    /// <summary>
    /// Creates a computed value.
    /// </summary>
    /// <param name="compute">Function that computes the value.</param>
    /// <param name="subscribeDependencies">Optional callback used to subscribe to
    /// dependency change events; call the provided action to invalidate.</param>
    public Computed(Func<T> compute, Action<Action>? subscribeDependencies = null)
    {
        _compute = compute;
        _subscribe = subscribeDependencies;
        _cachedValue = default!;
        _isValid = false;

        // Subscribe to dependencies if provided
        _subscribe?.Invoke(Invalidate);
    }

    /// <inheritdoc />
    public T Value
    {
        get
        {
            if (!_isValid)
            {
                _cachedValue = _compute();
                _isValid = true;
            }
            return _cachedValue;
        }
    }

    /// <inheritdoc />
    public event EventHandler<T>? ValueChanged;

    public void Invalidate()
    {
        if (_isValid)
        {
            _isValid = false;
            ValueChanged?.Invoke(this, _cachedValue);
        }
    }

    public void Dispose()
    {
        // No resources yet
    }
}
