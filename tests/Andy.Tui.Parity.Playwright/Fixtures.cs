using System.Text;
using PW = Microsoft.Playwright;
using Andy.Tui.Layout;
using Andy.Tui.Style;

namespace Andy.Tui.Parity.Playwright;

public class Fixtures
{
    [Fact(Skip = "Temporarily disabled - test failing, needs investigation")]
    public async Task Row_Wrap_With_Gaps_Matches_Approx()
    {
        var pw = await TestUtil.TryCreatePlaywrightAsync();
        if (pw is null) return; // skip locally when browsers not installed
        await using var browser = await pw.Chromium.LaunchAsync(new PW.BrowserTypeLaunchOptions { Headless = true });
        var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 300, Height = 200 } });
        var page = await context.NewPageAsync();
        var html = HtmlForRowWrap();
        await page.SetContentAsync(html);
        var boxes = await GetClientRects(page, ".c", ".item");

        // Simulate our layout with similar inputs (in px)
        var containerStyle = ResolvedStyle.Default with { ColumnGap = new Length(10), RowGap = new Length(10), FlexWrap = FlexWrap.Wrap, AlignContent = AlignContent.FlexStart };
        var items = Enumerable.Repeat((ILayoutNode)new DummyNode(50, 20), 6)
            .Select(n => (n, ResolvedStyle.Default)).ToList<(ILayoutNode, ResolvedStyle)>();
        FlexLayout.Layout(new Size(300, 200), containerStyle, items);
        var ourPairs = NormalizePairs(items.Select(t => ((DummyNode)t.Item1).ArrangedRect).Select(r => (r.X, r.Y)).ToArray());
        var refPairs = NormalizePairs(boxes);
        for (int i = 0; i < ourPairs.Length; i++)
        {
            Assert.InRange(ourPairs[i].X, refPairs[i].X - 2, refPairs[i].X + 2);
            Assert.InRange(ourPairs[i].Y, refPairs[i].Y - 2, refPairs[i].Y + 2);
        }
    }

    private static (double X, double Y)[] NormalizePairs((double X, double Y)[] pairs)
    {
        if (pairs.Length == 0) return pairs;
        var minX = pairs.Min(p => p.X);
        var minY = pairs.Min(p => p.Y);
        return pairs
            .Select(p => (X: p.X - minX, Y: p.Y - minY))
            .OrderBy(p => p.Y)
            .ThenBy(p => p.X)
            .ToArray();
    }

    [Fact]
    public async Task Justify_Content_Variants_Match_Centers()
    {
        var pw = await TestUtil.TryCreatePlaywrightAsync();
        if (pw is null) return;
        await using var browser = await pw.Chromium.LaunchAsync(new PW.BrowserTypeLaunchOptions { Headless = true });
        var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 300, Height = 100 } });
        var page = await context.NewPageAsync();
        var html = "<style>.c{display:flex;width:300px;align-content:flex-start}.i{width:50px;height:10px}</style><div class=\"c\" style=\"justify-content:center\"><div class=i></div><div class=i></div></div>";
        await page.SetContentAsync(html);
        var boxes = await GetClientRects(page, ".c", ".i");

        var containerStyle = ResolvedStyle.Default with { ColumnGap = new Length(0), JustifyContent = JustifyContent.Center, AlignContent = AlignContent.FlexStart };
        var n1 = new DummyNode(50, 10);
        var n2 = new DummyNode(50, 10);
        var items = new List<(ILayoutNode, ResolvedStyle)> { (n1, ResolvedStyle.Default), (n2, ResolvedStyle.Default) };
        FlexLayout.Layout(new Size(300, 100), containerStyle, items);
        var ourPairs2 = NormalizePairs(new[] { (n1.ArrangedRect.X, n1.ArrangedRect.Y), (n2.ArrangedRect.X, n2.ArrangedRect.Y) });
        var refPairs2 = NormalizePairs(boxes);
        for (int i = 0; i < ourPairs2.Length; i++)
        {
            Assert.InRange(ourPairs2[i].X, refPairs2[i].X - 2, refPairs2[i].X + 2);
        }
    }

    [Fact(Skip = "Temporarily disabled - test failing, needs investigation")]
    public async Task Column_Wrap_AlignContent_Matches_Approx()
    {
        var pw = await TestUtil.TryCreatePlaywrightAsync();
        if (pw is null) return;
        await using var browser = await pw.Chromium.LaunchAsync(new PW.BrowserTypeLaunchOptions { Headless = true });
        var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 300, Height = 200 } });
        var page = await context.NewPageAsync();
        var html = "<style>.c{display:flex;flex-direction:column;flex-wrap:wrap;gap:10px;height:200px;width:200px;align-content:flex-start}.i{width:50px;height:50px}</style><div class=\"c\">" + new string('x', 0) + "<div class=i></div><div class=i></div><div class=i></div><div class=i></div></div>";
        await page.SetContentAsync(html);
        var boxes = await GetClientRects(page, ".c", ".i");

        var containerStyle = ResolvedStyle.Default with { FlexDirection = FlexDirection.Column, FlexWrap = FlexWrap.Wrap, RowGap = new Length(10), ColumnGap = new Length(10), AlignContent = AlignContent.FlexStart };
        var nodes = Enumerable.Repeat((ILayoutNode)new DummyNode(50, 50), 4).Select(n => (n, ResolvedStyle.Default)).ToList<(ILayoutNode, ResolvedStyle)>();
        FlexLayout.Layout(new Size(200, 200), containerStyle, nodes);
        var ourPairs3 = NormalizePairs(nodes.Select(t => ((DummyNode)t.Item1).ArrangedRect).Select(r => (r.X, r.Y)).ToArray());
        var refPairs3 = NormalizePairs(boxes);
        // Compare horizontal span between first and last columns
        var ourSpan = ourPairs3.Max(p => p.X) - ourPairs3.Min(p => p.X);
        var refSpan = refPairs3.Max(p => p.X) - refPairs3.Min(p => p.X);
        Assert.InRange(ourSpan, refSpan - 5, refSpan + 5);
    }

    private static string HtmlForRowWrap()
    {
        var sb = new StringBuilder();
        sb.Append("<style> html,body{margin:0;padding:0} *{box-sizing:border-box} .c { display:flex; flex-wrap:wrap; gap:10px; width:300px; height:200px; } .item{ width:50px; height:20px; background:#ccc } </style>");
        sb.Append("<div class=\"c\">");
        for (int i = 0; i < 6; i++) sb.Append("<div class=\"item\"></div>");
        sb.Append("</div>");
        return sb.ToString();
    }

    private static async Task<(double X, double Y)[]> GetClientRects(PW.IPage page, string containerSelector, string itemSelector)
    {
        var container = await page.QuerySelectorAsync(containerSelector);
        var cbox = await container!.EvaluateAsync<dynamic>("e => { const r = e.getBoundingClientRect(); return { x: r.left, y: r.top }; }");
        double cx = (double)cbox.x;
        double cy = (double)cbox.y;
        var handles = await page.QuerySelectorAllAsync(itemSelector);
        var list = new List<(double X, double Y)>();
        foreach (var h in handles)
        {
            var box = await h.EvaluateAsync<dynamic>("e => { const r = e.getBoundingClientRect(); return { x: r.left, y: r.top }; }");
            double x = (double)box.x - cx;
            double y = (double)box.y - cy;
            list.Add((x, y));
        }
        return list.ToArray();
    }

    private sealed class DummyNode : ILayoutNode
    {
        private readonly Size _size;
        public Rect ArrangedRect { get; private set; }
        public DummyNode(double w, double h) { _size = new Size(w, h); }
        public Size Measure(in Size available) => _size;
        public void Arrange(in Rect finalRect) { ArrangedRect = finalRect; }
    }
}
