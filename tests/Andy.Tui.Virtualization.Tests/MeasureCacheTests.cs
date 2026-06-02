namespace Andy.Tui.Virtualization.Tests;

public class MeasureCacheTests
{
    [Fact]
    public void TryGet_Returns_False_When_Key_Absent()
    {
        var cache = new MeasureCache();
        Assert.False(cache.TryGet("missing", out int height));
        Assert.Equal(0, height);
    }

    [Fact]
    public void Set_Then_TryGet_Returns_Stored_Height()
    {
        var cache = new MeasureCache();
        cache.Set("row-1", 5);
        Assert.True(cache.TryGet("row-1", out int height));
        Assert.Equal(5, height);
    }

    [Fact]
    public void Set_Overwrites_Existing_Height()
    {
        var cache = new MeasureCache();
        cache.Set("row-1", 5);
        cache.Set("row-1", 9);
        Assert.True(cache.TryGet("row-1", out int height));
        Assert.Equal(9, height);
    }
}
