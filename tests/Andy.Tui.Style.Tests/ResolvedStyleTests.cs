using System.Collections.Generic;
using Andy.Tui.Style;

namespace Andy.Tui.Style.Tests;

public class ResolvedStyleTests
{
    [Fact]
    public void Length_And_LengthOrAuto_Basics()
    {
        var a = LengthOrAuto.FromPixels(5);
        Assert.False(a.IsAuto);
        Assert.Equal(5, a.Value!.Value.Pixels);

        var b = LengthOrAuto.Auto();
        Assert.True(b.IsAuto);
    }

    [Fact]
    public void FlexDefaults_And_Overrides()
    {
        var node = new Node("div");
        var resolver = new StyleResolver();
        var style = resolver.Compute(node, new[] { new Stylesheet(System.Array.Empty<Rule>()) });
        Assert.Equal(Display.Flex, style.Display);
        Assert.Equal(FlexDirection.Row, style.FlexDirection);
        Assert.Equal(0, style.Order);
        Assert.Equal(FontWeight.Normal, style.FontWeight);
        Assert.Equal(FontStyle.Normal, style.FontStyle);
        Assert.Equal(TextDecoration.None, style.TextDecoration);

        var rule = new Rule(new TypeSelector("div"), new Dictionary<string, object> { { "order", 10 }, { "flex-direction", FlexDirection.Column } }, 0);
        var style2 = resolver.Compute(node, new[] { new Stylesheet(new[] { rule }) });
        Assert.Equal(10, style2.Order);
        Assert.Equal(FlexDirection.Column, style2.FlexDirection);
    }

    [Fact]
    public void Shorthand_Longhand_Padding_And_Margin()
    {
        var node = new Node("div");
        var sheet = new Stylesheet(new[]
        {
            new Rule(new TypeSelector("div"), new Dictionary<string, object>{{"padding", new Thickness(new Length(1), new Length(2), new Length(3), new Length(4))}}, 0),
            new Rule(new TypeSelector("div"), new Dictionary<string, object>{{"margin-top", new Length(5)}}, 1),
        });

        var resolver = new StyleResolver();
        var style = resolver.Compute(node, new[] { sheet });
        Assert.Equal(1, style.Padding.Left.Pixels);
        Assert.Equal(2, style.Padding.Top.Pixels);
        Assert.Equal(3, style.Padding.Right.Pixels);
        Assert.Equal(4, style.Padding.Bottom.Pixels);
        Assert.Equal(5, style.Margin.Top.Pixels);
    }

    [Fact]
    public void Variables_Resolve_With_Fallback()
    {
        var node = new Node("div");
        var sheet = new Stylesheet(new[]
        {
            new Rule(new TypeSelector("div"), new Dictionary<string, object>{{"--main-gap", 6}, {"row-gap", "var(--main-gap, 2)"}}, 0),
            new Rule(new TypeSelector("div"), new Dictionary<string, object>{{"column-gap", "var(--missing, 3)"}}, 0),
        });

        var resolver = new StyleResolver();
        var style = resolver.Compute(node, new[] { sheet });
        Assert.Equal(6, style.RowGap.Pixels);
        Assert.Equal(3, style.ColumnGap.Pixels);
    }

    [Fact]
    public void Shorthand_Wins_Over_Weaker_Longhand_But_Loses_To_Stronger_Longhand()
    {
        var node = new Node("div");
        var weakLonghand = new Rule(new TypeSelector("div"), new Dictionary<string, object> { { "padding-left", new Length(9) } }, 0);
        var shorthand = new Rule(new TypeSelector("div"), new Dictionary<string, object> { { "padding", new Thickness(new Length(1), new Length(1), new Length(1), new Length(1)) } }, 1);
        var strongLonghand = new Rule(new ClassSelector("x"), new Dictionary<string, object> { { "padding-left", new Length(7) } }, 0);

        var resolver = new StyleResolver();
        var style1 = resolver.Compute(node, new[] { new Stylesheet(new[] { weakLonghand, shorthand }) });
        Assert.Equal(1, style1.Padding.Left.Pixels); // shorthand beats earlier weak longhand

        var style2 = resolver.Compute(new Node("div", classes: new[] { "x" }), new[] { new Stylesheet(new[] { shorthand, strongLonghand }) });
        Assert.Equal(7, style2.Padding.Left.Pixels); // class longhand beats type shorthand
    }

    [Fact]
    public void Gap_Row_And_Column_Shortcut_And_Longhand_Precedence()
    {
        var node = new Node("div");
        var sheet = new Stylesheet(new[]
        {
            new Rule(new TypeSelector("div"), new Dictionary<string, object>{{"gap", 6}}, 0)
        });

        var resolver = new StyleResolver();
        var style = resolver.Compute(node, new[] { sheet });
        Assert.Equal(6, style.RowGap.Pixels);
        Assert.Equal(6, style.ColumnGap.Pixels);

        // Longhand later with higher precedence should override
        var sheet2 = new Stylesheet(new[]
        {
            new Rule(new TypeSelector("div"), new Dictionary<string, object>{{"gap", 4}}, 0),
            new Rule(new ClassSelector("x"), new Dictionary<string, object>{{"row-gap", 2}}, 0),
        });
        var style2 = resolver.Compute(new Node("div", classes: new[] { "x" }), new[] { sheet2 });
        Assert.Equal(2, style2.RowGap.Pixels);
        Assert.Equal(4, style2.ColumnGap.Pixels);
    }

    [Fact]
    public void Keywords_Inherit_Initial_Unset_For_Colors()
    {
        var node = new Node("div");
        var parentStyle = ResolvedStyle.Default with { Color = RgbaColor.FromRgb(10, 20, 30), BackgroundColor = RgbaColor.FromRgb(1, 2, 3) };

        // inherit for color should pick from parent even if property is explicitly set
        var sheetInherit = new Stylesheet(new[]
        {
            new Rule(new TypeSelector("div"), new Dictionary<string, object>{{"color", "inherit"}}, 0)
        });
        var sInherit = new StyleResolver().Compute(node, new[] { sheetInherit }, parent: parentStyle);
        Assert.Equal(parentStyle.Color, sInherit.Color);

        // initial for background-color should reset to initial regardless of parent
        var sheetInitial = new Stylesheet(new[]
        {
            new Rule(new TypeSelector("div"), new Dictionary<string, object>{{"background-color", "initial"}}, 0)
        });
        var sInitial = new StyleResolver().Compute(node, new[] { sheetInitial }, parent: parentStyle);
        Assert.Equal(ResolvedStyle.Default.BackgroundColor, sInitial.BackgroundColor);

        // unset behaves like inherit for inheritable (color) and initial for non-inheritable (background-color)
        var sheetUnset = new Stylesheet(new[]
        {
            new Rule(new TypeSelector("div"), new Dictionary<string, object>{{"color", "unset"}, {"background-color", "unset"}}, 0)
        });
        var sUnset = new StyleResolver().Compute(node, new[] { sheetUnset }, parent: parentStyle);
        Assert.Equal(parentStyle.Color, sUnset.Color);
        Assert.Equal(ResolvedStyle.Default.BackgroundColor, sUnset.BackgroundColor);
    }
}
