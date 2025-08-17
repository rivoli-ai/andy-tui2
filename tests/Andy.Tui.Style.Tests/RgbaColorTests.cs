namespace Andy.Tui.Style.Tests;

public class RgbaColorTests
{
    [Fact]
    public void FromRgb_Sets_Opaque_Alpha()
    {
        var c = RgbaColor.FromRgb(1, 2, 3);
        Assert.Equal((byte)1, c.R);
        Assert.Equal((byte)2, c.G);
        Assert.Equal((byte)3, c.B);
        Assert.Equal((byte)255, c.A);
    }

    [Fact]
    public void Equality_Works_For_RecordStruct()
    {
        var a = RgbaColor.FromRgb(10, 20, 30);
        var b = new RgbaColor(10, 20, 30, 255);
        Assert.Equal(a, b);
    }
}
