using Andy.Tui.Input;

namespace Andy.Tui.Input.Tests;

public class FocusManagerTests
{
    [Fact]
    public void Traversal_Wraps_Around()
    {
        var fm = new FocusManager();
        fm.Register(1); fm.Register(2); fm.Register(3);
        Assert.Equal(1, fm.ActiveId);
        fm.FocusNext(); Assert.Equal(2, fm.ActiveId);
        fm.FocusNext(); Assert.Equal(3, fm.ActiveId);
        fm.FocusNext(); Assert.Equal(1, fm.ActiveId);
        fm.FocusPrevious(); Assert.Equal(3, fm.ActiveId);
    }

    [Fact]
    public void Unregister_Active_Moves_To_First()
    {
        var fm = new FocusManager();
        fm.Register(1); fm.Register(2);
        fm.FocusNext(); // 2
        Assert.Equal(2, fm.ActiveId);
        fm.Unregister(2);
        Assert.Equal(1, fm.ActiveId);
    }

    [Fact]
    public void Scopes_Limit_Traversal()
    {
        var fm = new FocusManager();
        fm.Register(1); fm.Register(2); fm.Register(3);
        fm.PushScope();
        fm.Register(10); fm.Register(11); // in scope
        // traversal should stay within scope {10,11}
        fm.FocusNext();
        Assert.Contains(fm.ActiveId!.Value, new[]{10,11});
        var first = fm.ActiveId;
        fm.FocusNext();
        Assert.Contains(fm.ActiveId!.Value, new[]{10,11});
        Assert.NotEqual(first, fm.ActiveId);
        fm.PopScope();
        // after popping, global traversal resumes
        fm.FocusNext();
        Assert.NotNull(fm.ActiveId);
    }
}
