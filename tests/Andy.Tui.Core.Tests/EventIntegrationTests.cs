using Andy.Tui.Core;
using Andy.Tui.Input;

namespace Andy.Tui.Core.Tests;

public class EventIntegrationTests
{
    [Fact]
    public void Tab_Advances_Focus_And_Requests_Recompose()
    {
        var bus = new InvalidationBus();
        var states = new PseudoStateRegistry();
        var focus = new FocusManager();
        focus.Register(1); focus.Register(2);
        bool requested = false;
        bus.RecomposeRequested += () => requested = true;
        var ei = new EventIntegration(bus, states, focus, () => new Dictionary<int, Andy.Tui.Layout.Rect> { { 1, new Andy.Tui.Layout.Rect(0, 0, 10, 1) }, { 2, new Andy.Tui.Layout.Rect(0, 1, 10, 1) } });
        var handled = ei.Handle(new KeyEvent("Tab", "Tab", KeyModifiers.None));
        Assert.True(handled);
        Assert.True(requested);
        Assert.Equal(2, focus.ActiveId);
    }
}

public class EventIntegrationMouseTests
{
    [Fact]
    public void Mouse_Move_Sets_And_Clears_Hover()
    {
        var bus = new InvalidationBus();
        bool requested = false; bus.RecomposeRequested += () => requested = true;
        var states = new PseudoStateRegistry();
        var focus = new FocusManager();
        var rects = new Dictionary<int, Andy.Tui.Layout.Rect> { { 1, new Andy.Tui.Layout.Rect(0, 0, 2, 1) }, { 2, new Andy.Tui.Layout.Rect(3, 0, 2, 1) } };
        var integ = new EventIntegration(bus, states, focus, () => rects);
        // Move over node 1
        Assert.True(integ.Handle(new MouseEvent(MouseKind.Move, 1, 0, MouseButton.None, KeyModifiers.None)));
        Assert.Equal(PseudoState.Hover, states.Get(1));
        Assert.True(requested);
        // Move to empty area: clears hover from 1
        requested = false;
        Assert.False(integ.Handle(new MouseEvent(MouseKind.Move, 10, 0, MouseButton.None, KeyModifiers.None)));
        Assert.Equal(PseudoState.None, states.Get(1));
        Assert.False(requested);
    }
}
