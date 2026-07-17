using System.Collections.Generic;
using System.Linq;
using Andy.Tui.Compose;
using Andy.Tui.DisplayList;
using Xunit;

namespace Andy.Tui.Compose.Tests;

public class KeyedListStateTests
{
    // Builds a parent component that renders one keyed item component per entry
    // of an external, mutable order list. Each item holds its own state and
    // exposes a captured setter so a test can mutate per-item state and then
    // reorder/insert/remove entries.
    private static (Composer composer, ManualFrameScheduler scheduler, Dictionary<string, StateRef<int>> setters)
        BuildList(List<string> order)
    {
        var setters = new Dictionary<string, StateRef<int>>();

        var root = new VComponent(_ =>
        {
            var stack = new VElement("stack");
            foreach (var key in order)
            {
                var capturedKey = key;
                var item = new VComponent(ctx =>
                {
                    var value = ctx.UseState(0);
                    setters[capturedKey] = value;
                    return new VText($"{capturedKey}={value.Value}");
                });
                item.WithKey(capturedKey);
                stack.AddChild(item);
            }
            return stack;
        });

        var scheduler = new ManualFrameScheduler();
        var composer = new Composer(root, scheduler);
        composer.Recompose();
        return (composer, scheduler, setters);
    }

    private static List<string> Rows(Composer composer)
    {
        return composer.Render().Ops
            .OfType<TextRun>()
            .OrderBy(t => t.Y)
            .Select(t => t.Content)
            .ToList();
    }

    [Fact]
    public void Keyed_Reorder_Preserves_State()
    {
        var order = new List<string> { "A", "B", "C" };
        var (composer, scheduler, setters) = BuildList(order);

        // Give each item a distinct state value.
        setters["A"].Set(10);
        setters["B"].Set(20);
        setters["C"].Set(30);
        scheduler.Flush();

        Assert.Equal(new[] { "A=10", "B=20", "C=30" }, Rows(composer));

        // Reverse the order and recompose.
        order.Clear();
        order.AddRange(new[] { "C", "B", "A" });
        composer.Recompose();

        // State followed identity (the key), not position.
        Assert.Equal(new[] { "C=30", "B=20", "A=10" }, Rows(composer));
    }

    [Fact]
    public void Keyed_Insertion_Keeps_Existing_State()
    {
        var order = new List<string> { "A", "B" };
        var (composer, scheduler, setters) = BuildList(order);

        setters["A"].Set(1);
        setters["B"].Set(2);
        scheduler.Flush();

        // Insert a new item between A and B.
        order.Clear();
        order.AddRange(new[] { "A", "X", "B" });
        composer.Recompose();

        Assert.Equal(new[] { "A=1", "X=0", "B=2" }, Rows(composer));
    }

    [Fact]
    public void Keyed_Removal_Drops_Only_That_Item()
    {
        var order = new List<string> { "A", "B", "C" };
        var (composer, scheduler, setters) = BuildList(order);

        setters["A"].Set(1);
        setters["B"].Set(2);
        setters["C"].Set(3);
        scheduler.Flush();

        // Remove the middle item.
        order.Clear();
        order.AddRange(new[] { "A", "C" });
        composer.Recompose();

        Assert.Equal(new[] { "A=1", "C=3" }, Rows(composer));
    }

    [Fact]
    public void Replacement_At_Same_Position_Resets_State()
    {
        var order = new List<string> { "A" };
        var (composer, scheduler, setters) = BuildList(order);

        setters["A"].Set(99);
        scheduler.Flush();
        Assert.Equal(new[] { "A=99" }, Rows(composer));

        // Replace the key at position 0 with a different identity.
        order.Clear();
        order.Add("B");
        composer.Recompose();

        // Fresh identity => state reset to its initial value.
        Assert.Equal(new[] { "B=0" }, Rows(composer));
    }
}
