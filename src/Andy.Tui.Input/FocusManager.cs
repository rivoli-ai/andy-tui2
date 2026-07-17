namespace Andy.Tui.Input;

public sealed class FocusManager
{
    private sealed class Scope
    {
        public readonly HashSet<int> Members = new();
        public int? SavedActiveId;
    }

    private readonly LinkedList<int> _globalOrder = new();
    private int? _activeId;
    private readonly Stack<Scope> _scopes = new();

    public int? ActiveId => _activeId;

    /// <summary>
    /// Enter a focus scope (e.g. a modal). The currently active node is remembered so
    /// it can be restored on exit. Once nodes are registered inside the scope, focus is
    /// moved into the scope so traversal stays contained.
    /// </summary>
    public void PushScope() => _scopes.Push(new Scope { SavedActiveId = _activeId });

    /// <summary>
    /// Exit the top focus scope, restoring focus to the node that was active before the
    /// scope was entered (if it still exists). If that node is gone and focus is still
    /// inside the popped scope, fall back to the first eligible node in the outer context.
    /// </summary>
    public void PopScope()
    {
        if (_scopes.Count == 0) return;
        var scope = _scopes.Pop();

        if (scope.SavedActiveId is int saved && _globalOrder.Contains(saved))
        {
            _activeId = saved;
        }
        else if (_activeId is int a && scope.Members.Contains(a) && !CurrentAllowed().Contains(a))
        {
            _activeId = FirstAllowedInOrder();
        }
    }

    public void Register(int nodeId)
    {
        if (!_globalOrder.Contains(nodeId)) _globalOrder.AddLast(nodeId);
        if (_scopes.Count > 0)
        {
            var scope = _scopes.Peek();
            scope.Members.Add(nodeId);
            // Modal entry: if focus is currently outside this scope, move it inside.
            if (_activeId is null || !scope.Members.Contains(_activeId.Value))
                _activeId = nodeId;
        }
        else if (_activeId is null)
        {
            _activeId = nodeId;
        }
    }

    public void Unregister(int nodeId)
    {
        var node = _globalOrder.Find(nodeId);
        if (node != null) _globalOrder.Remove(node);
        foreach (var s in _scopes) s.Members.Remove(nodeId);
        if (_activeId == nodeId)
        {
            // Move to the next eligible node in the current context, if any.
            _activeId = FirstAllowedInOrder();
        }
    }

    public void FocusNext()
    {
        if (_globalOrder.Count == 0) { _activeId = null; return; }
        var allowed = CurrentAllowed();
        if (allowed.Count == 0) { _activeId = null; return; }
        if (_activeId is null || !allowed.Contains(_activeId.Value))
        {
            _activeId = FirstAllowedInOrder();
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
        if (allowed.Count == 0) { _activeId = null; return; }
        if (_activeId is null || !allowed.Contains(_activeId.Value))
        {
            _activeId = FirstAllowedInOrder();
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
        return new HashSet<int>(_scopes.Peek().Members);
    }

    private int? FirstAllowedInOrder()
    {
        var allowed = CurrentAllowed();
        foreach (var id in _globalOrder)
            if (allowed.Contains(id)) return id;
        return null;
    }
}
