namespace Andy.Tui.Text;

/// <summary>
/// Thin internal shim retained for backwards compatibility. All width decisions
/// are delegated to <see cref="TerminalText"/>, the single shared width service,
/// so there is exactly one wide/zero-width table in the codebase.
/// </summary>
internal static class WcWidth
{
    public static int GetCharWidth(int codePoint) => TerminalText.ScalarCellWidth(codePoint);

    public static bool IsWide(int codePoint) => TerminalText.IsWide(codePoint);
}
