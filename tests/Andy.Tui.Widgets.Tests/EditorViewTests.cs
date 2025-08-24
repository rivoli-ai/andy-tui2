using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class EditorViewTests
{
    [Fact]
    public void EditorView_Renders_Lines_And_Caret()
    {
        var ev = new Andy.Tui.Widgets.EditorView();
        ev.SetText("hello\nworld\nfrom\neditor");
        ev.SetCursor(1, 3); // on 'world', after 'wor'

        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        ev.Render(new L.Rect(0, 0, 10, 3), baseDl, b);
        var dl = b.Build();

        var texts = dl.Ops.OfType<DL.TextRun>().ToList();
        Assert.Contains(texts, t => t.Y == 0 && t.Content.Contains("hello"));
        Assert.Contains(texts, t => t.Y == 1 && t.Content.Contains("world"));
        // caret on row 1
        Assert.Contains(texts, t => t.Y == 1 && t.Content == "|");
    }

    [Fact]
    public void EditorView_Caret_Scrolls_Into_View()
    {
        var ev = new Andy.Tui.Widgets.EditorView();
        ev.SetText(string.Join('\n', Enumerable.Range(1, 50).Select(i => $"Line {i}")));
        ev.SetCursor(20, 1);
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        ev.Render(new L.Rect(0, 0, 12, 3), baseDl, b);
        var dl = b.Build();
        var texts = dl.Ops.OfType<DL.TextRun>().ToList();
        // Expect that line 20 is visible after ensuring cursor is visible
        Assert.Contains(texts, t => t.Content.Contains("Line 20"));
    }
}
