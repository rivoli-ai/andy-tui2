using Andy.Tui.DisplayList;
using Andy.Tui.Compositor;

namespace Andy.Tui.Rendering.Tests;

public class DisplayListClipTests
{
    [Fact]
    public void Clip_Prevents_Drawing_Outside()
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(2,2,3,2));
        b.DrawRect(new Rect(0,0,2,2,new Rgb24(100,100,100)));
        b.DrawText(new TextRun(0,0,"hi", new Rgb24(255,255,255), null, CellAttrFlags.None));
        b.Pop();
        var dl = b.Build();

        var grid = new TtyCompositor().Composite(dl, (5,4));
        // Top-left should remain default (not overdrawn). Default Grapheme is null.
        Assert.Null(grid[0,0].Grapheme);
        Assert.Equal((byte)0, grid[0,0].Width);
    }

    [Fact]
    public void Clip_Allows_Drawing_Inside_Intersection()
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(1,1,3,2));
        b.DrawRect(new Rect(0,0,4,3,new Rgb24(10,10,10)));
        b.DrawText(new TextRun(2,2,"x", new Rgb24(255,255,255), null, CellAttrFlags.None));
        b.Pop();
        var grid = new TtyCompositor().Composite(b.Build(), (5,4));

        // Inside intersection: (1,1) must be filled (space with bg color)
        Assert.Equal(" ", grid[1,1].Grapheme);
        Assert.Equal(new Rgb24(10,10,10), grid[1,1].Bg);
        // Text at (2,2) should appear
        Assert.Equal("x", grid[2,2].Grapheme);
        // Outside clip: (0,0) remains default
        Assert.Null(grid[0,0].Grapheme);
    }
}
