using System.Collections.Generic;
using Andy.Tui.Style;

namespace Andy.Tui.Style.Tests;

public class SelectorMatchingTests
{
    [Fact]
    public void Type_Class_Id_Selectors_Match_As_Expected()
    {
        var node = new Node(type: "button", id: "ok", classes: new[] { "primary", "rounded" });
        Assert.True(new TypeSelector("button").Matches(node));
        Assert.True(new ClassSelector("primary").Matches(node));
        Assert.False(new ClassSelector("ghost").Matches(node));
        Assert.True(new IdSelector("ok").Matches(node));
        Assert.False(new IdSelector("cancel").Matches(node));
    }

    [Fact]
    public void Specificity_Order_Is_Correct_Tuple_Compare()
    {
        var type = new TypeSelector("div").Specificity;       // (0,0,1)
        var @class = new ClassSelector("x").Specificity;      // (0,1,0)
        var id = new IdSelector("a").Specificity;             // (1,0,0)
        Assert.True(id > @class);
        Assert.True(@class > type);
        Assert.True(id > type);
    }

    [Fact]
    public void Matches_Pseudo_Classes()
    {
        var node = new Node("div") { IsHover = true, IsFocus = false, IsActive = true, IsDisabled = false };
        Assert.True(new PseudoClassSelector(":hover").Matches(node));
        Assert.False(new PseudoClassSelector(":focus").Matches(node));
        Assert.True(new PseudoClassSelector(":active").Matches(node));
        Assert.False(new PseudoClassSelector(":disabled").Matches(node));
    }
}
