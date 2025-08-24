using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class DataGridTests
{
    [Fact]
    public void Renders_Header_And_Active_Cell()
    {
        var grid = new Andy.Tui.Widgets.DataGrid();
        grid.SetColumns(new[] { "A", "B" }, new[] { 3, 3 });
        grid.SetRowCount(10);
        grid.SetCellTextProvider((r, c) => c == 0 ? $"{r:D2}" : $"X{r % 10}");
        grid.SetActiveCell(1, 1);
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        grid.Render(new L.Rect(0, 0, 10, 5), baseDl, b);
        var dl = b.Build();
        // Header present at y=0
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), t => t.Y == 0 && t.Content.Contains("A"));
        // Active cell text rendered
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), t => t.Content.Contains("X1"));
    }
}
