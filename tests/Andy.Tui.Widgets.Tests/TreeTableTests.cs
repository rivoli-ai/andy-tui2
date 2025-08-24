using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class TreeTableTests
{
    private Andy.Tui.Widgets.TreeTable.Node N(string s, params Andy.Tui.Widgets.TreeTable.Node[] c) { var n = new Andy.Tui.Widgets.TreeTable.Node(s); n.Children.AddRange(c); return n; }

    [Fact]
    public void Renders_Flat_View_And_Toggle()
    {
        var t = new Andy.Tui.Widgets.TreeTable();
        t.SetRoots(new[]{N("root", N("child")), N("root2")});
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        t.Render(new L.Rect(0,0,40,5), baseDl, b);
        var dl = b.Build();
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), tr => tr.Content.Contains("root"));
        // Toggle expand on first row
        t.MoveCursor(0, 3);
        t.ToggleExpanded();
        b = new DL.DisplayListBuilder();
        t.Render(new L.Rect(0,0,40,5), baseDl, b);
        dl = b.Build();
        var text = string.Join("", dl.Ops.OfType<DL.TextRun>().Select(tr => tr.Content));
        Assert.Contains("child", text);
    }
}
