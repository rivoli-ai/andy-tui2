namespace Andy.Tui.Compose;

/// <summary>
/// A virtual text node containing plain text content.
/// </summary>
public sealed class VText : VNode
{
    /// <summary>
    /// Gets the text content of this node.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Creates a new text node with the given content.
    /// </summary>
    public VText(string text)
    {
        Text = text;
    }
}
