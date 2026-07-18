using Andy.Tui.Core;
using Andy.Tui.Input;
using Andy.Tui.Layout;

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
        // Move to empty area: clears hover from 1 and, because that is a visible
        // pseudo-state change, requests a recompose so the frame no longer shows hover.
        requested = false;
        Assert.True(integ.Handle(new MouseEvent(MouseKind.Move, 10, 0, MouseButton.None, KeyModifiers.None)));
        Assert.Equal(PseudoState.None, states.Get(1));
        Assert.True(requested);
        // A second move over the same empty area produces no state change and no recompose.
        requested = false;
        Assert.False(integ.Handle(new MouseEvent(MouseKind.Move, 10, 0, MouseButton.None, KeyModifiers.None)));
        Assert.False(requested);
    }

    [Fact]
    public void Release_Outside_Pressed_Node_Clears_Active()
    {
        var bus = new InvalidationBus();
        var states = new PseudoStateRegistry();
        var focus = new FocusManager();
        var rects = new Dictionary<int, Rect> { { 1, new Rect(0, 0, 2, 1) }, { 2, new Rect(3, 0, 2, 1) } };
        var integ = new EventIntegration(bus, states, focus, () => rects);

        // Press node 1 -> Active.
        Assert.True(integ.Handle(new MouseEvent(MouseKind.Down, 1, 0, MouseButton.Left, KeyModifiers.None)));
        Assert.True(states.Get(1).HasFlag(PseudoState.Active));

        // Release far outside any node -> pointer capture clears Active on node 1.
        bool requested = false; bus.RecomposeRequested += () => requested = true;
        integ.Handle(new MouseEvent(MouseKind.Up, 99, 99, MouseButton.Left, KeyModifiers.None));
        Assert.False(states.Get(1).HasFlag(PseudoState.Active));
        Assert.True(requested);
    }

    [Fact]
    public void Second_Down_Before_Up_Does_Not_Strand_First_Nodes_Active()
    {
        var bus = new InvalidationBus();
        var states = new PseudoStateRegistry();
        var focus = new FocusManager();
        var rects = new Dictionary<int, Rect> { { 1, new Rect(0, 0, 2, 1) }, { 2, new Rect(3, 0, 2, 1) } };
        var integ = new EventIntegration(bus, states, focus, () => rects);

        // Press node 1 -> Active.
        integ.Handle(new MouseEvent(MouseKind.Down, 1, 0, MouseButton.Left, KeyModifiers.None));
        Assert.True(states.Get(1).HasFlag(PseudoState.Active));

        // A second Down on node 2 with the same button arrives before node 1 saw its Up
        // (e.g. a lost release). Node 1's Active must be cleared, not left stranded.
        integ.Handle(new MouseEvent(MouseKind.Down, 3, 0, MouseButton.Left, KeyModifiers.None));
        Assert.False(states.Get(1).HasFlag(PseudoState.Active));
        Assert.True(states.Get(2).HasFlag(PseudoState.Active));

        // The Up for node 2 clears the correct (current) capture and leaves nothing stuck.
        integ.Handle(new MouseEvent(MouseKind.Up, 3, 0, MouseButton.Left, KeyModifiers.None));
        Assert.False(states.Get(1).HasFlag(PseudoState.Active));
        Assert.False(states.Get(2).HasFlag(PseudoState.Active));
    }

    [Fact]
    public void Resize_Propagates_New_Dimensions_To_Viewport()
    {
        var bus = new InvalidationBus();
        var states = new PseudoStateRegistry();
        var focus = new FocusManager();
        var viewport = new CoreViewportState(80, 24);
        var integ = new EventIntegration(bus, states, focus, () => new Dictionary<int, Rect>(), viewport);
        bool requested = false; bus.RecomposeRequested += () => requested = true;

        Assert.True(integ.Handle(new ResizeEvent(120, 40)));
        Assert.Equal((120, 40), viewport.Size);
        Assert.True(requested);
    }

    [Fact]
    public void Overlapping_Nodes_Route_To_Top_Painted_Target()
    {
        var bus = new InvalidationBus();
        var states = new PseudoStateRegistry();
        var focus = new FocusManager();
        // Node 2 is painted after node 1 and overlaps it; node 3 is clipped away from the point.
        IReadOnlyList<HitTestNode> nodes = new List<HitTestNode>
        {
            new HitTestNode(1, new Rect(0, 0, 10, 10)),
            new HitTestNode(2, new Rect(0, 0, 5, 5)),
            new HitTestNode(3, new Rect(0, 0, 5, 5), Clip: new Rect(0, 0, 1, 1)),
        };
        var integ = new EventIntegration(bus, states, focus, () => nodes);

        // Point (2,2) is inside 1, 2, and 3's bounds, but 3 is clipped out and 2 is topmost.
        Assert.True(integ.Handle(new MouseEvent(MouseKind.Move, 2, 2, MouseButton.None, KeyModifiers.None)));
        Assert.True(states.Get(2).HasFlag(PseudoState.Hover));
        Assert.False(states.Get(1).HasFlag(PseudoState.Hover));
        Assert.False(states.Get(3).HasFlag(PseudoState.Hover));
    }

    [Fact]
    public void ShiftTab_Moves_Focus_Backwards()
    {
        var bus = new InvalidationBus();
        var states = new PseudoStateRegistry();
        var focus = new FocusManager();
        focus.Register(1); focus.Register(2); focus.Register(3);
        focus.FocusNext(); // 2
        var integ = new EventIntegration(bus, states, focus, () => new Dictionary<int, Rect>());

        Assert.True(integ.Handle(new KeyEvent("Tab", "Tab", KeyModifiers.Shift)));
        Assert.Equal(1, focus.ActiveId);
    }
}
