namespace Andy.Tui.Compose;

/// <summary>
/// A virtual element node with a semantic type (e.g., "stack", "box").
/// </summary>
public sealed class VElement : VNode
{
    /// <summary>
    /// Gets the semantic type of this element.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Creates a new element of the given semantic type.
    /// </summary>
    public VElement(string type)
    {
        Type = type;
    }
}
