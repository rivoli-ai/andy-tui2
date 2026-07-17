namespace Andy.Tui.Compose;

/// <summary>
/// The commit phase in which a composition effect runs.
/// </summary>
/// <remarks>
/// Within a single commit, every <see cref="Layout"/> effect runs before any
/// <see cref="Paint"/> effect. Cleanups for effects that are being re-run (or
/// unmounted) run before any new effect of that commit, in reverse of the order
/// in which their effects were registered.
/// </remarks>
public enum EffectPhase
{
    /// <summary>
    /// Runs after reconciliation but before paint. Use for work that must be
    /// observed before the frame is drawn (e.g. measuring or positioning).
    /// </summary>
    Layout = 0,

    /// <summary>
    /// Runs after layout effects. Use for work that can happen once the frame's
    /// visual output is committed (e.g. subscriptions, side effects).
    /// </summary>
    Paint = 1,
}
