namespace Andy.Tui.Backend.Terminal;

public sealed class TerminalCapabilities
{
    public bool TrueColor { get; init; }
    public bool Palette256 { get; init; }
    public bool Hyperlinks { get; init; }
    public UnderlineMode Underline { get; init; } = UnderlineMode.Single;
}

public enum UnderlineMode
{
    None,
    Single,
    Double
}
