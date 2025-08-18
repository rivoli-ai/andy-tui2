using Andy.Tui.Animations;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Animations.Tests;

public class InterpolatorsTests
{
    [Fact]
    public void LerpColor_Linear_Midpoint()
    {
        var a = new Rgb24(0, 0, 0);
        var b = new Rgb24(100, 200, 50);
        var m = Interpolators.Lerp(a, b, 0.5);
        Assert.Equal(new Rgb24(50, 100, 25), m);
    }

    [Fact]
    public void Parses_Simple_Color_Transition()
    {
        var from = new Andy.Tui.DisplayList.Rgb24(0,0,0);
        var to = new Andy.Tui.DisplayList.Rgb24(255,255,255);
        var t = TransitionParser.TryParseColor("color 200ms linear", from, to);
        Assert.NotNull(t);
        Assert.Equal(200, t!.DurationMs);
    }
}
