namespace Andy.Tui.Style;

/// <summary>
/// Selector matching the node's <see cref="Node.Type"/> (e.g., "div").
/// </summary>
public sealed record TypeSelector(string Type) : Selector(new Specificity(0, 0, 1))
{
    public override bool Matches(Node node) => string.Equals(node.Type, Type, StringComparison.OrdinalIgnoreCase);
}