using Andy.Tui.Input;
using Andy.Tui.Layout;

namespace Andy.Tui.Core;

public sealed class EventIntegration
{
    private readonly InvalidationBus _bus;
    private readonly PseudoStateRegistry _states;
    private readonly FocusManager _focus;
    private readonly Func<IReadOnlyList<HitTestNode>> _getNodes;
    private readonly CoreViewportState? _viewport;

    // Pointer capture, tracked per button. Each button remembers the node that received
    // its last Down; that node keeps Active until the matching Up, even if the pointer is
    // released outside the node's bounds. Tracking per button means a second Down on a
    // different button (or a repeat Down that never saw its Up) cannot orphan an earlier
    // node's Active state.
    private readonly Dictionary<MouseButton, int> _pressedNodes = new();

    public EventIntegration(InvalidationBus bus, PseudoStateRegistry states, FocusManager focus, Func<IReadOnlyDictionary<int, Rect>> getRects, CoreViewportState? viewport = null)
        : this(bus, states, focus, ToNodeProvider(getRects), viewport)
    {
    }

    public EventIntegration(InvalidationBus bus, PseudoStateRegistry states, FocusManager focus, Func<IReadOnlyList<HitTestNode>> getNodes, CoreViewportState? viewport = null)
    {
        _bus = bus; _states = states; _focus = focus; _getNodes = getNodes; _viewport = viewport;
    }

    private static Func<IReadOnlyList<HitTestNode>> ToNodeProvider(Func<IReadOnlyDictionary<int, Rect>> getRects)
        => () =>
        {
            var rects = getRects();
            var list = new List<HitTestNode>(rects.Count);
            foreach (var kv in rects)
                list.Add(new HitTestNode(kv.Key, kv.Value));
            return list;
        };

    public bool Handle(IInputEvent ev)
    {
        switch (ev)
        {
            case KeyEvent ke:
                if (ke.Key == "Shift+Tab" || (ke.Key == "Tab" && ke.Modifiers.HasFlag(KeyModifiers.Shift)))
                {
                    _focus.FocusPrevious(); _bus.RequestRecompose(); return true;
                }
                if (ke.Key == "Tab")
                {
                    _focus.FocusNext(); _bus.RequestRecompose(); return true;
                }
                return false;

            case ResizeEvent re:
                // Propagate new dimensions into shared runtime state so the next arranged
                // and rendered frame uses them, then request a recompose/relayout.
                _viewport?.Resize(re.Cols, re.Rows);
                _bus.RequestRecompose();
                return true;

            case MouseEvent me:
                return HandleMouse(me);

            default:
                return false;
        }
    }

    private bool HandleMouse(MouseEvent me)
    {
        var nodes = _getNodes();
        var nodeId = HitTest.HitAt(nodes, me.X, me.Y);
        bool changed = false;

        switch (me.Kind)
        {
            case MouseKind.Move:
                // Clear hover from every node that is no longer under the pointer.
                foreach (var n in nodes)
                {
                    if (n.NodeId != nodeId)
                        changed |= _states.Remove(n.NodeId, PseudoState.Hover);
                }
                if (nodeId != null)
                    changed |= _states.Add(nodeId.Value, PseudoState.Hover);
                break;

            case MouseKind.Down:
                if (nodeId != null)
                {
                    // If this button already holds a capture (its Up was lost, or a second
                    // Down arrived first), release the earlier node so its Active never
                    // gets stranded.
                    if (_pressedNodes.TryGetValue(me.Button, out var previous) && previous != nodeId.Value)
                        changed |= _states.Remove(previous, PseudoState.Active);
                    _pressedNodes[me.Button] = nodeId.Value;
                    changed |= _states.Add(nodeId.Value, PseudoState.Active);
                }
                break;

            case MouseKind.Up:
                // Clear Active from the node captured by this button regardless of the
                // pointer's current position, so a release outside the pressed node never
                // leaves it stuck.
                if (_pressedNodes.TryGetValue(me.Button, out var captured))
                {
                    changed |= _states.Remove(captured, PseudoState.Active);
                    _pressedNodes.Remove(me.Button);
                }
                if (nodeId != null)
                    changed |= _states.Remove(nodeId.Value, PseudoState.Active);
                break;
        }

        // Request invalidation for every visible pseudo-state change.
        if (changed) _bus.RequestRecompose();
        // The event is considered handled when it targets a node or when it produced a
        // visible change (e.g. clearing stale hover after the pointer exits).
        return nodeId != null || changed;
    }
}
