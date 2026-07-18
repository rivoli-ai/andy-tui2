using System;

namespace Andy.Tui.Core.Reactive;

/// <summary>
/// A reactive source (signal or computed) that can be observed. Automatic
/// dependency tracking subscribes and unsubscribes dependents against these
/// members as a consumer's dynamic reads change between runs.
/// </summary>
internal interface IReactiveSource
{
    /// <summary>Adds a dependent that should be notified when this source changes.</summary>
    void AddDependent(IReactiveDependent dependent);

    /// <summary>Removes a previously added dependent.</summary>
    void RemoveDependent(IReactiveDependent dependent);
}

/// <summary>
/// A reactive consumer (computed or effect) that is notified when one of the
/// sources it read during its last run changes.
/// </summary>
internal interface IReactiveDependent
{
    /// <summary>Invoked when a tracked dependency changed.</summary>
    void OnDependencyChanged();
}

/// <summary>
/// An ambient scope that records the reactive sources read while a computed or
/// effect body executes.
/// </summary>
internal interface ITrackingScope
{
    /// <summary>Records that <paramref name="source"/> was read within this scope.</summary>
    void OnRead(IReactiveSource source);
}

/// <summary>
/// Thread-affine ambient dependency tracker. A computed or effect installs a
/// <see cref="ITrackingScope"/> for the duration of its body; every signal or
/// computed read during that window reports itself, allowing dependencies to be
/// discovered automatically.
/// </summary>
/// <remarks>
/// Thread affinity: the current scope is stored in thread-local state, so a
/// reactive graph must be built and updated from a single thread. Cross-thread
/// mutation is not synchronized and is unsupported.
/// </remarks>
internal static class DependencyTracker
{
    [ThreadStatic]
    private static ITrackingScope? _current;

    /// <summary>The scope currently collecting reads on this thread, if any.</summary>
    public static ITrackingScope? Current => _current;

    /// <summary>
    /// Installs <paramref name="scope"/> as the current scope and returns a token
    /// that restores the previous scope when disposed. Scopes nest correctly.
    /// </summary>
    public static IDisposable BeginScope(ITrackingScope scope)
    {
        var previous = _current;
        _current = scope;
        return new ScopeToken(previous);
    }

    private sealed class ScopeToken : IDisposable
    {
        private readonly ITrackingScope? _previous;
        private bool _restored;

        public ScopeToken(ITrackingScope? previous) => _previous = previous;

        public void Dispose()
        {
            if (_restored)
            {
                return;
            }

            _restored = true;
            _current = _previous;
        }
    }
}
