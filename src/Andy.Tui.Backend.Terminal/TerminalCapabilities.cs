namespace Andy.Tui.Backend.Terminal;

public sealed class TerminalCapabilities
{
    public bool TrueColor { get; init; }
    public bool Palette256 { get; init; }
    public bool Hyperlinks { get; init; }
    public UnderlineMode Underline { get; init; } = UnderlineMode.Single;

    /// <summary>
    /// True when the terminal is known to implement the standard whole-screen
    /// scroll operations (CSI S / CSI T, SU/SD) with predictable semantics.
    /// Vertical-scroll damage reduction is only applied when this is set; when
    /// it is false the renderer repaints every changed row instead, which is
    /// always correct but emits more bytes.
    /// </summary>
    public bool ScrollRegion { get; init; }
}

public enum UnderlineMode
{
    None,
    Single,
    Double
}
