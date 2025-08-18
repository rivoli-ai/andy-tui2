using Andy.Tui.Input;

namespace Andy.Tui.Input.Tests;

public class EventRouterTests
{
    [Fact]
    public void Capture_Stops_Bubble_When_Handled()
    {
        var r = new EventRouter();
        bool captureCalled = false; bool bubbleCalled = false;
        r.AddCapture(ev => { captureCalled = true; return true; });
        r.AddBubble(ev => { bubbleCalled = true; return false; });
        var handled = r.Route(new KeyEvent("X", "X", KeyModifiers.None));
        Assert.True(handled);
        Assert.True(captureCalled);
        Assert.False(bubbleCalled);
    }

    [Fact]
    public void Bubble_Receives_When_Not_Handled_In_Capture()
    {
        var r = new EventRouter();
        bool bubbleCalled = false;
        r.AddCapture(ev => false);
        r.AddBubble(ev => { bubbleCalled = true; return true; });
        var handled = r.Route(new KeyEvent("X", "X", KeyModifiers.None));
        Assert.True(handled);
        Assert.True(bubbleCalled);
    }

    [Fact]
    public void Bubble_Orders_Last_Registered_First()
    {
        var r = new EventRouter();
        var calls = new List<int>();
        r.AddBubble(ev => { calls.Add(1); return false; });
        r.AddBubble(ev => { calls.Add(2); return true; });
        var handled = r.Route(new KeyEvent("X", "X", KeyModifiers.None));
        Assert.True(handled);
        // Because the second handler returns true, routing stops after it
        Assert.Equal(new[] { 2 }, calls);

        // If neither handles, both are called in reverse order
        calls.Clear();
        r = new EventRouter();
        r.AddBubble(ev => { calls.Add(1); return false; });
        r.AddBubble(ev => { calls.Add(2); return false; });
        handled = r.Route(new KeyEvent("X", "X", KeyModifiers.None));
        Assert.False(handled);
        Assert.Equal(new[] { 2, 1 }, calls);
    }

    [Fact]
    public void Capture_Then_Bubble_With_Midchain_Cancel()
    {
        var r = new EventRouter();
        var calls = new List<string>();
        r.AddCapture(ev => { calls.Add("c1"); return false; });
        r.AddCapture(ev => { calls.Add("c2"); return false; });
        r.AddBubble(ev => { calls.Add("b1"); return false; });
        r.AddBubble(ev => { calls.Add("b2"); return true; });
        r.AddBubble(ev => { calls.Add("b3"); return false; });
        var handled = r.Route(new KeyEvent("X", "X", KeyModifiers.None));
        Assert.True(handled);
        Assert.Equal(new[] { "c1", "c2", "b3", "b2" }, calls);
        // Explanation: capture runs first (c1,c2), then bubble in reverse registration order (b3 then b2); b2 handles and stops
    }
}
