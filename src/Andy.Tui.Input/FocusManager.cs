namespace Andy.Tui.Input;

public sealed class FocusManager
{
    private readonly LinkedList<int> _globalOrder = new();
    private int? _activeId;
    private readonly Stack<HashSet<int>> _scopes = new();

    public int? ActiveId => _activeId;

    public void PushScope() => _scopes.Push(new HashSet<int>());
    public void PopScope()
    {
        if (_scopes.Count == 0) return;
        var scope = _scopes.Pop();
        if (_activeId is int a && scope.Contains(a))
        {
            _activeId = _globalOrder.First?.Value;
        }
    }

    public void Register(int nodeId)
    {
        if (!_globalOrder.Contains(nodeId)) _globalOrder.AddLast(nodeId);
        if (_scopes.Count > 0) _scopes.Peek().Add(nodeId);
        if (_activeId is null) _activeId = nodeId;
    }

    public void Unregister(int nodeId)
    {
        var node = _globalOrder.Find(nodeId);
        if (node != null) _globalOrder.Remove(node);
        foreach (var s in _scopes) s.Remove(nodeId);
        if (_activeId == nodeId) _activeId = _globalOrder.First?.Value;
    }

    public void FocusNext()
    {
        if (_globalOrder.Count == 0) { _activeId = null; return; }
        var allowed = CurrentAllowed();
        if (_activeId is null)
        {
            _activeId = allowed.FirstOrDefault();
            return;
        }
        var node = _globalOrder.Find(_activeId.Value) ?? _globalOrder.First!;
        var cursor = node.Next ?? _globalOrder.First!;
        for (int i = 0; i < _globalOrder.Count; i++)
        {
            if (allowed.Contains(cursor.Value)) { _activeId = cursor.Value; return; }
            cursor = cursor.Next ?? _globalOrder.First!;
        }
    }

    public void FocusPrevious()
    {
        if (_globalOrder.Count == 0) { _activeId = null; return; }
        var allowed = CurrentAllowed();
        if (_activeId is null)
        {
            _activeId = allowed.FirstOrDefault();
            return;
        }
        var node = _globalOrder.Find(_activeId.Value) ?? _globalOrder.First!;
        var cursor = node.Previous ?? _globalOrder.Last!;
        for (int i = 0; i < _globalOrder.Count; i++)
        {
            if (allowed.Contains(cursor.Value)) { _activeId = cursor.Value; return; }
            cursor = cursor.Previous ?? _globalOrder.Last!;
        }
    }

    private HashSet<int> CurrentAllowed()
    {
        if (_scopes.Count == 0) return new HashSet<int>(_globalOrder);
        return new HashSet<int>(_scopes.Peek());
    }
}
