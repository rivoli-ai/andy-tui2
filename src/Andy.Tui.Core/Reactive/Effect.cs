using System;

namespace Andy.Tui.Core.Reactive;

/// <summary>
/// An imperative effect that runs an action immediately and whenever explicitly triggered.
/// Composition systems can subscribe this effect to dependency changes.
/// </summary>
public sealed class Effect : IDisposable
{
    private readonly Action _action;
    private readonly Action<Action>? _subscribe;

    /// <summary>
    /// Creates an effect.
    /// </summary>
    /// <param name="action">Action to run.</param>
    /// <param name="subscribeDependencies">Optional subscription hookup that will call Run upon changes.</param>
    public Effect(Action action, Action<Action>? subscribeDependencies = null)
    {
        _action = action;
        _subscribe = subscribeDependencies;
        _subscribe?.Invoke(Run);
        Run();
    }

    /// <summary>
    /// Runs the effect's action.
    /// </summary>
    public void Run()
    {
        _action();
    }

    public void Dispose()
    {
        // No-op for now
    }
}
