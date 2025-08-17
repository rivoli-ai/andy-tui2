namespace Andy.Tui.Style;

/// <summary>
/// Base selector abstraction. Implementations provide a match predicate and specificity.
/// </summary>
public abstract record Selector(Specificity Specificity)
{
    public abstract bool Matches(Node node);
}

public sealed record PseudoClassSelector(string Name) : Selector(new Specificity(0, 1, 0))
{
    public override bool Matches(Node node)
        => Name switch
        {
            ":hover" => node.IsHover,
            ":focus" => node.IsFocus,
            ":active" => node.IsActive,
            ":disabled" => node.IsDisabled,
            _ => false
        };
}

public sealed record AndSelector(Selector Left, Selector Right) : Selector(new Specificity(Left.Specificity.A + Right.Specificity.A, Left.Specificity.B + Right.Specificity.B, Left.Specificity.C + Right.Specificity.C))
{
    public override bool Matches(Node node) => Left.Matches(node) && Right.Matches(node);
}