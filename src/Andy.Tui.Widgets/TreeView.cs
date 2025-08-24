using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets;

public interface ITreeNode
{
    string Id { get; }
    string Label { get; }
    bool IsLeaf { get; }
    IEnumerable<ITreeNode> Children { get; }
}

public sealed class TreeView
{
    private readonly List<ITreeNode> _roots = new();
    private readonly HashSet<string> _expanded = new();
    private string? _selectedId;

    public void SetRoots(IEnumerable<ITreeNode> roots)
    { _roots.Clear(); _roots.AddRange(roots); }
    public void Expand(string id) => _expanded.Add(id);
    public void Collapse(string id) => _expanded.Remove(id);
    public void Select(string id) => _selectedId = id;

    public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        int x = (int)rect.X; int y = (int)rect.Y; int w = (int)rect.Width; int h = (int)rect.Height;
        builder.PushClip(new DL.ClipPush(x, y, w, h));
        int yy = y;
        foreach (var root in _roots)
        {
            yy = RenderNode(root, 0, x, yy, w, h, builder);
            if (yy >= y + h) break;
        }
        builder.Pop();
    }

    private IEnumerable<(ITreeNode Node, int Depth)> VisibleNodes()
    {
        foreach (var r in _roots)
        {
            foreach (var t in VisibleFrom(r, 0)) yield return t;
        }
    }
    private IEnumerable<(ITreeNode, int)> VisibleFrom(ITreeNode node, int depth)
    {
        yield return (node, depth);
        if (!node.IsLeaf && _expanded.Contains(node.Id))
        {
            foreach (var c in node.Children)
            {
                foreach (var t in VisibleFrom(c, depth + 1)) yield return t;
            }
        }
    }

    public void SelectNext()
    {
        var list = VisibleNodes().Select(t => t.Node.Id).ToList();
        if (list.Count == 0) return;
        int idx = _selectedId == null ? -1 : list.IndexOf(_selectedId);
        idx = Math.Min(list.Count - 1, Math.Max(0, idx + 1));
        _selectedId = list[idx];
    }
    public void SelectPrevious()
    {
        var list = VisibleNodes().Select(t => t.Node.Id).ToList();
        if (list.Count == 0) return;
        int idx = _selectedId == null ? list.Count : list.IndexOf(_selectedId);
        idx = Math.Max(0, idx - 1);
        _selectedId = list[idx];
    }
    public void ToggleExpandSelected()
    {
        if (_selectedId is null) return;
        var node = VisibleNodes().FirstOrDefault(t => t.Node.Id == _selectedId).Node;
        if (node is null || node.IsLeaf) return;
        if (_expanded.Contains(node.Id)) _expanded.Remove(node.Id); else _expanded.Add(node.Id);
    }

    private int RenderNode(ITreeNode node, int depth, int x, int yy, int w, int h, DL.DisplayListBuilder b)
    {
        if (yy >= h) return yy;
        var isSel = _selectedId == node.Id;
        var bg = isSel ? new DL.Rgb24(50, 50, 90) : new DL.Rgb24(20, 20, 20);
        b.DrawRect(new DL.Rect(x, yy, w, 1, bg));
        var marker = node.IsLeaf ? "  " : (_expanded.Contains(node.Id) ? "▾ " : "▸ ");
        var label = new string(' ', depth * 2) + marker + node.Label;
        b.DrawText(new DL.TextRun(x + 1, yy, label, new DL.Rgb24(220, 220, 220), bg, isSel ? DL.CellAttrFlags.Bold : DL.CellAttrFlags.None));
        yy++;
        if (!node.IsLeaf && _expanded.Contains(node.Id))
        {
            foreach (var child in node.Children)
            {
                yy = RenderNode(child, depth + 1, x, yy, w, h, b);
            }
        }
        return yy;
    }
}
