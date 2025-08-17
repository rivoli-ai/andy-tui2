namespace Andy.Tui.Style;

/// <summary>
/// Minimal environment context for media queries and runtime state.
/// </summary>
public sealed class EnvironmentContext
{
    public double ViewportWidth { get; init; }
    public double ViewportHeight { get; init; }
    public bool IsTerminal { get; init; } = true;
    public bool PrefersReducedMotion { get; init; }

    // Pseudo-class state could be provided via Node if needed; kept simple for now
}
