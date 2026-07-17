using System.Text;
using PW = Microsoft.Playwright;

namespace Andy.Tui.Parity.Playwright;

/// <summary>
/// Browser parity tests. Every scenario in <see cref="ParityScenarios.All"/> is
/// rendered by a real Chromium engine and compared, position by position,
/// against Andy.Tui's <see cref="Andy.Tui.Layout.FlexLayout"/> output. The same
/// scenario objects generate both the reference HTML and the Andy.Tui inputs,
/// so the two sides can never drift apart.
///
/// When a Chromium build is not available (for example a developer machine that
/// never ran <c>playwright install</c>), the tests degrade to a no-op instead
/// of failing, because <see cref="TestUtil.TryCreatePlaywrightAsync"/> returns
/// null. In CI the browser is installed, so the comparison actually runs. The
/// browser-free <see cref="DeterministicParityTests"/> guarantee coverage
/// everywhere regardless.
/// </summary>
public class Fixtures
{
    public static IEnumerable<object[]> Scenarios() =>
        ParityScenarios.All.Select(s => new object[] { s.Name });

    [Theory]
    [MemberData(nameof(Scenarios))]
    public async Task Browser_Parity(string name)
    {
        var scenario = ParityScenarios.All.Single(s => s.Name == name);

        var pw = await TestUtil.TryCreatePlaywrightAsync();
        if (pw is null) return; // Chromium not installed locally; deterministic tests still cover this.

        await using var browser = await pw.Chromium.LaunchAsync(new PW.BrowserTypeLaunchOptions { Headless = true });
        var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new()
            {
                Width = (int)Math.Ceiling(scenario.ContainerWidth) + 40,
                Height = (int)Math.Ceiling(scenario.ContainerHeight) + 40,
            },
        });
        var page = await context.NewPageAsync();
        await page.SetContentAsync(scenario.ToReferenceHtml());

        var refPositions = await GetItemTopLefts(page);
        var reference = ParityScenarios.Normalize(refPositions);

        var ourRects = scenario.RunAndyTui();
        var ours = ParityScenarios.Normalize(ourRects.Select(r => (r.X, r.Y)));

        Assert.Equal(reference.Length, ours.Length);

        var tol = ParityScenarios.PositionTolerancePx;
        for (int i = 0; i < reference.Length; i++)
        {
            bool xOk = Math.Abs(ours[i].X - reference[i].X) <= tol;
            bool yOk = Math.Abs(ours[i].Y - reference[i].Y) <= tol;
            if (!xOk || !yOk)
            {
                Assert.Fail(Diagnostics(name, reference, ours));
            }
        }
    }

    /// <summary>
    /// Reads the top-left of every laid-out flex child relative to the container
    /// content box. Items with display:none are skipped so only in-flow items
    /// are compared, matching how <see cref="ParityScenario.RunAndyTui"/> reports
    /// only visible items.
    /// </summary>
    private static async Task<(double X, double Y)[]> GetItemTopLefts(PW.IPage page)
    {
        var container = await page.QuerySelectorAsync(".c");
        var cbox = await container!.EvaluateAsync<dynamic>(
            "e => { const r = e.getBoundingClientRect(); return { x: r.left, y: r.top }; }");
        double cx = (double)cbox.x;
        double cy = (double)cbox.y;

        var handles = await page.QuerySelectorAllAsync(".c > .i");
        var list = new List<(double X, double Y)>();
        foreach (var h in handles)
        {
            var visible = await h.EvaluateAsync<bool>(
                "e => { const s = getComputedStyle(e); return s.display !== 'none'; }");
            if (!visible) continue;
            var box = await h.EvaluateAsync<dynamic>(
                "e => { const r = e.getBoundingClientRect(); return { x: r.left, y: r.top }; }");
            list.Add(((double)box.x - cx, (double)box.y - cy));
        }
        return list.ToArray();
    }

    private static string Diagnostics(
        string name,
        IReadOnlyList<(double X, double Y)> reference,
        IReadOnlyList<(double X, double Y)> ours)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Browser parity mismatch for scenario '{name}' (tolerance {ParityScenarios.PositionTolerancePx}px).");
        sb.AppendLine("idx |   browser (x,y)      |     andytui (x,y)    |  dx     dy");
        int n = Math.Max(reference.Count, ours.Count);
        for (int i = 0; i < n; i++)
        {
            var e = i < reference.Count ? reference[i] : (double.NaN, double.NaN);
            var o = i < ours.Count ? ours[i] : (double.NaN, double.NaN);
            sb.AppendLine($"{i,3} | ({e.Item1,7:0.##},{e.Item2,7:0.##}) | ({o.Item1,7:0.##},{o.Item2,7:0.##}) | {o.Item1 - e.Item1,6:0.##} {o.Item2 - e.Item2,6:0.##}");
        }
        return sb.ToString();
    }
}
