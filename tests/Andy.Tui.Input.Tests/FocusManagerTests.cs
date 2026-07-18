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
        Assert.Contains(fm.ActiveId!.Value, new[] { 10, 11 });
        var first = fm.ActiveId;
        fm.FocusNext();
        Assert.Contains(fm.ActiveId!.Value, new[] { 10, 11 });
        Assert.NotEqual(first, fm.ActiveId);
        fm.PopScope();
        // after popping, global traversal resumes
        fm.FocusNext();
        Assert.NotNull(fm.ActiveId);
    }

    [Fact]
    public void Entering_Scope_Moves_Focus_Inside()
    {
        var fm = new FocusManager();
        fm.Register(1); fm.Register(2);
        Assert.Equal(1, fm.ActiveId);
        fm.PushScope();
        fm.Register(10);
        // Modal entry: focus moves into the scope's first member.
        Assert.Equal(10, fm.ActiveId);
    }

    [Fact]
    public void Popping_Scope_Restores_Previous_Focus()
    {
        var fm = new FocusManager();
        fm.Register(1); fm.Register(2);
        fm.FocusNext(); // active = 2
        Assert.Equal(2, fm.ActiveId);
        fm.PushScope();
        fm.Register(10); fm.Register(11);
        Assert.Contains(fm.ActiveId!.Value, new[] { 10, 11 });
        fm.PopScope();
        // Focus is restored to what was active before entering the scope.
        Assert.Equal(2, fm.ActiveId);
    }

    [Fact]
    public void Popping_Scope_Does_Not_Restore_Removed_Node()
    {
        var fm = new FocusManager();
        fm.Register(1); fm.Register(2);
        fm.FocusNext(); // active = 2 (this will be removed while the scope is open)
        fm.PushScope();
        fm.Register(10);
        Assert.Equal(10, fm.ActiveId);
        // The previously active node is removed while inside the modal scope.
        fm.Unregister(2);
        fm.PopScope();
        // It must not restore focus to the now-inaccessible node 2.
        Assert.NotEqual(2, fm.ActiveId);
        Assert.NotNull(fm.ActiveId);
        Assert.Contains(fm.ActiveId!.Value, new[] { 1, 10 });
    }

    [Fact]
    public void Removing_Active_In_Scope_Stays_In_Scope()
    {
        var fm = new FocusManager();
        fm.Register(1);
        fm.PushScope();
        fm.Register(10); fm.Register(11);
        Assert.Equal(10, fm.ActiveId);
        // Removing the active scoped node moves focus to another node in the same scope,
        // never to the global node 1.
        fm.Unregister(10);
        Assert.Equal(11, fm.ActiveId);
    }

    [Fact]
    public void Nested_Scopes_Contain_Traversal_And_Restore()
    {
        var fm = new FocusManager();
        fm.Register(1);
        fm.PushScope();      // outer modal
        fm.Register(10); fm.Register(11);
        fm.PushScope();      // inner modal
        fm.Register(20); fm.Register(21);
        Assert.Contains(fm.ActiveId!.Value, new[] { 20, 21 });
        fm.FocusNext();
        Assert.Contains(fm.ActiveId!.Value, new[] { 20, 21 });
        fm.PopScope();       // exit inner -> restore to outer's active member
        Assert.Contains(fm.ActiveId!.Value, new[] { 10, 11 });
        fm.PopScope();       // exit outer -> restore to global node 1
        Assert.Equal(1, fm.ActiveId);
    }
}
