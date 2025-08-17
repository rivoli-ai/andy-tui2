using System.Collections.Generic;
using Andy.Tui.Style;

namespace Andy.Tui.Style.Tests;

public class CascadeTests
{
    [Fact]
    public void Later_Stylesheet_Takes_Priority_On_Tie()
    {
        var node = new Node("div");
        var rule1 = new Rule(new TypeSelector("div"), new Dictionary<string, object> { { "background-color", RgbaColor.FromRgb(255, 0, 0) } }, 0);
        var rule2 = new Rule(new TypeSelector("div"), new Dictionary<string, object> { { "background-color", RgbaColor.FromRgb(0, 255, 0) } }, 0);

        var sheet1 = new Stylesheet(new[] { rule1 });
        var sheet2 = new Stylesheet(new[] { rule2 });

        var resolver = new StyleResolver();
        var style = resolver.Compute(node, new[] { sheet1, sheet2 });

        Assert.Equal(RgbaColor.FromRgb(0, 255, 0), style.BackgroundColor);
    }
}
