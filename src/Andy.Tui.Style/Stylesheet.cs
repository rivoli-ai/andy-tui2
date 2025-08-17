namespace Andy.Tui.Style;

/// <summary>
/// A stylesheet is an ordered set of rules.
/// </summary>
public sealed class Stylesheet
{
    public IReadOnlyList<Rule> Rules { get; }
    public Stylesheet(IEnumerable<Rule> rules)
    {
        Rules = rules.ToArray();
    }

    public static Stylesheet Empty { get; } = new(Array.Empty<Rule>());
}