using Andy.Tui.DisplayList;
using Andy.Tui.Compositor;

namespace Andy.Tui.Rendering.Tests;

public class TextBackgroundPolicyTests
{
    [Fact]
    public void TextRun_Without_Bg_Does_Not_Paint_Background()
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0,0,5,1));
        b.DrawRect(new Rect(0,0,5,1,new Rgb24(1,1,1)));
        b.DrawText(new TextRun(0,0,"A", new Rgb24(10,10,10), null, CellAttrFlags.None));
        b.Pop();
        var g = new TtyCompositor().Composite(b.Build(), (5,1));
        Assert.Equal(new Rgb24(1,1,1), g[0,0].Bg); // from rect
        Assert.Equal(new Rgb24(10,10,10), g[0,0].Fg); // from text
    }

    [Fact]
    public void TextRun_With_Bg_Paints_Background()
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0,0,5,1));
        b.DrawRect(new Rect(0,0,5,1,new Rgb24(1,1,1)));
        b.DrawText(new TextRun(0,0,"A", new Rgb24(10,10,10), new Rgb24(2,2,2), CellAttrFlags.None));
        b.Pop();
        var g = new TtyCompositor().Composite(b.Build(), (5,1));
        Assert.Equal(new Rgb24(2,2,2), g[0,0].Bg); // overridden by text bg
    }
}
