namespace Andy.Tui.Virtualization;

public interface IVirtualizedCollection<T>
{
    int Count { get; }
    T this[int index] { get; }
    string GetKey(int index);
}
