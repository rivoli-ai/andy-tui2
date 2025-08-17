using System.Collections.Generic;
using Andy.Tui.Style;

namespace Andy.Tui.Style.Tests;

public class SpecificityTests
{
    [Fact]
    public void IdBeatsClassBeatsType_WithSourceOrderTiebreak()
    {
        var node = new Node(type: "div", id: "a", classes: new[] { "x" });

        var typeRule = new Rule(new TypeSelector("div"),
            new Dictionary<string, object> { { "color", RgbaColor.FromRgb(255, 0, 0) } }, 1);
        var classRule = new Rule(new ClassSelector("x"),
            new Dictionary<string, object> { { "color", RgbaColor.FromRgb(0, 255, 0) } }, 1);
        var idRuleEarlier = new Rule(new IdSelector("a"),
            new Dictionary<string, object> { { "color", RgbaColor.FromRgb(0, 0, 255) } }, 0);

        var sheet = new Stylesheet(new[] { typeRule, classRule, idRuleEarlier });
        var resolver = new StyleResolver();
        var style = resolver.Compute(node, new[] { sheet });

        Assert.Equal(RgbaColor.FromRgb(0, 0, 255), style.Color);

        // When specificity ties, later wins
        var typeRuleLater = new Rule(new TypeSelector("div"),
            new Dictionary<string, object> { { "background-color", RgbaColor.FromRgb(1, 2, 3) } }, 2);
        var sheet2 = new Stylesheet(new[] { typeRule, typeRuleLater });
        var style2 = resolver.Compute(node, new[] { sheet2 });
        Assert.Equal(RgbaColor.FromRgb(1, 2, 3), style2.BackgroundColor);
    }

    [Fact]
    public void Compare_Tuple_Lexicographic()
    {
        var a = new Specificity(0, 2, 0);
        var b = new Specificity(0, 1, 10);
        Assert.True(a > b);
        Assert.True(!(b > a));
    }
}
