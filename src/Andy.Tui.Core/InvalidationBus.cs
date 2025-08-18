namespace Andy.Tui.Core;

public sealed class InvalidationBus
{
    public event Action? RecomposeRequested;
    public void RequestRecompose() => RecomposeRequested?.Invoke();
}
