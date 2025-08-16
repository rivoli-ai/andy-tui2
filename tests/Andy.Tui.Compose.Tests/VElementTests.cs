using Andy.Tui.Compose;
using Xunit;

namespace Andy.Tui.Compose.Tests;

public class VElementTests
{
    [Fact]
    public void VElement_Has_Type()
    {
        var el = new VElement("stack");
        Assert.Equal("stack", el.Type);
    }
}
