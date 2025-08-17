using Xunit;
using Andy.Tui.Style;
using System.Linq;

public class ParserAndMediaTests
{
    [Fact]
    public void CssParser_Parses_Class_Rule_And_Color_Name()
    {
        var css = ".btn { color: red; }";
        var sheet = CssParser.Parse(css);
        var node = new Node("button", classes: new[] { "btn" });
        var style = new StyleResolver().Compute(node, new[] { sheet });
        Assert.Equal(RgbaColor.FromRgb(255, 0, 0), style.Color);
    }

    [Fact]
    public void ColorParser_Supports_Rgb_And_Rgba_Functions()
    {
        var css = ".x { color: rgb(1,2,3); background-color: rgba(10,20,30,0.5); }";
        var sheet = CssParser.Parse(css);
        var node = new Node("div", classes: new[] { "x" });
        var style = new StyleResolver().Compute(node, new[] { sheet });
        Assert.Equal(new RgbaColor(1, 2, 3, 255), style.Color);
        Assert.Equal(new RgbaColor(10, 20, 30, 128), style.BackgroundColor);
    }

    [Fact]
    public void Text_Props_Parse_And_Inherit()
    {
        var css = ".parent { font-weight: 700; font-style: italic; text-decoration: underline; } .child { }";
        var sheet = CssParser.Parse(css);
        var parentNode = new Node("div", classes: new[] { "parent" });
        var childNode = new Node("span", classes: new[] { "child" });
        var resolver = new StyleResolver();
        var parent = resolver.Compute(parentNode, new[] { sheet });
        var child = resolver.Compute(childNode, new[] { sheet }, parent: parent);
        Assert.Equal(FontWeight.Bold, parent.FontWeight);
        Assert.Equal(FontStyle.Italic, parent.FontStyle);
        Assert.Equal(TextDecoration.Underline, parent.TextDecoration);
        Assert.Equal(parent.FontWeight, child.FontWeight);
        Assert.Equal(parent.FontStyle, child.FontStyle);
        Assert.Equal(parent.TextDecoration, child.TextDecoration);
    }

    [Fact]
    public void CssParser_PseudoClass_Hover_Matches_BackgroundColor()
    {
        var css = "button:hover { background-color: #00ff00; }";
        var sheet = CssParser.Parse(css);
        var node = new Node("button") { IsHover = true };
        var style = new StyleResolver().Compute(node, new[] { sheet });
        Assert.Equal(RgbaColor.FromRgb(0, 255, 0), style.BackgroundColor);
    }

    [Fact]
    public void Media_MinWidth_Gates_Rule()
    {
        var css = "@media(min-width: 100) .wide { color: blue; }";
        var sheet = CssParser.Parse(css);
        var node = new Node("div", classes: new[] { "wide" });
        var resolver = new StyleResolver();
        var styleNarrow = resolver.Compute(node, new[] { sheet }, new EnvironmentContext { ViewportWidth = 50 });
        var styleWide = resolver.Compute(node, new[] { sheet }, new EnvironmentContext { ViewportWidth = 150 });
        Assert.NotEqual(styleNarrow.Color, styleWide.Color);
        Assert.Equal(RgbaColor.FromRgb(0, 0, 255), styleWide.Color);
    }

    [Fact]
    public void StyleCache_Invalidates_On_Env_Change()
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

        // Change env and invalidate
        cache.InvalidateForEnvChange(envNarrow, envWide);
        var styleWide2 = cache.GetComputedStyle(node, new[] { sheet }, envWide);
        Assert.Equal(styleWide1.Color, styleWide2.Color);
    }

    [Fact]
    public void Inheritance_Color_Falls_Back_To_Parent()
    {
        var css = ".child { }"; // no color set
        var sheet = CssParser.Parse(css);
        var node = new Node("div", classes: new[] { "child" });
        var parentStyle = ResolvedStyle.Default with { Color = RgbaColor.FromRgb(10, 20, 30) };
        var style = new StyleResolver().Compute(node, new[] { sheet }, env: null, parent: parentStyle);
        Assert.Equal(parentStyle.Color, style.Color);
    }
}
