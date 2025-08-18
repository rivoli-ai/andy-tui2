using Andy.Tui.DisplayList;

namespace Andy.Tui.Rendering.Tests;

public class DisplayListTests
{
    [Fact]
    public void Builder_Emits_Ops_And_Validates_Balance()
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, 10, 10));
        b.DrawRect(new Rect(1, 1, 2, 2, new Rgb24(10, 20, 30)));
        b.DrawText(new TextRun(1, 1, "hi", new Rgb24(255, 255, 255), null, CellAttrFlags.Bold));
        b.Pop();
        var dl = b.Build();

        Assert.Equal(4, dl.Ops.Count);
        DisplayListInvariants.Validate(dl);
    }

    [Fact]
    public void Unbalanced_Clip_Throws()
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, 1, 1));
        var dl = b.Build();
        Assert.Throws<DisplayListInvariantViolationException>(() => DisplayListInvariants.Validate(dl));
    }
}
