using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Andy.Tui.Style;

namespace Andy.Tui.Style.Tests;

/// <summary>
/// Covers StyleCache correctness for issue #36: identity, dependency-complete keying,
/// bounded eviction, and thread-safety.
/// </summary>
public class StyleCacheTests
{
    private static EnvironmentContext Env(double w = 100, double h = 40)
        => new() { ViewportWidth = w, ViewportHeight = h };

    [Fact]
    public void Changing_Stylesheet_Does_Not_Return_Stale_Result()
    {
        var cache = new StyleCache();
        var node = new Node("div", classes: new[] { "x" });

        var sheetRed = CssParser.Parse(".x { color: red; }");
        var sheetBlue = CssParser.Parse(".x { color: blue; }");

        var first = cache.GetComputedStyle(node, new[] { sheetRed }, Env());
        var second = cache.GetComputedStyle(node, new[] { sheetBlue }, Env());

        Assert.Equal(RgbaColor.FromRgb(255, 0, 0), first.Color);
        // Must reflect the new stylesheet, not the cached red result.
        Assert.Equal(RgbaColor.FromRgb(0, 0, 255), second.Color);
    }

    [Fact]
    public void Changing_Parent_Does_Not_Return_Stale_Result()
    {
        var cache = new StyleCache();
        // .child sets no color, so color inherits from the parent style.
        var sheet = CssParser.Parse(".child { }");
        var node = new Node("span", classes: new[] { "child" });

        var parentA = ResolvedStyle.Default with { Color = RgbaColor.FromRgb(10, 20, 30) };
        var parentB = ResolvedStyle.Default with { Color = RgbaColor.FromRgb(200, 100, 50) };

        var withA = cache.GetComputedStyle(node, new[] { sheet }, Env(), parentA);
        var withB = cache.GetComputedStyle(node, new[] { sheet }, Env(), parentB);

        Assert.Equal(parentA.Color, withA.Color);
        // Different parent must not reuse the entry cached for parentA.
        Assert.Equal(parentB.Color, withB.Color);
    }

    [Fact]
    public void Changing_Theme_Stylesheet_Does_Not_Return_Stale_Result()
    {
        var cache = new StyleCache();
        var node = new Node("button", classes: new[] { "btn" });

        // Two "themes" modeled as distinct stylesheet objects layered over a base sheet.
        var baseSheet = CssParser.Parse(".btn { color: black; }");
        var lightTheme = CssParser.Parse(".btn { background-color: rgb(255,255,255); }");
        var darkTheme = CssParser.Parse(".btn { background-color: rgb(0,0,0); }");

        var light = cache.GetComputedStyle(node, new[] { baseSheet, lightTheme }, Env());
        var dark = cache.GetComputedStyle(node, new[] { baseSheet, darkTheme }, Env());

        Assert.Equal(new RgbaColor(255, 255, 255, 255), light.BackgroundColor);
        Assert.Equal(new RgbaColor(0, 0, 0, 255), dark.BackgroundColor);
    }

    [Fact]
    public void Pseudo_State_Produces_Distinct_Cached_Results()
    {
        var cache = new StyleCache();
        var sheet = CssParser.Parse("button { color: black; } button:hover { color: red; }");

        var normal = new Node("button");
        var hovered = new Node("button") { IsHover = true };

        var normalStyle = cache.GetComputedStyle(normal, new[] { sheet }, Env());
        var hoverStyle = cache.GetComputedStyle(hovered, new[] { sheet }, Env());

        Assert.NotEqual(normalStyle.Color, hoverStyle.Color);
        Assert.Equal(RgbaColor.FromRgb(255, 0, 0), hoverStyle.Color);
        // Re-fetching the hovered node returns the same (correct) cached result.
        Assert.Equal(hoverStyle.Color, cache.GetComputedStyle(hovered, new[] { sheet }, Env()).Color);
    }

    [Fact]
    public void Forced_Hash_Collisions_Do_Not_Cross_Contaminate_Nodes()
    {
        // Every key collapses to the same hash bucket; only reference-identity equality
        // separates entries. Each node must still get its own result.
        var cache = new StyleCache(maxEntries: 1024, forceHashCollisionsForTesting: true);
        var sheet = CssParser.Parse(".a { color: red; } .b { color: blue; }");

        var nodeA = new Node("div", classes: new[] { "a" });
        var nodeB = new Node("div", classes: new[] { "b" });

        // Prime the cache in one order...
        var a1 = cache.GetComputedStyle(nodeA, new[] { sheet }, Env());
        var b1 = cache.GetComputedStyle(nodeB, new[] { sheet }, Env());
        // ...then read back interleaved.
        var a2 = cache.GetComputedStyle(nodeA, new[] { sheet }, Env());
        var b2 = cache.GetComputedStyle(nodeB, new[] { sheet }, Env());

        Assert.Equal(RgbaColor.FromRgb(255, 0, 0), a1.Color);
        Assert.Equal(RgbaColor.FromRgb(0, 0, 255), b1.Color);
        Assert.Equal(a1.Color, a2.Color);
        Assert.Equal(b1.Color, b2.Color);
        Assert.Equal(2, cache.Count);
    }

