namespace Andy.Tui.Layout;

public readonly record struct Hit(int NodeId, int X, int Y);

public static class HitTest
{
    // Placeholder: hit-test against provided rects map (nodeId -> rect)
    public static int? HitAt(IReadOnlyDictionary<int, Rect> rects, int x, int y)
    {
        foreach (var kv in rects.Reverse())
        {
            var r = kv.Value;
            if (x >= r.X && x < r.X + r.Width && y >= r.Y && y < r.Y + r.Height)
                return kv.Key;
        }
        return null;
    }
}
