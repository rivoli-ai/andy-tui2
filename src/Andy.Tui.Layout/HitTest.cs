namespace Andy.Tui.Layout;

public readonly record struct Hit(int NodeId, int X, int Y);

/// <summary>
/// A hit-testable node placed in explicit paint order. Later entries paint on top
/// of earlier ones. An optional clip restricts the visible/interactive region, and
/// invisible nodes never receive events.
/// </summary>
public readonly record struct HitTestNode(int NodeId, Rect Bounds, Rect? Clip = null, bool Visible = true);

public static class HitTest
{
    private static bool Contains(in Rect r, double x, double y)
        => x >= r.X && x < r.Right && y >= r.Y && y < r.Bottom;

    // Legacy: hit-test against a rects map (nodeId -> rect). Enumeration order is not
    // a reliable paint order; prefer the explicit-order overload below.
    public static int? HitAt(IReadOnlyDictionary<int, Rect> rects, int x, int y)
    {
        foreach (var kv in rects.Reverse())
        {
            var r = kv.Value;
            if (Contains(r, x, y))
                return kv.Key;
        }
        return null;
    }

    /// <summary>
    /// Hit-test nodes in explicit paint order, returning the top-most eligible target.
    /// Nodes are evaluated from last (top) to first (bottom); a node is eligible only
    /// when it is visible, the point lies inside its bounds, and inside its clip (if any).
    /// </summary>
    public static int? HitAt(IReadOnlyList<HitTestNode> nodesInPaintOrder, double x, double y)
    {
        for (int i = nodesInPaintOrder.Count - 1; i >= 0; i--)
        {
            var n = nodesInPaintOrder[i];
            if (!n.Visible) continue;
            if (!Contains(n.Bounds, x, y)) continue;
            if (n.Clip is Rect clip && !Contains(clip, x, y)) continue;
            return n.NodeId;
        }
        return null;
    }
}
