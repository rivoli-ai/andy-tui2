using Andy.Tui.Animations;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Animations.Tests;

public class ColorTransitionApplierTests
{
    [Fact]
    public void Applies_Lerped_Fg_Based_On_Time()
    {
        var run = new TextRun(0, 0, "A", new Rgb24(0, 0, 0), null, CellAttrFlags.None);
        var tr = new TransitionColor(new Rgb24(0, 0, 0), new Rgb24(100, 0, 0), 1000);
        var mid = ColorTransitionApplier.Apply(run, startMs: 0, nowMs: 500, tr);
        Assert.Equal(new Rgb24(50, 0, 0), mid.Fg);
        var end = ColorTransitionApplier.Apply(run, startMs: 0, nowMs: 1000, tr);
        Assert.Equal(new Rgb24(100, 0, 0), end.Fg);
    }
}
