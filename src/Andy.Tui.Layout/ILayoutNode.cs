namespace Andy.Tui.Layout;

/// <summary>
/// Contract for layout-capable nodes.
/// </summary>
public interface ILayoutNode
{
    Size Measure(in Size available);
    void Arrange(in Rect finalRect);
}