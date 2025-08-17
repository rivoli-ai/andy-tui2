using System.Collections.Generic;

namespace Andy.Tui.Style.Tests;

public class SelectorEdgeCaseTests
{
    [Fact]
    public void TypeSelector_Is_Case_Insensitive()
    {
        var node = new Node("Button");
        Assert.True(new TypeSelector("button").Matches(node));
    }

    [Fact]
    public void IdSelector_Is_Case_Sensitive()
    {
        var node = new Node("div", id: "Save");
        Assert.True(new IdSelector("Save").Matches(node));
        Assert.False(new IdSelector("save").Matches(node));
    }

    [Fact]
    public void ClassSelector_Matches_When_Class_Present()
    {
        var node = new Node("div", classes: new[] { "primary", "rounded" });
        Assert.True(new ClassSelector("primary").Matches(node));
        Assert.False(new ClassSelector("ghost").Matches(node));
    }
}
