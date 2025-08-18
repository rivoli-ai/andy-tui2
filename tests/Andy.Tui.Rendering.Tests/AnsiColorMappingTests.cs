using Andy.Tui.Backend.Terminal;

namespace Andy.Tui.Rendering.Tests;

public class AnsiColorMappingTests
{
    [Theory]
    [InlineData(255, 0, 0, 196)]
    [InlineData(0, 255, 0, 46)]
    [InlineData(0, 0, 255, 21)]
    [InlineData(255, 255, 0, 226)]
    public void Rgb_To_256_Maps_Into_Color_Cube(byte r, byte g, byte b, int expected)
    {
        var idx = AnsiColorMapping.RgbTo256Color(r, g, b);
        Assert.Equal(expected, idx);
    }

    [Theory]
    [InlineData(255, 0, 0)]
    [InlineData(0, 255, 0)]
    [InlineData(0, 0, 255)]
    public void Rgb_To_16_Returns_Valid_Index(byte r, byte g, byte b)
    {
        var idx = AnsiColorMapping.RgbTo16Color(r, g, b);
        Assert.InRange(idx, 0, 15);
    }
}
