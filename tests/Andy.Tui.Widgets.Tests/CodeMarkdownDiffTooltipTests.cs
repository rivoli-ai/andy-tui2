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
    public void Markdown_Renders_Code_Fence_With_Closing()
    {
        var md = new Andy.Tui.Widgets.MarkdownRenderer();
        md.SetText("```\ncode line 1\ncode line 2\n```\nafter code");
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        md.Render(new L.Rect(0,0,40,10), baseDl, b);
        var dl = b.Build();
        var text = string.Join("", dl.Ops.OfType<DL.TextRun>().Select(t => t.Content));

        // Should contain the code content
        Assert.Contains("code line 1", text);
        Assert.Contains("code line 2", text);

        // Should contain text after the closing fence
        Assert.Contains("after code", text);

        // Should NOT contain the fence markers themselves
        Assert.DoesNotContain("```", text);
    }

    [Fact]
    public void Markdown_Handles_Code_Fence_Without_Closing()
    {
        var md = new Andy.Tui.Widgets.MarkdownRenderer();
        md.SetText("```\ncode line 1\ncode line 2");
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        md.Render(new L.Rect(0,0,40,10), baseDl, b);
        var dl = b.Build();
        var text = string.Join("", dl.Ops.OfType<DL.TextRun>().Select(t => t.Content));

        // All lines after opening fence should be treated as code
        Assert.Contains("code line 1", text);
        Assert.Contains("code line 2", text);
    }

    [Fact]
    public void Markdown_Handles_Multiple_Code_Fences()
    {
        var md = new Andy.Tui.Widgets.MarkdownRenderer();
        md.SetText("first\n```\ncode1\n```\nmiddle\n```\ncode2\n```\nlast");
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        md.Render(new L.Rect(0,0,40,15), baseDl, b);
        var dl = b.Build();
        var text = string.Join("", dl.Ops.OfType<DL.TextRun>().Select(t => t.Content));

        // Should contain all content
        Assert.Contains("first", text);
        Assert.Contains("code1", text);
        Assert.Contains("middle", text);
        Assert.Contains("code2", text);
        Assert.Contains("last", text);

        // Should NOT contain fence markers
        Assert.DoesNotContain("```", text);
    }

    [Fact]
    public void Markdown_Code_Fence_Does_Not_Process_Markdown_Inside()
    {
        var md = new Andy.Tui.Widgets.MarkdownRenderer();
        md.SetText("```\n# Not a heading\n**not bold**\n```");
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        md.Render(new L.Rect(0,0,40,10), baseDl, b);
        var dl = b.Build();
        var textRuns = dl.Ops.OfType<DL.TextRun>().ToList();
        var text = string.Join("", textRuns.Select(t => t.Content));

        // Should contain the raw markdown syntax
        Assert.Contains("# Not a heading", text);
        Assert.Contains("**not bold**", text);

        // None of the text should be bold (no markdown processing)
        Assert.All(textRuns, tr => Assert.Equal(DL.CellAttrFlags.None, tr.Attrs));
    }

    [Fact]
    public void Markdown_Code_Fence_With_Language_Specifier()
    {
        var md = new Andy.Tui.Widgets.MarkdownRenderer();
        md.SetText("```csharp\nvar x = 1;\n```");
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        md.Render(new L.Rect(0,0,40,10), baseDl, b);
        var dl = b.Build();
        var text = string.Join("", dl.Ops.OfType<DL.TextRun>().Select(t => t.Content));

        // Should contain the code
        Assert.Contains("var x = 1;", text);

        // Should not contain the fence or language specifier
        Assert.DoesNotContain("```", text);
        Assert.DoesNotContain("csharp", text);
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
