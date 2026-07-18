namespace Andy.Tui.Style;

/// <summary>
/// A stylesheet is an ordered set of rules, plus any diagnostics produced while parsing it.
/// </summary>
public sealed class Stylesheet
{
    public IReadOnlyList<Rule> Rules { get; }

    /// <summary>
    /// Diagnostics emitted by <see cref="CssParser"/> for unsupported, invalid, or malformed
    /// input. Empty for hand-constructed stylesheets. Never null.
    /// </summary>
    public IReadOnlyList<CssDiagnostic> Diagnostics { get; }

    public Stylesheet(IEnumerable<Rule> rules)
        : this(rules, Array.Empty<CssDiagnostic>())
    {
    }

    public Stylesheet(IEnumerable<Rule> rules, IEnumerable<CssDiagnostic> diagnostics)
    {
        Rules = rules.ToArray();
        Diagnostics = diagnostics.ToArray();
    }

    public static Stylesheet Empty { get; } = new(Array.Empty<Rule>());
}
