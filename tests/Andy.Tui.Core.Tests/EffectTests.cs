using Andy.Tui.Core.Reactive;
using Xunit;

namespace Andy.Tui.Core.Tests;

public class EffectTests
{
    [Fact]
    public void Effect_Runs_Action()
    {
        int calls = 0;
        using var e = new Effect(() => calls++);
        Assert.Equal(1, calls);
        e.Run();
        Assert.Equal(2, calls);
    }
}
