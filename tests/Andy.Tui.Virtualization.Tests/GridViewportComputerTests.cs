namespace Andy.Tui.Virtualization.Tests;

public class GridViewportComputerTests
{
    private sealed class IntGrid : IGridProvider<int>
    {
        public int RowCount => 100;
        public int ColCount => 50;
        public int GetItem(int row, int col) => row * ColCount + col;
        public string GetKey(int row, int col) => $"{row}:{col}";
    }

    [Fact]
    public void ComputeWindow_Applies_Overscan_In_Both_Dimensions()
    {
        var grid = new IntGrid();
        var vp = new GridViewportState(FirstRow: 10, RowCount: 5, FirstCol: 4, ColCount: 3);
        var rowOver = new OverscanPolicy(Before: 2, After: 3, Adaptive: false);
        var colOver = new OverscanPolicy(Before: 1, After: 2, Adaptive: false);

        var (r0, r1, c0, c1) = GridViewportComputer.ComputeWindow(grid, vp, rowOver, colOver);

        Assert.Equal(8, r0);                 // 10 - 2
        Assert.Equal(10 + 5 + 3 - 1, r1);    // 17
        Assert.Equal(3, c0);                 // 4 - 1
        Assert.Equal(4 + 3 + 2 - 1, c1);     // 8
    }

    [Fact]
    public void ComputeWindow_Clamps_To_Grid_Bounds()
    {
        var grid = new IntGrid();
        var vp = new GridViewportState(FirstRow: 0, RowCount: 200, FirstCol: 0, ColCount: 200);
        var over = new OverscanPolicy(Before: 5, After: 5, Adaptive: false);

        var (r0, r1, c0, c1) = GridViewportComputer.ComputeWindow(grid, vp, over, over);

        Assert.Equal(0, r0);
        Assert.Equal(grid.RowCount - 1, r1);
        Assert.Equal(0, c0);
        Assert.Equal(grid.ColCount - 1, c1);
    }
}
