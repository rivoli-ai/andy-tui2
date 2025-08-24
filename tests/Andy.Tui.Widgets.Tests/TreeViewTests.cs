using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

file sealed class Node : Andy.Tui.Widgets.ITreeNode
{
    public string Id { get; }
    public string Label { get; }
    public bool IsLeaf { get; }
    public IEnumerable<Andy.Tui.Widgets.ITreeNode> Children { get; }
    public Node(string id, string label, bool leaf = false, IEnumerable<Node>? children = null)
    { Id = id; Label = label; IsLeaf = leaf; Children = children ?? Enumerable.Empty<Node>(); }
}

public class TreeViewTests
{
    [Fact]
    public void Renders_Expanded_Marker_And_Selected_Bold()
    {
        var tv = new Andy.Tui.Widgets.TreeView();
        var root = new Node("root", "Root", false, new[] { new Node("c1", "Child1", true), new Node("c2", "Child2", true) });
        tv.SetRoots(new[] { root });
        tv.Expand("root");
        tv.Select("c1");
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        tv.Render(new L.Rect(0, 0, 40, 5), baseDl, b);
        var dl = b.Build();
        var runs = dl.Ops.OfType<DL.TextRun>().Select(r => r.Content).ToList();
        Assert.Contains("▾ Root", runs[0]);
        Assert.Contains("Child1", runs[1]);
    }

    [Fact]
    public void Navigation_SelectNextPrev_Moves_Selection()
    {
        var tv = new Andy.Tui.Widgets.TreeView();
        var root = new Node("root", "Root", false, new[] { new Node("c1", "Child1", true), new Node("c2", "Child2", true) });
        tv.SetRoots(new[] { root }); tv.Expand("root"); tv.Select("c1");
        tv.SelectNext(); // should go to c2
        var baseDl = new DL.DisplayListBuilder().Build(); var b = new DL.DisplayListBuilder();
        tv.Render(new L.Rect(0, 0, 40, 5), baseDl, b);
        var dl = b.Build(); var runs = dl.Ops.OfType<DL.TextRun>().ToList();
        Assert.Contains("Child2", runs[2].Content);
    }
}
