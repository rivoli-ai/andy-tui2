using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class CodeMarkdownDiffTooltipTests
{
    [Fact]
    public void CodeViewer_Renders_Gutter_And_Keywords()
    {
        var cv = new Andy.Tui.Widgets.CodeViewer();
        cv.SetText("using System;\npublic class X { }\nreturn;");
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        cv.Render(new L.Rect(0,0,40,5), baseDl, b);
        var dl = b.Build();
        var text = string.Join("", dl.Ops.OfType<DL.TextRun>().Select(t => t.Content));
        Assert.Contains("using", text);
        Assert.Contains("class", text);
    }

    [Fact]
    public void Markdown_Renders_Headings_And_List()
    {
        var md = new Andy.Tui.Widgets.MarkdownRenderer();
        md.SetText("# H1\n- item");
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        md.Render(new L.Rect(0,0,20,3), baseDl, b);
        var dl = b.Build();
        var text = string.Join("", dl.Ops.OfType<DL.TextRun>().Select(t => t.Content));
        Assert.Contains("H1", text);
        Assert.Contains("•", text);
    }

    [Fact]
    public void DiffViewer_Colors_Differences()
    {
        var dv = new Andy.Tui.Widgets.DiffViewer();
        dv.SetLeft("a\nb");
        dv.SetRight("a\nB");
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        dv.Render(new L.Rect(0,0,20,5), baseDl, b);
        var dl = b.Build();
        Assert.True(dl.Ops.OfType<DL.Rect>().Any());
    }

    [Fact]
    public void Tooltip_Measures_And_Renders()
    {
        var t = new Andy.Tui.Widgets.Tooltip();
        t.SetText("Hello");
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        t.RenderAt(0,0, baseDl, b);
        var dl = b.Build();
        Assert.Contains(dl.Ops.OfType<DL.Border>(), _ => true);
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), tr => tr.Content.Contains("Hello"));
    }
}
