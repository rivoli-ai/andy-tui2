using System.Linq;
using Andy.Tui.Style;
using Xunit;

namespace Andy.Tui.Style.Tests;

/// <summary>
/// Conformance tests for the documented CSS subset (issue #37): value/unit parsing,
/// hyphenated enum tokens, selector combinator handling, nested @media, var() validation,
/// and diagnostics for unsupported input. Also asserts that invalid input never throws.
/// </summary>
public class CssConformanceTests
{
    private static ResolvedStyle Resolve(string css, Node node, EnvironmentContext? env = null)
    {
        var sheet = CssParser.Parse(css);
        return new StyleResolver().Compute(node, new[] { sheet }, env);
    }

    // ---- Lengths: unitless and px are equivalent; percent is preserved ----

    [Theory]
    [InlineData("width: 40;")]
    [InlineData("width: 40px;")]
    [InlineData("width: 40PX;")]
    public void Width_Parses_Unitless_And_Px_Consistently(string decl)
    {
        var style = Resolve($".x {{ {decl} }}", new Node("div", classes: new[] { "x" }));
        Assert.False(style.Width.IsAuto);
        Assert.Equal(40, style.Width.Value!.Value.Pixels);
        Assert.False(style.Width.Value!.Value.IsPercent);
    }

    [Fact]
    public void Width_Percent_Is_Preserved()
    {
        var style = Resolve(".x { width: 50%; }", new Node("div", classes: new[] { "x" }));
        Assert.False(style.Width.IsAuto);
        Assert.True(style.Width.Value!.Value.IsPercent);
        Assert.Equal(50, style.Width.Value!.Value.Percentage);
    }

    [Fact]
    public void RowGap_Parses_Px()
    {
        var style = Resolve(".x { row-gap: 8px; column-gap: 4; }", new Node("div", classes: new[] { "x" }));
        Assert.Equal(8, style.RowGap.Pixels);
        Assert.Equal(4, style.ColumnGap.Pixels);
    }

    [Fact]
    public void Padding_Shorthand_Accepts_Px_Units()
    {
        var style = Resolve(".x { padding: 1px 2px 3px 4px; }", new Node("div", classes: new[] { "x" }));
        // CSS order is top right bottom left.
        Assert.Equal(1, style.Padding.Top.Pixels);
        Assert.Equal(2, style.Padding.Right.Pixels);
        Assert.Equal(3, style.Padding.Bottom.Pixels);
        Assert.Equal(4, style.Padding.Left.Pixels);
    }

    // ---- Hyphenated enum tokens ----

    [Fact]
    public void JustifyContent_SpaceBetween_Maps()
    {
        var style = Resolve(".x { justify-content: space-between; }", new Node("div", classes: new[] { "x" }));
        Assert.Equal(JustifyContent.SpaceBetween, style.JustifyContent);
    }

    [Fact]
    public void AlignItems_FlexStart_Maps()
    {
        var style = Resolve(".x { align-items: flex-start; }", new Node("div", classes: new[] { "x" }));
        Assert.Equal(AlignItems.FlexStart, style.AlignItems);
    }

    [Fact]
    public void FlexWrap_WrapReverse_Maps()
    {
        var style = Resolve(".x { flex-wrap: wrap-reverse; }", new Node("div", classes: new[] { "x" }));
        Assert.Equal(FlexWrap.WrapReverse, style.FlexWrap);
    }

    [Fact]
    public void JustifyContent_SpaceEvenly_Maps()
    {
        var style = Resolve(".x { justify-content: space-evenly; }", new Node("div", classes: new[] { "x" }));
        Assert.Equal(JustifyContent.SpaceEvenly, style.JustifyContent);
    }

    // ---- Documented CSS round-trips into a full ResolvedStyle ----

    [Fact]
    public void Documented_Css_RoundTrips_Into_Expected_ResolvedStyle()
    {
        const string css = @"
            .card {
                display: flex;
                flex-direction: column;
                justify-content: space-between;
                align-items: center;
                flex-wrap: wrap-reverse;
                width: 120px;
                height: 40;
                row-gap: 2;
                column-gap: 3px;
                overflow: hidden;
                color: rgb(1,2,3);
                background-color: #0a0b0c;
                font-weight: 700;
                font-style: italic;
                text-decoration: underline;
                padding: 1 2 3 4;
            }";
        var style = Resolve(css, new Node("div", classes: new[] { "card" }));

        Assert.Equal(Display.Flex, style.Display);
        Assert.Equal(FlexDirection.Column, style.FlexDirection);
        Assert.Equal(JustifyContent.SpaceBetween, style.JustifyContent);
        Assert.Equal(AlignItems.Center, style.AlignItems);
        Assert.Equal(FlexWrap.WrapReverse, style.FlexWrap);
        Assert.Equal(120, style.Width.Value!.Value.Pixels);
        Assert.Equal(40, style.Height.Value!.Value.Pixels);
        Assert.Equal(2, style.RowGap.Pixels);
        Assert.Equal(3, style.ColumnGap.Pixels);
        Assert.Equal(Overflow.Hidden, style.Overflow);
        Assert.Equal(new RgbaColor(1, 2, 3, 255), style.Color);
        Assert.Equal(FontWeight.Bold, style.FontWeight);
        Assert.Equal(FontStyle.Italic, style.FontStyle);
        Assert.Equal(TextDecoration.Underline, style.TextDecoration);
        Assert.Equal(1, style.Padding.Top.Pixels);
        Assert.Equal(4, style.Padding.Left.Pixels);
    }

