namespace Andy.Tui.Core;

[System.Flags]
public enum PseudoState
{
    None = 0,
    Focus = 1 << 0,
    Hover = 1 << 1,
    Active = 1 << 2,
}

public sealed class PseudoStateRegistry
{
    private readonly Dictionary<int, PseudoState> _map = new();

    public PseudoState Get(int nodeId) => _map.TryGetValue(nodeId, out var s) ? s : PseudoState.None;
    public void Set(int nodeId, PseudoState state) => _map[nodeId] = state;
    public void Add(int nodeId, PseudoState state) => _map[nodeId] = Get(nodeId) | state;
    public void Remove(int nodeId, PseudoState state) => _map[nodeId] = Get(nodeId) & ~state;
}