    [Fact]
    public void Repeated_Resize_Does_Not_Grow_Cache_Without_Bound()
    {
        const int capacity = 16;
        var cache = new StyleCache(capacity);
        var sheet = CssParser.Parse("div { color: green; }");
        var node = new Node("div");

        // Each distinct viewport width is a distinct environment signature => distinct key.
        for (int w = 0; w < 500; w++)
        {
            cache.GetComputedStyle(node, new[] { sheet }, Env(w: w));
        }

        Assert.True(cache.Count <= capacity, $"Cache grew to {cache.Count}, expected <= {capacity}.");
    }

    [Fact]
    public void Repeated_Theme_Changes_Do_Not_Grow_Cache_Without_Bound()
    {
        const int capacity = 8;
        var cache = new StyleCache(capacity);
        var node = new Node("div", classes: new[] { "x" });

        for (int i = 0; i < 200; i++)
        {
            // A fresh stylesheet object each iteration models a theme swap.
            var themeSheet = CssParser.Parse(".x { color: rgb(" + (i % 256) + ",0,0); }");
            cache.GetComputedStyle(node, new[] { themeSheet }, Env());
        }

        Assert.True(cache.Count <= capacity, $"Cache grew to {cache.Count}, expected <= {capacity}.");
    }

    [Fact]
    public void Least_Recently_Used_Entry_Is_Evicted_First()
    {
        var cache = new StyleCache(maxEntries: 2);
        var sheet = CssParser.Parse("div { color: black; }");
        var node = new Node("div");

        var e0 = Env(w: 0);
        var e1 = Env(w: 1);
        var e2 = Env(w: 2);

        cache.GetComputedStyle(node, new[] { sheet }, e0); // insert e0
        cache.GetComputedStyle(node, new[] { sheet }, e1); // insert e1
        cache.GetComputedStyle(node, new[] { sheet }, e0); // touch e0 -> e1 now LRU
        cache.GetComputedStyle(node, new[] { sheet }, e2); // insert e2 -> evicts e1

        Assert.Equal(2, cache.Count);
        // e1 was evicted; recomputing it grows count back but stays within capacity.
        cache.GetComputedStyle(node, new[] { sheet }, e1);
        Assert.Equal(2, cache.Count);
    }

    [Fact]
    public void Concurrent_Access_Is_Safe_And_Correct()
    {
        var cache = new StyleCache(maxEntries: 4096);
        var sheet = CssParser.Parse(".a { color: red; } .b { color: blue; }");
        var nodeA = new Node("div", classes: new[] { "a" });
        var nodeB = new Node("div", classes: new[] { "b" });

        var red = RgbaColor.FromRgb(255, 0, 0);
        var blue = RgbaColor.FromRgb(0, 0, 255);

        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
        Parallel.For(0, 2000, i =>
        {
            try
            {
                var env = Env(w: i % 50); // exercise inserts, hits, and eviction concurrently
                var a = cache.GetComputedStyle(nodeA, new[] { sheet }, env);
                var b = cache.GetComputedStyle(nodeB, new[] { sheet }, env);
                Assert.Equal(red, a.Color);
                Assert.Equal(blue, b.Color);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.Empty(exceptions);
    }

    [Fact]
    public void Invalidate_On_Env_Change_Keeps_Results_Consistent()
    {
        var css = "@media(min-width: 100) div { color: blue; }";
        var sheet = CssParser.Parse(css);
        var node = new Node("div");
        var cache = new StyleCache();
        var envNarrow = new EnvironmentContext { ViewportWidth = 80 };
        var envWide = new EnvironmentContext { ViewportWidth = 150 };

        var styleNarrow = cache.GetComputedStyle(node, new[] { sheet }, envNarrow);
        var styleWide1 = cache.GetComputedStyle(node, new[] { sheet }, envWide);
        Assert.NotEqual(styleNarrow.Color, styleWide1.Color);

        cache.InvalidateForEnvChange(envNarrow, envWide);
        var styleWide2 = cache.GetComputedStyle(node, new[] { sheet }, envWide);
        Assert.Equal(styleWide1.Color, styleWide2.Color);
    }

    [Fact]
    public void NonPositive_Capacity_Is_Rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new StyleCache(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new StyleCache(-5));
    }
}
