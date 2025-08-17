namespace Andy.Tui.Compositor;

public interface ICompositor
{
    CellGrid Composite(Andy.Tui.DisplayList.DisplayList dl, (int Width, int Height) viewport);
    IReadOnlyList<DirtyRect> Damage(CellGrid previous, CellGrid next);
    IReadOnlyList<RowRun> RowRuns(CellGrid grid, IReadOnlyList<DirtyRect> dirty);
}