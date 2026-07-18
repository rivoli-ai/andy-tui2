namespace Andy.Tui.Style;

/// <summary>
/// Severity classification for a <see cref="CssDiagnostic"/>.
/// </summary>
public enum CssDiagnosticSeverity
{
    /// <summary>The input was accepted but with a caveat (e.g. an ignored declaration).</summary>
    Warning,

    /// <summary>The input was rejected and produced no rule or declaration.</summary>
    Error
}

/// <summary>
/// An actionable message emitted by <see cref="CssParser"/> when it encounters
/// unsupported, invalid, or malformed CSS. Diagnostics let callers surface problems
/// instead of silently falling back to defaults.
/// </summary>
public sealed record CssDiagnostic(CssDiagnosticSeverity Severity, string Message)
{
    public static CssDiagnostic Warning(string message) => new(CssDiagnosticSeverity.Warning, message);
    public static CssDiagnostic Error(string message) => new(CssDiagnosticSeverity.Error, message);

    public override string ToString() => $"{Severity}: {Message}";
}
