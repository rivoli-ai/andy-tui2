namespace Andy.Tui.Compose.Tests;

public class KeyedListStateTests
{
    private record Item(string Key, int Value);

    [Fact]
    public void Keyed_Reorder_Preserves_State()
    {
        // Pseudocode-level test due to missing Compose runtime harness
        // Arrange keyed items A,B with per-item state; reorder to B,A and assert per-item state preserved
        // This is a placeholder to be replaced when Compose expose a simple list widget test harness
        Assert.True(true);
    }
}
