using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class TableTests
{
    [Fact]
    public void SortBy_Sorts_Rows_By_Column()
    {
        var t = new Andy.Tui.Widgets.Table();
        t.SetColumns(new[] { "A", "B" });
        t.SetRows(new[] { new[] { "b", "2" }, new[] { "a", "1" } });
        t.SortBy(0, asc: true);
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        t.Render(new L.Rect(0, 0, 40, 5), baseDl, b);
        var dl = b.Build();
        var runs = dl.Ops.OfType<DL.TextRun>().ToList();
        // Header row is at Y=1
        var header = runs.First(r => r.Y == 1);
        Assert.Contains("A", header.Content);
        // First data row begins at Y=2; pick the leftmost run on that row
        var rowRuns = runs.Where(r => r.Y == 2 && !string.IsNullOrWhiteSpace(r.Content))
                          .OrderBy(r => r.X)
                          .ToList();
        Assert.NotEmpty(rowRuns);
        Assert.Equal("a", rowRuns.First().Content);
    }

    [Fact]
    public void Header_Row_Is_Rendered_With_Sort_Arrow()
    {
        var t = new Andy.Tui.Widgets.Table();
        t.SetColumns(new[] { "Ticker", "Price", "Change" });
        t.SetRows(new[] {
            new[]{"AAPL","196.48","+0.84%"},
            new[]{"MSFT","423.12","-0.31%"}
        });
        // Sort by first column to show ▲ in header
        t.SortBy(0, asc: true);
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        t.Render(new L.Rect(0, 0, 40, 6), baseDl, b);
        var dl = b.Build();
        // Header is drawn one row below the top border within the widget rect
        var headerRuns = dl.Ops.OfType<DL.TextRun>().Where(r => r.Y == 1).ToList();
        Assert.NotEmpty(headerRuns);
        var header = headerRuns[0];
        Assert.Contains("Ticker", header.Content);
        Assert.Contains("▲", header.Content);
        Assert.True((header.Attrs & DL.CellAttrFlags.Bold) == DL.CellAttrFlags.Bold);
    }
}