    // ---- Comments are stripped ----

    [Fact]
    public void Comments_Are_Ignored_Including_Inside_Values()
    {
        var css = ".x /* a */ { color/* b */: red; /* c */ }";
        var sheet = CssParser.Parse(css);
        var style = new StyleResolver().Compute(new Node("div", classes: new[] { "x" }), new[] { sheet });
        Assert.Equal(RgbaColor.FromRgb(255, 0, 0), style.Color);
        Assert.Empty(sheet.Diagnostics);
    }

    // ---- Descendant / combinator / list selectors are rejected with diagnostics ----

    [Fact]
    public void Descendant_Selector_Is_Rejected_With_Diagnostic()
    {
        var sheet = CssParser.Parse("div span { color: red; }");
        Assert.Empty(sheet.Rules);
        Assert.Contains(sheet.Diagnostics, d => d.Message.Contains("Descendant"));
    }

    [Fact]
    public void Descendant_Selector_Does_Not_Match_As_Compound()
    {
        // "div.a" (compound) must still match; "div .a" (descendant) must not silently apply.
        var sheet = CssParser.Parse("div .a { color: red; }");
        var node = new Node("div", classes: new[] { "a" });
        var style = new StyleResolver().Compute(node, new[] { sheet });
        Assert.Equal(ResolvedStyle.Default.Color, style.Color);
    }

    [Fact]
    public void Child_Combinator_Is_Rejected_With_Diagnostic()
    {
        var sheet = CssParser.Parse("ul > li { color: red; }");
        Assert.Empty(sheet.Rules);
        Assert.Contains(sheet.Diagnostics, d => d.Message.Contains("combinator"));
    }

    [Fact]
    public void Selector_List_Is_Rejected_With_Diagnostic()
    {
        var sheet = CssParser.Parse("a, b { color: red; }");
        Assert.Empty(sheet.Rules);
        Assert.Contains(sheet.Diagnostics, d => d.Message.Contains("Selector lists"));
    }

    [Fact]
    public void Compound_Selector_Still_Matches()
    {
        var sheet = CssParser.Parse("button.primary:hover { color: red; }");
        Assert.Empty(sheet.Diagnostics);
        var node = new Node("button", classes: new[] { "primary" }) { IsHover = true };
        var style = new StyleResolver().Compute(node, new[] { sheet });
        Assert.Equal(RgbaColor.FromRgb(255, 0, 0), style.Color);
    }

    // ---- Standard nested @media ----

    [Fact]
    public void Nested_Media_Standard_Syntax_Gates_Rule()
    {
        var css = "@media (min-width: 100px) { .wide { color: blue; } }";
        var sheet = CssParser.Parse(css);
        Assert.Empty(sheet.Diagnostics);
        var node = new Node("div", classes: new[] { "wide" });
        var resolver = new StyleResolver();
        var narrow = resolver.Compute(node, new[] { sheet }, new EnvironmentContext { ViewportWidth = 50 });
        var wide = resolver.Compute(node, new[] { sheet }, new EnvironmentContext { ViewportWidth = 150 });
        Assert.Equal(ResolvedStyle.Default.Color, narrow.Color);
        Assert.Equal(RgbaColor.FromRgb(0, 0, 255), wide.Color);
    }

    [Fact]
    public void Nested_Media_With_Multiple_Rules_All_Gated()
    {
        var css = "@media (min-width: 100) { .a { color: red; } .b { color: blue; } }";
        var sheet = CssParser.Parse(css);
        Assert.Equal(2, sheet.Rules.Count);
        var resolver = new StyleResolver();
        var wide = new EnvironmentContext { ViewportWidth = 150 };
        var a = resolver.Compute(new Node("div", classes: new[] { "a" }), new[] { sheet }, wide);
        var b = resolver.Compute(new Node("div", classes: new[] { "b" }), new[] { sheet }, wide);
        Assert.Equal(RgbaColor.FromRgb(255, 0, 0), a.Color);
        Assert.Equal(RgbaColor.FromRgb(0, 0, 255), b.Color);
    }

    [Fact]
    public void Media_And_Combines_Features()
    {
        var css = "@media (min-width: 100) and (max-width: 200) { .m { color: green; } }";
        var sheet = CssParser.Parse(css);
        Assert.Empty(sheet.Diagnostics);
        var resolver = new StyleResolver();
        var node = new Node("div", classes: new[] { "m" });
        var inRange = resolver.Compute(node, new[] { sheet }, new EnvironmentContext { ViewportWidth = 150 });
        var tooWide = resolver.Compute(node, new[] { sheet }, new EnvironmentContext { ViewportWidth = 250 });
        Assert.Equal(RgbaColor.FromRgb(0, 128, 0), inRange.Color);
        Assert.Equal(ResolvedStyle.Default.Color, tooWide.Color);
    }

