using System.Collections.Generic;

namespace Andy.Tui.Compose;

/// <summary>
/// Base class for all virtual nodes in the Compose DSL tree.
/// Maintains an ordered list of child nodes.
/// </summary>
public abstract class VNode
{
    private readonly List<VNode> _children = new();

    /// <summary>
    /// Gets the ordered, read-only collection of child virtual nodes.
    /// </summary>
    public IReadOnlyList<VNode> Children => _children;

    /// <summary>
    /// Adds a child node to this node, ignoring nulls, preserving insertion order.
    /// </summary>
    public void AddChild(VNode child)
    {
        if (child is null) return;
        _children.Add(child);
    }
}
