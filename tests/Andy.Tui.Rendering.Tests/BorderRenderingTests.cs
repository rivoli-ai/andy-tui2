using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Rendering.Tests;

public class BorderRenderingTests
{
    [Fact]
    public void Draws_Border_Corners_And_Edges()
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, 10, 5));
        b.DrawBorder(new Border(1, 1, 4, 3, "single", new Rgb24(200, 200, 200)));
        b.Pop();
        var g = new TtyCompositor().Composite(b.Build(), (10, 5));

        Assert.Equal("┌", g[1, 1].Grapheme);
        Assert.Equal("┐", g[4, 1].Grapheme);
        Assert.Equal("└", g[1, 3].Grapheme);
        Assert.Equal("┘", g[4, 3].Grapheme);
        Assert.Equal("─", g[2, 1].Grapheme);
        Assert.Equal("│", g[1, 2].Grapheme);
    }

    [Fact]
    public void Border_Respects_Clip()
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(2, 1, 3, 2));
        b.DrawBorder(new Border(1, 1, 4, 3, "single", new Rgb24(200, 200, 200)));
        b.Pop();
        var g = new TtyCompositor().Composite(b.Build(), (10, 5));

        // Outside clip left
        Assert.Null(g[1, 2].Grapheme);
        // Inside clip area, expect some border segment
        Assert.NotNull(g[2, 1].Grapheme);
    }
}
