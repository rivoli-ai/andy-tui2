using System.Text;

namespace Andy.Tui.Parity.Playwright;

/// <summary>
/// Browser-free half of the parity suite. Each scenario in
/// <see cref="ParityScenarios.All"/> is compared against geometry derived by
/// hand from the CSS flexbox specification. Because these run without a
/// browser, they execute on every machine and every CI job, so the row-wrap and
/// column-wrap regressions this issue targets are guarded even where Playwright
/// browsers are unavailable. The Playwright <see cref="Fixtures"/> tests then
/// confirm the same scenarios against a real engine.
/// </summary>
public class DeterministicParityTests
{
    public static IEnumerable<object[]> Scenarios() =>
        ParityScenarios.All.Select(s => new object[] { s.Name });

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void AndyTui_Matches_Css_Reference_Geometry(string name)
    {
        var scenario = ParityScenarios.All.Single(s => s.Name == name);
        var expected = ParityScenarios.Normalize(ExpectedGeometry[name]);

        var ourRects = scenario.RunAndyTui();
        var ours = ParityScenarios.Normalize(ourRects.Select(r => (r.X, r.Y)));

        Assert.Equal(expected.Length, ours.Length);

        var tol = ParityScenarios.PositionTolerancePx;
        for (int i = 0; i < expected.Length; i++)
        {
            bool xOk = Math.Abs(ours[i].X - expected[i].X) <= tol;
            bool yOk = Math.Abs(ours[i].Y - expected[i].Y) <= tol;
            if (!xOk || !yOk)
            {
                Assert.Fail(Diagnostics(name, expected, ours));
            }
        }
    }

    /// <summary>
    /// The display:none item must be excluded from the flow entirely, so the
    /// two visible items sit flush as if the hidden item were absent.
    /// </summary>
    [Fact]
    public void Display_None_Item_Is_Removed_From_Flow()
    {
        var scenario = ParityScenarios.All.Single(s => s.Name == "row_display_none");
        var rects = scenario.RunAndyTui();
        Assert.Equal(2, rects.Length); // hidden item produced no box
        var ours = ParityScenarios.Normalize(rects.Select(r => (r.X, r.Y)));
        Assert.Equal(0, ours[0].X, 3);
        Assert.Equal(60, ours[1].X, 3); // 50px item + 10px gap, hidden item skipped
    }

    private static string Diagnostics(
        string name,
        IReadOnlyList<(double X, double Y)> expected,
        IReadOnlyList<(double X, double Y)> ours)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Parity mismatch for scenario '{name}' (tolerance {ParityScenarios.PositionTolerancePx}px).");
        sb.AppendLine("idx |    expected (x,y)    |     andytui (x,y)    |  dx     dy");
        int n = Math.Max(expected.Count, ours.Count);
        for (int i = 0; i < n; i++)
        {
            var e = i < expected.Count ? expected[i] : (double.NaN, double.NaN);
            var o = i < ours.Count ? ours[i] : (double.NaN, double.NaN);
            sb.AppendLine($"{i,3} | ({e.Item1,7:0.##},{e.Item2,7:0.##}) | ({o.Item1,7:0.##},{o.Item2,7:0.##}) | {o.Item1 - e.Item1,6:0.##} {o.Item2 - e.Item2,6:0.##}");
        }
        return sb.ToString();
    }

    /// <summary>
    /// CSS-spec reference geometry (top-left of each visible item, relative to
    /// the container content box, in DOM order). Normalized before comparison.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, (double X, double Y)[]> ExpectedGeometry =
        new Dictionary<string, (double X, double Y)[]>
        {
            // 300x200, 6x(50x20), gap 10, row wrap -> 5 on line 1, 1 on line 2
            ["row_wrap_gaps"] = new[]
            {
                (0d, 0d), (60d, 0d), (120d, 0d), (180d, 0d), (240d, 0d), (0d, 30d),
            },
            // 200x200, 4x(50x50), gap 10, column wrap -> 3 in col 1, 1 in col 2
            ["column_wrap_gaps"] = new[]
            {
                (0d, 0d), (0d, 60d), (0d, 120d), (60d, 0d),
            },
            // 120x300, 6x(50x30), gap 10, row wrap -> 2 per line, 3 lines
            ["row_wrap_three_lines"] = new[]
            {
                (0d, 0d), (60d, 0d), (0d, 40d), (60d, 40d), (0d, 80d), (60d, 80d),
            },
            // 400x100, 4x(40x20), column-gap 12, row nowrap
            ["row_gaps_nowrap"] = new[]
            {
                (0d, 0d), (52d, 0d), (104d, 0d), (156d, 0d),
            },
            // 100x400, 4x(40x30), row-gap 15, column nowrap
            ["column_gaps_nowrap"] = new[]
            {
                (0d, 0d), (0d, 45d), (0d, 90d), (0d, 135d),
            },
            // 300x60, 3x(40x20) grow 1 -> each 100 wide
            ["row_grow_even"] = new[]
            {
                (0d, 0d), (100d, 0d), (200d, 0d),
            },
            // 300x60, 30w grow 1 and grow 2 -> widths 110 and 190
            ["row_grow_weighted"] = new[]
            {
                (0d, 0d), (110d, 0d),
            },
            // 200x60, 2x(120x20) shrink 1 -> each 100 wide
            ["row_shrink"] = new[]
            {
                (0d, 0d), (100d, 0d),
            },
            // 300x60, grow 1 capped at max 60, grow 1 -> widths 60 and 240
            ["row_grow_max_width"] = new[]
            {
                (0d, 0d), (60d, 0d),
            },
            // 300x60, 2x(50x20) justify center -> start at 100
            ["justify_center"] = new[]
            {
                (100d, 0d), (150d, 0d),
            },
            // 300x60, 2x(50x20) justify flex-end -> start at 200
            ["justify_flex_end"] = new[]
            {
                (200d, 0d), (250d, 0d),
            },
            // 300x60, 3x(40x20) justify space-between -> 90px between
            ["justify_space_between"] = new[]
            {
                (0d, 0d), (130d, 0d), (260d, 0d),
            },
            // 300x100, 2x(40x20) align-items center, col-gap 10 -> y centered at 40
            ["align_items_center"] = new[]
            {
                (0d, 40d), (50d, 40d),
            },
            // 300x100, 2x(40x20) align-items flex-end, col-gap 10 -> y at 80
            ["align_items_flex_end"] = new[]
            {
                (0d, 80d), (50d, 80d),
            },
            // 100x300, 2x(40x30) column align-items center -> x centered at 30
            ["column_align_items_center"] = new[]
            {
                (30d, 0d), (30d, 40d),
            },
            // 300x60, middle item display:none, col-gap 10 -> visible items flush
            ["row_display_none"] = new[]
            {
                (0d, 0d), (60d, 0d),
            },
        };
}
