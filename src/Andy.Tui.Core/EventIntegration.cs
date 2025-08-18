using Andy.Tui.Input;
using Andy.Tui.Layout;

namespace Andy.Tui.Core;

public sealed class EventIntegration
{
    private readonly InvalidationBus _bus;
    private readonly PseudoStateRegistry _states;
    private readonly FocusManager _focus;
    private readonly Func<IReadOnlyDictionary<int, Rect>> _getRects;
    public EventIntegration(InvalidationBus bus, PseudoStateRegistry states, FocusManager focus, Func<IReadOnlyDictionary<int, Rect>> getRects)
    {
        _bus = bus; _states = states; _focus = focus; _getRects = getRects;
    }

    public bool Handle(IInputEvent ev)
    {
        switch (ev)
        {
            case KeyEvent ke:
                if (ke.Key == "Tab") { _focus.FocusNext(); _bus.RequestRecompose(); return true; }
                if (ke.Key == "Shift+Tab") { _focus.FocusPrevious(); _bus.RequestRecompose(); return true; }
                return false;
            case ResizeEvent re:
                // Trigger a recompose/layout on resize
                _bus.RequestRecompose();
                return true;
            case MouseEvent me:
                var rects = _getRects();
                var nodeId = HitTest.HitAt(rects, me.X, me.Y);
                // Remove hover from nodes that are no longer under the pointer
                if (me.Kind == MouseKind.Move)
                {
                    foreach (var kv in rects)
                    {
                        bool inside = kv.Key == nodeId;
                        if (!inside)
                        {
                            _states.Remove(kv.Key, PseudoState.Hover);
                        }
                    }
                }
                if (nodeId != null)
                {
                    if (me.Kind == MouseKind.Move)
                    {
                        _states.Add(nodeId.Value, PseudoState.Hover);
                    }
                    else if (me.Kind == MouseKind.Down)
                    {
                        _states.Add(nodeId.Value, PseudoState.Active);
                    }
                    else if (me.Kind == MouseKind.Up)
                    {
                        _states.Remove(nodeId.Value, PseudoState.Active);
                    }
                    _bus.RequestRecompose();
                    return true;
                }
                return false;
            default:
                return false;
        }
    }
}
