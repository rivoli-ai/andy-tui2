namespace Andy.Tui.Style;

/// <summary>
/// Selector matching an id on the node.
/// </summary>
public sealed record IdSelector(string Id) : Selector(new Specificity(1, 0, 0))
{
    public override bool Matches(Node node) => string.Equals(node.Id, Id, StringComparison.Ordinal);
}