using Andy.Tui.Compose;
using Xunit;

namespace Andy.Tui.Compose.Tests;

public class VTextTests
{
    [Fact]
    public void VText_Holds_Text()
    {
        var t = new VText("hello");
        Assert.Equal("hello", t.Text);
    }
}
