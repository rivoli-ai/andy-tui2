using Andy.Tui.Core.Reactive;
using Xunit;

namespace Andy.Tui.Core.Tests;

public class ReactiveTests
{
    [Fact]
    public void Signal_Updates_ValueChanged_Fires()
    {
        var s = new Signal<int>(1);
        int observed = 0;
        s.ValueChanged += (_, v) => observed = v;
        s.Value = 2;
        Assert.Equal(2, s.Value);
        Assert.Equal(2, observed);
    }

    [Fact]
    public void Computed_Computes_And_Invalidates()
    {
        var a = new Signal<int>(2);
        var b = new Signal<int>(3);
        var c = new Computed<int>(() => a.Value + b.Value, subscribeDependencies: invalidate =>
        {
            a.ValueChanged += (_, _) => invalidate();
            b.ValueChanged += (_, _) => invalidate();
        });

        Assert.Equal(5, c.Value);
        a.Value = 10;
        Assert.Equal(13, c.Value);
    }
}
