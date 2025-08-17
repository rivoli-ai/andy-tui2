namespace Andy.Tui.Style;

/// <summary>
/// A rule binds a selector to a set of property declarations.
/// Declarations are stored as a string key to object value; the resolver parses/validates into the typed ResolvedStyle.
/// An optional media condition can gate the rule based on the current environment.
/// </summary>
public sealed record Rule(
    Selector Selector,
    IReadOnlyDictionary<string, object> Declarations,
    int SourceOrder,
    Func<EnvironmentContext, bool>? MediaCondition = null);