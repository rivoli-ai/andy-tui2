using Andy.Tui.Observability;

namespace Andy.Tui.Input;

public delegate bool InputHandler(IInputEvent ev);

public sealed class EventRouter
{
    private readonly List<InputHandler> _capture = new();
    private readonly List<InputHandler> _bubble = new();

    public void AddCapture(InputHandler handler) => _capture.Add(handler);
    public void AddBubble(InputHandler handler) => _bubble.Add(handler);
    public void RemoveCapture(InputHandler handler) => _capture.Remove(handler);
    public void RemoveBubble(InputHandler handler) => _bubble.Remove(handler);

    public bool Route(IInputEvent ev)
    {
        using (Tracer.BeginSpan("route", ev.GetType().Name))
        {
            // Capture phase
            foreach (var h in _capture)
            {
                if (h(ev)) return true; // handled, stop
            }
            // Bubble phase
            for (int i = _bubble.Count - 1; i >= 0; i--)
            {
                if (_bubble[i](ev)) return true;
            }
            return false;
        }
    }
}
