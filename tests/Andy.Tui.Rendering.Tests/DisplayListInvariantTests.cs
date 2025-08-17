using Andy.Tui.DisplayList;

namespace Andy.Tui.Rendering.Tests;

public class DisplayListInvariantTests
{
    [Fact]
    public void Empty_Intersection_Clip_Throws()
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0,0,2,2));
        b.PushClip(new ClipPush(3,3,1,1)); // no overlap
        b.Pop();
        b.Pop();
        var dl = b.Build();
        Assert.Throws<DisplayListInvariantViolationException>(() => DisplayListInvariants.Validate(dl));
    }

    [Fact]
    public void Pop_Without_Push_Throws()
    {
        var b = new DisplayListBuilder();
        b.Pop();
        var dl = b.Build();
        Assert.Throws<DisplayListInvariantViolationException>(() => DisplayListInvariants.Validate(dl));
    }

    [Fact]
    public void Unbalanced_Leftover_Push_Throws()
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0,0,1,1));
        var dl = b.Build();
        Assert.Throws<DisplayListInvariantViolationException>(() => DisplayListInvariants.Validate(dl));
    }
}
