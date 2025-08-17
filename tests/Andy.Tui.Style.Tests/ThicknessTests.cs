namespace Andy.Tui.Style.Tests;

public class ThicknessTests
{
    [Fact]
    public void Zero_Returns_All_Zeros()
    {
        var z = Thickness.Zero;
        Assert.Equal(0, z.Left.Pixels);
        Assert.Equal(0, z.Top.Pixels);
        Assert.Equal(0, z.Right.Pixels);
        Assert.Equal(0, z.Bottom.Pixels);
    }
}
