using System;

namespace Andy.Tui.Compose;

/// <summary>
/// Coalesces invalidations into frames. When a composition is invalidated it
/// asks the scheduler to run a frame; multiple invalidations that arrive before
/// the scheduler runs the frame must collapse into a single frame so that a
/// burst of state changes produces bounded work.
/// </summary>
public interface IFrameScheduler
{
    /// <summary>
    /// Requests that <paramref name="frameCallback"/> be run once. Repeated
    /// requests before the callback runs coalesce into a single pending frame.
    /// </summary>
    void Request(Action frameCallback);
}

/// <summary>
/// A deterministic scheduler that holds at most one pending frame and runs it
/// only when <see cref="Flush"/> is called. Useful for driving and testing a
/// composition frame-by-frame.
/// </summary>
public sealed class ManualFrameScheduler : IFrameScheduler
{
    private Action? _pending;

    /// <summary>
    /// Number of distinct frames that have been requested (a coalesced burst of
    /// requests counts as one).
    /// </summary>
    public int RequestedFrames { get; private set; }

    /// <summary>
    /// True when a frame has been requested but not yet flushed.
    /// </summary>
    public bool HasPendingFrame => _pending is not null;

    /// <inheritdoc />
    public void Request(Action frameCallback)
    {
        if (frameCallback is null) throw new ArgumentNullException(nameof(frameCallback));
        if (_pending is null) RequestedFrames++;
        _pending = frameCallback;
    }

    /// <summary>
    /// Runs the pending frame, if any, then clears it. Returns true if a frame ran.
    /// </summary>
    public bool Flush()
    {
        var cb = _pending;
        _pending = null;
        if (cb is null) return false;
        cb();
        return true;
    }
}
