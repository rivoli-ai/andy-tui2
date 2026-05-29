using Xunit;
using Andy.Tui.Style;
using Andy.Tui.DisplayList;

public class TransparentColorTests
{
    [Fact]
    public void Transparent_Constant_Is_Alpha_Zero()
    {
        Assert.True(RgbaColor.Transparent.IsTransparent);
        Assert.Equal(0, RgbaColor.Transparent.A);
    }

    [Fact]
    public void Opaque_Color_Is_Not_Transparent()
    {
        Assert.False(RgbaColor.FromRgb(10, 20, 30).IsTransparent);
    }

    [Fact]
    public void ToRgb24_Returns_Null_For_Transparent()
    {
        Assert.Null(RgbaColor.Transparent.ToRgb24());
        Assert.Null(new RgbaColor(10, 20, 30, 0).ToRgb24()); // alpha 0 regardless of rgb
    }

    [Fact]
    public void ToRgb24_Returns_Rgb_For_Opaque()
    {
        Assert.Equal(new Rgb24(10, 20, 30), RgbaColor.FromRgb(10, 20, 30).ToRgb24());
    }

    [Fact]
    public void Parser_Resolves_Transparent_Keyword()
    {
        var css = ".x { background-color: transparent; }";
        var sheet = CssParser.Parse(css);
        var node = new Node("div", classes: new[] { "x" });
        var style = new StyleResolver().Compute(node, new[] { sheet });
        Assert.True(style.BackgroundColor.IsTransparent);
        Assert.Null(style.BackgroundColor.ToRgb24());
    }

    [Fact]
    public void Parser_Resolves_None_Keyword()
    {
        var css = ".x { background-color: none; }";
        var sheet = CssParser.Parse(css);
        var node = new Node("div", classes: new[] { "x" });
        var style = new StyleResolver().Compute(node, new[] { sheet });
        Assert.True(style.BackgroundColor.IsTransparent);
    }

    [Fact]
    public void Parser_Resolves_Rgba_Zero_Alpha_As_Transparent()
    {
        var css = ".x { background-color: rgba(10,20,30,0); }";
        var sheet = CssParser.Parse(css);
        var node = new Node("div", classes: new[] { "x" });
        var style = new StyleResolver().Compute(node, new[] { sheet });
        Assert.True(style.BackgroundColor.IsTransparent);
        Assert.Null(style.BackgroundColor.ToRgb24());
    }
}
