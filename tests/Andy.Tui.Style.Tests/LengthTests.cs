namespace Andy.Tui.Style.Tests;

public class LengthTests
{
    [Fact]
    public void Zero_Is_Zero()
    {
        Assert.Equal(0, Length.Zero.Pixels);
    }

    [Fact]
    public void Equality_By_Value()
    {
        var a = new Length(5);
        var b = new Length(5);
        Assert.Equal(a, b);
    }
}
