using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Rendering.Tests;

public class ZOrderTests
{
    [Fact]
    public void Later_Ops_Override_Earlier()
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, 3, 1));
        b.DrawRect(new Rect(0, 0, 3, 1, new Rgb24(10, 10, 10)));
        b.DrawText(new TextRun(1, 0, "X", new Rgb24(255, 255, 255), null, CellAttrFlags.None));
        b.Pop();
        var g = new TtyCompositor().Composite(b.Build(), (3, 1));
        Assert.Equal("X", g[1, 0].Grapheme);
    }

    [Fact]
    public void Reverse_Order_Covers_Text()
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, 3, 1));
        b.DrawText(new TextRun(1, 0, "X", new Rgb24(255, 255, 255), null, CellAttrFlags.None));
        b.DrawRect(new Rect(0, 0, 3, 1, new Rgb24(10, 10, 10)));
        b.Pop();
        var g = new TtyCompositor().Composite(b.Build(), (3, 1));
        // Overwrite model: bg space will replace glyph at (1,0)
        Assert.Equal(" ", g[1, 0].Grapheme);
        Assert.Equal(new Rgb24(10, 10, 10), g[1, 0].Bg);
    }
}
