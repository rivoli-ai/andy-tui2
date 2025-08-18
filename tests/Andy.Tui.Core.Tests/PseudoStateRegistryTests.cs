using Andy.Tui.Core;

namespace Andy.Tui.Core.Tests;

public class PseudoStateRegistryTests
{
    [Fact]
    public void Set_Add_Remove_Work()
    {
        var reg = new PseudoStateRegistry();
        Assert.Equal(PseudoState.None, reg.Get(1));
        reg.Set(1, PseudoState.Focus);
        Assert.Equal(PseudoState.Focus, reg.Get(1));
        reg.Add(1, PseudoState.Hover);
        Assert.Equal(PseudoState.Focus | PseudoState.Hover, reg.Get(1));
        reg.Remove(1, PseudoState.Focus);
        Assert.Equal(PseudoState.Hover, reg.Get(1));
    }
}
