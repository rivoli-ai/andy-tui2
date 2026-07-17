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

    /// <summary>Set the full pseudo-state; returns true if it changed.</summary>
    public bool Set(int nodeId, PseudoState state)
    {
        if (Get(nodeId) == state) return false;
        _map[nodeId] = state;
        return true;
    }

    /// <summary>Add pseudo-state flags; returns true if any flag was newly set.</summary>
    public bool Add(int nodeId, PseudoState state)
    {
        var cur = Get(nodeId);
        var next = cur | state;
        if (next == cur) return false;
        _map[nodeId] = next;
        return true;
    }

    /// <summary>Remove pseudo-state flags; returns true if any flag was cleared.</summary>
    public bool Remove(int nodeId, PseudoState state)
    {
        var cur = Get(nodeId);
        var next = cur & ~state;
        if (next == cur) return false;
        _map[nodeId] = next;
        return true;
    }
}