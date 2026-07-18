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
    /// Gets the stable identity key for this node, if any. Keys give a node an
    /// identity that survives reordering among its siblings: two renders that
    /// place a node with the same key retain the same mounted instance (and its
    /// state and effects). A <c>null</c> key falls back to positional identity.
    /// </summary>
    public object? Key { get; private set; }

    /// <summary>
    /// Assigns a stable identity key and returns this node for fluent chaining.
    /// </summary>
    public VNode WithKey(object? key)
    {
        Key = key;
        return this;
    }

    /// <summary>
    /// Adds a child node to this node, ignoring nulls, preserving insertion order.
    /// </summary>
    public void AddChild(VNode child)
    {
        if (child is null) return;
        _children.Add(child);
    }
}
