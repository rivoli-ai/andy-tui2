using Andy.Tui.Animations;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Animations.Tests;

public class OpacityTransitionApplierTests
{
    [Fact]
    public void Lerps_Fg_Toward_Bg_By_Opacity()
    {
        var run = new TextRun(0, 0, "A", new Rgb24(100, 0, 0), new Rgb24(0, 0, 0), CellAttrFlags.None);
        var mid = OpacityTransitionApplier.Apply(run, 0, 500, 1000, 0.0, 1.0);
        Assert.Equal(new Rgb24(50, 0, 0), mid.Fg);
    }
}
