namespace Andy.Tui.Style;

/// <summary>
/// Describes a problem encountered while resolving a recognized declaration.
/// Emitted when a supported property is present but carries a value that cannot
/// be interpreted, so callers can surface authoring mistakes instead of having
/// the value silently fall back to its default.
/// </summary>
public readonly record struct StyleDiagnostic(string Property, string RawValue, string Message)
{
    public override string ToString() => $"{Property}: {Message} (value: '{RawValue}')";
}
