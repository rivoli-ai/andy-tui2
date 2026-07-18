using System;
using System.Collections.Generic;

namespace Andy.Tui.Core.Reactive;

/// <summary>
/// An imperative effect that runs an action immediately and re-runs when a tracked
/// dependency changes.
/// </summary>
/// <remarks>
/// <para>
/// When no explicit <c>subscribeDependencies</c> callback is supplied the effect
/// tracks its dependencies automatically: every signal or computed read while the
/// action runs is recorded and the effect subscribes to exactly those sources.
/// Subscriptions are recomputed on every run so dynamic dependency sets are
/// handled without leaking stale subscriptions.
/// </para>
/// <para>
/// Re-entrant triggers are collapsed: if a run mutates a dependency that would
/// re-trigger the effect, the re-run is scheduled iteratively after the current
/// run completes rather than recursing, preventing stack overflow. Disposal is
/// idempotent, releases every automatically-tracked subscription, and guarantees
/// the action never runs again. Effects are thread-affine; see
/// <see cref="DependencyTracker"/>.
/// </para>
/// </remarks>
public sealed class Effect : IDisposable, IReactiveDependent
{
    private readonly Action _action;
    private readonly Action<Action>? _subscribe;
    private readonly bool _autoTrack;
    private HashSet<IReactiveSource> _trackedSources = new();
    private bool _disposed;
    private bool _isRunning;
    private bool _pendingRerun;

    /// <summary>
    /// Creates an effect and runs it once.
    /// </summary>
    /// <param name="action">Action to run.</param>
    /// <param name="subscribeDependencies">Optional subscription hookup that will
    /// call Run upon changes. When omitted, dependencies are tracked automatically.</param>
    public Effect(Action action, Action<Action>? subscribeDependencies = null)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _subscribe = subscribeDependencies;
        _autoTrack = subscribeDependencies is null;
        _subscribe?.Invoke(Run);
        Run();
    }

    /// <summary>
    /// Runs the effect's action. Does nothing after disposal. Re-entrant calls
    /// are folded into a single follow-up run.
    /// </summary>
    public void Run()
    {
        if (_disposed)
        {
            return;
        }

        if (_isRunning)
        {
            // Fold a re-entrant trigger into the active run instead of recursing.
            _pendingRerun = true;
            return;
        }

        _isRunning = true;
        try
        {
            do
            {
                _pendingRerun = false;

                if (_autoTrack)
                {
                    var newDeps = new HashSet<IReactiveSource>();
                    using (DependencyTracker.BeginScope(new CollectingScope(newDeps)))
                    {
                        _action();
                    }

                    // If the action disposed this effect (directly or transitively),
                    // Dispose already released every subscription. Re-subscribing here
                    // would leak references to the disposed effect for the lifetime of
                    // each source, so skip the update entirely.
                    if (_disposed)
                    {
                        return;
                    }

                    UpdateSubscriptions(newDeps);
                }
                else
                {
                    _action();
                }
            }
            while (_pendingRerun && !_disposed);
        }
        finally
        {
            _isRunning = false;
        }
    }

    /// <inheritdoc />
    void IReactiveDependent.OnDependencyChanged()
    {
        if (_disposed)
        {
            return;
        }

        Run();
    }

    private void UpdateSubscriptions(HashSet<IReactiveSource> newDeps)
    {
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

    /// <summary>
    /// Releases all automatically-tracked subscriptions and prevents further runs.
    /// Idempotent.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var source in _trackedSources)
        {
            source.RemoveDependent(this);
        }

        _trackedSources = new HashSet<IReactiveSource>();
    }

    private sealed class CollectingScope : ITrackingScope
    {
        private readonly HashSet<IReactiveSource> _deps;

        public CollectingScope(HashSet<IReactiveSource> deps) => _deps = deps;

        public void OnRead(IReactiveSource source) => _deps.Add(source);
    }
}
