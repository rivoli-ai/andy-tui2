using Andy.Tui.DisplayList;

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
    /// Gets the resolved foreground color for this text, if any. A <c>null</c>
    /// value means the terminal's default foreground is used when the node is
    /// rendered into a display list.
    /// </summary>
    public Rgb24? Foreground { get; private set; }

    /// <summary>
    /// Creates a new text node with the given content.
    /// </summary>
    public VText(string text)
    {
        Text = text;
    }

    /// <summary>
    /// Sets the foreground color used when this node is rendered and returns
    /// this node for fluent chaining.
    /// </summary>
    public VText WithForeground(Rgb24 color)
    {
        Foreground = color;
        return this;
    }
}
