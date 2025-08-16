using Andy.Tui.Compose;
using Xunit;

namespace Andy.Tui.Compose.Tests;

public class VNodeTests
{
    [Fact]
    public void VElement_AddChild_Works()
    {
        var root = new VElement("root");
        var child = new VText("hi");
        root.AddChild(child);
        Assert.Single(root.Children);
        Assert.IsType<VText>(root.Children[0]);
    }

    [Fact]
    public void AddChild_Ignores_Null()
    {
        var root = new VElement("root");
        root.AddChild(null!);
        Assert.Empty(root.Children);
    }

    [Fact]
    public void AddChild_Multiple_Maintains_Order()
    {
        var root = new VElement("root");
        root.AddChild(new VText("first"));
        root.AddChild(new VText("second"));
        root.AddChild(new VText("third"));

        Assert.Equal(3, root.Children.Count);
        Assert.Equal("first", ((VText)root.Children[0]).Text);
        Assert.Equal("second", ((VText)root.Children[1]).Text);
        Assert.Equal("third", ((VText)root.Children[2]).Text);
    }
}
