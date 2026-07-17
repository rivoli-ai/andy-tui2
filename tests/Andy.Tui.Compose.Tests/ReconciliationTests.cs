using System.Collections.Generic;
using System.Linq;
using Andy.Tui.Compose;
using Andy.Tui.DisplayList;
using Xunit;

namespace Andy.Tui.Compose.Tests;

public class ReconciliationTests
{
    private static List<string> Rows(Composer composer) =>
        composer.Render().Ops.OfType<TextRun>().OrderBy(t => t.Y).Select(t => t.Content).ToList();

    [Fact]
    public void Update_In_Place_Preserves_Instance_State()
    {
        var scheduler = new ManualFrameScheduler();
        int mounts = 0;
        StateRef<int> state = default;

        var root = new VComponent(ctx =>
        {
            state = ctx.UseState(0);
            // Count mounts via an effect that runs once.
            ctx.UseEffect(() => { mounts++; return null; }, EffectPhase.Layout, new object?[] { });
            return new VText($"v={state.Value}");
        });

        var composer = new Composer(root, scheduler);
        composer.Recompose();
        Assert.Equal(new[] { "v=0" }, Rows(composer));
        Assert.Equal(1, mounts);

        state.Set(5);
        scheduler.Flush();

        // Same instance updated in place: state advanced, no remount.
        Assert.Equal(new[] { "v=5" }, Rows(composer));
        Assert.Equal(1, mounts);
    }

    [Fact]
    public void Changing_Element_Type_Remounts_Subtree()
    {
        var kind = new[] { "box" };
        int childMounts = 0;

        var root = new VComponent(_ =>
        {
            var container = new VElement(kind[0]);
            var child = new VComponent(ctx =>
            {
                ctx.UseEffect(() => { childMounts++; return null; }, EffectPhase.Paint, new object?[] { });
                return new VText("child");
            });
            container.AddChild(child);
            return container;
        });

        var composer = new Composer(root);
        composer.Recompose();
        Assert.Equal(1, childMounts);

        // Change the container element type -> identity changes -> subtree remounts.
        kind[0] = "stack";
        composer.Recompose();
        Assert.Equal(2, childMounts);
    }

    [Fact]
    public void Text_And_Nested_Elements_Flow_Top_To_Bottom()
    {
        var root = new VElement("stack");
        root.AddChild(new VText("one"));
        var inner = new VElement("stack");
        inner.AddChild(new VText("two"));
        inner.AddChild(new VText("three"));
        root.AddChild(inner);
        root.AddChild(new VText("four"));

        var composer = new Composer(root);
        composer.Recompose();

        var runs = composer.Render().Ops.OfType<TextRun>().OrderBy(t => t.Y).ToList();
        Assert.Equal(new[] { "one", "two", "three", "four" }, runs.Select(r => r.Content).ToArray());
        Assert.Equal(new[] { 0, 1, 2, 3 }, runs.Select(r => r.Y).ToArray());
    }

    [Fact]
    public void FromView_Builds_Root_Once()
    {
        var view = new StaticView();
        var composer = Composer.FromView(view);
        composer.Recompose();
        Assert.Equal(new[] { "hello" }, Rows(composer));
    }

    private sealed class StaticView : View
    {
        public override VNode Build()
        {
            var root = new VElement("stack");
            root.AddChild(new VText("hello"));
            return root;
        }
    }
}
