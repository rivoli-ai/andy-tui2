namespace Andy.Tui.Layout;

/// <summary>
/// Optional interface for nodes that can report a typographic first baseline offset from the top.
/// </summary>
public interface IBaselineProvider
{
    /// <summary>
    /// Returns the distance from the top edge to the first baseline, for the given measured size.
    /// </summary>
    double GetFirstBaseline(in Size measuredSize);
}
