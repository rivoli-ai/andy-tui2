using Andy.Tui.Compose;
using Xunit;

namespace Andy.Tui.Compose.Tests;

file sealed class HelloView : View
{
    public override VNode Build()
    {
        var root = new VElement("root");
        root.AddChild(new VText("hi"));
        return root;
    }
}

public class ViewTests
{
    [Fact]
    public void Build_Constructs_Tree()
    {
        var v = new HelloView();
        var node = v.Build();
        var el = Assert.IsType<VElement>(node);
        Assert.Single(el.Children);
        Assert.IsType<VText>(el.Children[0]);
    }
}
