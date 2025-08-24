namespace Andy.Tui.Virtualization.Tests;

public class ViewportComputerTests
{
    private sealed class IntCollection : IVirtualizedCollection<int>
    {
        public int Count => 100;
        public int this[int index] => index;
        public string GetKey(int index) => index.ToString();
    }

    [Fact]
    public void Fixed_Row_Height_Window_Computation()
    {
        var coll = new IntCollection();
        var vp = new ViewportState(FirstRow: 10, RowCount: 5, Cols: 80, Rows: 25, PixelWidth: 0, PixelHeight: 0);
        var over = new OverscanPolicy(Before: 2, After: 3, Adaptive: false);
        var (first, last) = ViewportComputer.ComputeWindowGeneric(coll, vp, over, coll.GetKey, _ => 1);
        Assert.Equal(8, first);
        Assert.Equal(17, last);
    }

    [Fact]
    public void Adaptive_Overscan_Expands_In_Scroll_Direction()
    {
        var coll = new IntCollection();
        var vp = new ViewportState(FirstRow: 50, RowCount: 10, Cols: 80, Rows: 25, PixelWidth: 0, PixelHeight: 0);
        var over = new OverscanPolicy(Before: 1, After: 1, Adaptive: true);
        // Scrolling down by 6 rows => after should expand
        var (f1, l1) = ViewportComputer.ComputeWindowGenericAdaptive(coll, vp, over, recentDeltaRows: 6, coll.GetKey, _ => 1);
        Assert.Equal(49, f1); // 50 - before(1)
        Assert.Equal(50 + 10 + (1 + 6) - 1, l1);
        // Scrolling up by 4 rows => before should expand
        var (f2, l2) = ViewportComputer.ComputeWindowGenericAdaptive(coll, vp, over, recentDeltaRows: -4, coll.GetKey, _ => 1);
        Assert.Equal(50 - (1 + 4), f2);
        Assert.Equal(50 + 10 + 1 - 1, l2);
    }

    [Fact]
    public void Variable_Row_Heights_Window_Computation()
    {
        var coll = new IntCollection();
        // Simulate alternating heights: 2,1,2,1,...
        int MeasureIndex(int i) => (i % 2 == 0) ? 2 : 1;
        var vp = new ViewportState(FirstRow: 10, RowCount: 5, Cols: 80, Rows: 25, PixelWidth: 0, PixelHeight: 0);
        var over = new OverscanPolicy(Before: 2, After: 2, Adaptive: false);
        var (first, last) = ViewportComputer.ComputeWindowMeasuredByIndex(coll, vp, over, MeasureIndex);
        Assert.True(first <= 10);
        Assert.True(last >= 14);
    }
}
