namespace Andy.Tui.Virtualization;

public readonly record struct GridViewportState(int FirstRow, int RowCount, int FirstCol, int ColCount);

public static class GridViewportComputer
{
    public static (int RowStart, int RowEnd, int ColStart, int ColEnd) ComputeWindow<T>(IGridProvider<T> provider, GridViewportState vp, OverscanPolicy rowOverscan, OverscanPolicy colOverscan)
    {
        int r0 = Math.Max(0, vp.FirstRow - rowOverscan.Before);
        int r1 = Math.Min(provider.RowCount - 1, vp.FirstRow + vp.RowCount + rowOverscan.After - 1);
        int c0 = Math.Max(0, vp.FirstCol - colOverscan.Before);
        int c1 = Math.Min(provider.ColCount - 1, vp.FirstCol + vp.ColCount + colOverscan.After - 1);
        return (r0, r1, c0, c1);
    }
}
