namespace Andy.Tui.Virtualization.Tests;

public class ViewportComputerNonGenericTests
{
    private sealed class ObjectCollection : IVirtualizedCollection<object>
    {
        public int Count => 100;
        public object this[int index] => index;
        public string GetKey(int index) => index.ToString();
    }

    [Fact]
    public void Fixed_Row_Height_Window_Computation()
    {
        var coll = new ObjectCollection();
        var vp = new ViewportState(FirstRow: 10, RowCount: 5, Cols: 80, Rows: 25, PixelWidth: 0, PixelHeight: 0);
        var over = new OverscanPolicy(Before: 2, After: 3, Adaptive: false);

        var (first, last) = ViewportComputer.ComputeWindow(coll, vp, over, coll.GetKey, _ => 1);

        Assert.Equal(8, first);
        Assert.Equal(17, last);
    }

    [Fact]
    public void Window_Clamps_To_Collection_Bounds()
    {
        var coll = new ObjectCollection();
        var vp = new ViewportState(FirstRow: 0, RowCount: 500, Cols: 80, Rows: 25, PixelWidth: 0, PixelHeight: 0);
        var over = new OverscanPolicy(Before: 5, After: 5, Adaptive: false);

        var (first, last) = ViewportComputer.ComputeWindow(coll, vp, over, coll.GetKey, _ => 1);

        Assert.Equal(0, first);
        Assert.Equal(coll.Count - 1, last);
    }
}
