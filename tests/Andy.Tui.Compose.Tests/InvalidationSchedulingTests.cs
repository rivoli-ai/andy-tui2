using Andy.Tui.Compose;
using Xunit;

namespace Andy.Tui.Compose.Tests;

public class InvalidationSchedulingTests
{
    [Fact]
    public void Burst_Of_State_Changes_Coalesces_Into_One_Frame()
    {
        var scheduler = new ManualFrameScheduler();
        StateRef<int> state = default;

        var root = new VComponent(ctx =>
        {
            state = ctx.UseState(0);
            return new VText($"{state.Value}");
        });

        var composer = new Composer(root, scheduler);
        composer.Recompose(); // initial mount frame
        int framesAfterMount = composer.FrameCount;

        // Several state changes before a flush.
        state.Set(1);
        state.Set(2);
        state.Set(3);

        Assert.Equal(1, scheduler.RequestedFrames);
        Assert.True(scheduler.HasPendingFrame);

        // Only one frame runs, reflecting the latest value.
        scheduler.Flush();
        Assert.Equal(framesAfterMount + 1, composer.FrameCount);
        Assert.False(scheduler.HasPendingFrame);
    }

    [Fact]
    public void Setting_Same_Value_Does_Not_Request_A_Frame()
    {
        var scheduler = new ManualFrameScheduler();
        StateRef<int> state = default;

        var root = new VComponent(ctx =>
        {
            state = ctx.UseState(7);
            return new VText($"{state.Value}");
        });

        var composer = new Composer(root, scheduler);
        composer.Recompose();

        state.Set(7); // identical value

        Assert.Equal(0, scheduler.RequestedFrames);
        Assert.False(scheduler.HasPendingFrame);
    }
}