    [Fact]
    public void Legacy_Prefix_Media_Still_Supported()
    {
        var css = "@media(min-width: 100) .wide { color: blue; }";
        var sheet = CssParser.Parse(css);
        Assert.Single(sheet.Rules);
        var node = new Node("div", classes: new[] { "wide" });
        var style = new StyleResolver().Compute(node, new[] { sheet }, new EnvironmentContext { ViewportWidth = 150 });
        Assert.Equal(RgbaColor.FromRgb(0, 0, 255), style.Color);
    }

    [Fact]
    public void Unsupported_Media_Feature_Produces_Diagnostic_And_Drops_Block()
    {
        var css = "@media (orientation: landscape) { .x { color: red; } }";
        var sheet = CssParser.Parse(css);
        Assert.Empty(sheet.Rules);
        Assert.Contains(sheet.Diagnostics, d => d.Message.Contains("media feature"));
    }

    // ---- var() validation ----

    [Fact]
    public void Var_With_Fallback_Resolves_Fallback_When_Unknown()
    {
        var css = ".x { color: var(--missing, red); }";
        var style = Resolve(css, new Node("div", classes: new[] { "x" }));
        Assert.Equal(RgbaColor.FromRgb(255, 0, 0), style.Color);
    }

    [Fact]
    public void Malformed_Var_Missing_Close_Produces_Diagnostic_And_Does_Not_Throw()
    {
        var css = ".x { color: var(--c; }";
        var sheet = CssParser.Parse(css); // must not throw
        Assert.Contains(sheet.Diagnostics, d => d.Message.Contains("var()"));
        // Resolution must not throw either.
        var ex = Record.Exception(() => new StyleResolver().Compute(new Node("div", classes: new[] { "x" }), new[] { sheet }));
        Assert.Null(ex);
    }

    [Fact]
    public void Var_Trailing_Content_Is_Flagged()
    {
        var sheet = CssParser.Parse(".x { color: var(--c) red; }");
        Assert.Contains(sheet.Diagnostics, d => d.Message.Contains("trailing"));
    }

    [Fact]
    public void Var_Bad_Name_Is_Flagged()
    {
        var sheet = CssParser.Parse(".x { color: var(accent); }");
        Assert.Contains(sheet.Diagnostics, d => d.Message.Contains("--"));
    }

    [Fact]
    public void Var_Cycle_Does_Not_Throw_And_Falls_Back()
    {
        // Self-referential custom property must not loop forever.
        var css = "#x { --a: var(--a); } #x { color: var(--a, green); }";
        var node = new Node("div", id: "x");
        var ex = Record.Exception(() =>
        {
            var sheet = CssParser.Parse(css);
            _ = new StyleResolver().Compute(node, new[] { sheet });
        });
        Assert.Null(ex);
    }

    // ---- Unsupported / malformed declarations produce diagnostics ----

    [Fact]
    public void Unknown_Property_Produces_Diagnostic()
    {
        var sheet = CssParser.Parse(".x { transform: rotate(3deg); }");
        Assert.Contains(sheet.Diagnostics, d => d.Message.Contains("Unsupported property 'transform'"));
    }

    [Fact]
    public void Custom_Property_Is_Not_Flagged_As_Unsupported()
    {
        var sheet = CssParser.Parse(".x { --accent: red; color: var(--accent); }");
        Assert.DoesNotContain(sheet.Diagnostics, d => d.Message.Contains("Unsupported property"));
    }

    [Fact]
    public void Declaration_Missing_Colon_Is_Flagged()
    {
        var sheet = CssParser.Parse(".x { color red; }");
        Assert.Contains(sheet.Diagnostics, d => d.Message.Contains("missing ':'"));
    }

    // ---- Robustness: invalid input never throws ----

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{")]
    [InlineData("}")]
    [InlineData(".x {")]
    [InlineData(".x { color: ; }")]
    [InlineData(".x { : red; }")]
    [InlineData("@media (")]
    [InlineData("@media (min-width: 100) {")]
    [InlineData("var(")]
    [InlineData(".x { color: var(; }")]
    [InlineData(".x { color: var(--); }")]
    [InlineData("garbage no braces at all")]
    [InlineData("/* only a comment */")]
    [InlineData("@unknown-rule { .x { color: red; } }")]
    public void Malformed_Input_Never_Throws(string css)
    {
        var ex = Record.Exception(() =>
        {
            var sheet = CssParser.Parse(css);
            _ = new StyleResolver().Compute(new Node("div", classes: new[] { "x" }), new[] { sheet }, new EnvironmentContext());
        });
        Assert.Null(ex);
    }

    [Fact]
    public void Valid_Input_Has_No_Diagnostics()
    {
        var css = ".btn { color: red; background-color: #fff; padding: 2px; justify-content: flex-start; }";
        var sheet = CssParser.Parse(css);
        Assert.Empty(sheet.Diagnostics);
        Assert.Single(sheet.Rules);
    }
}
