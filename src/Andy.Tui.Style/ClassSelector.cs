namespace Andy.Tui.Style;

/// <summary>
/// Selector matching a class on the node.
/// </summary>
public sealed record ClassSelector(string ClassName) : Selector(new Specificity(0, 1, 0))
{
    public override bool Matches(Node node) => node.Classes.Contains(ClassName);
}