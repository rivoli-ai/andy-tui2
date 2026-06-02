namespace Andy.Tui.Virtualization;

public interface IGridProvider<T>
{
    int RowCount { get; }
    int ColCount { get; }
    T GetItem(int row, int col);
    string GetKey(int row, int col);
}
