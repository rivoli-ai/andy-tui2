using System.Collections.Generic;
using System.Linq;
using Andy.Tui.Virtualization;
using Andy.Tui.Widgets;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class VirtualizedListRenderTests
{
    private sealed class IntCollection : IVirtualizedCollection<int>
    {
        private readonly int _count;
        public IntCollection(int count) => _count = count;
        public int Count => _count;
        public int this[int index] => index;
        public string GetKey(int index) => index.ToString();
    }

    // Records the slot each item was asked to render into, keyed by item index.
    private sealed class RecordingRenderer : IItemRenderer<int>
    {
        public readonly List<(int Index, int Y, int Height)> Rendered = new();
        public int? MeasureHeight;
        public void Render(in int item, int index, in L.Rect slot, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
        {
            Rendered.Add((index, (int)slot.Y, (int)slot.Height));
            builder.DrawText(new DL.TextRun((int)slot.X, (int)slot.Y, item.ToString(), null, null, DL.CellAttrFlags.None));
        }
    }

    private static DL.DisplayList Render(VirtualizedList<int> list, RecordingRenderer r, L.Rect vp)
    {
        var builder = new DL.DisplayListBuilder();
        var baseDl = new DL.DisplayListBuilder().Build();
        list.Render(vp, baseDl, builder);
        return builder.Build();
    }

    [Fact]
    public void First_Visible_Item_Renders_At_Viewport_Top_Not_Overscan_Item()
    {
        var coll = new IntCollection(100);
        var r = new RecordingRenderer();
        var list = new VirtualizedList<int>(coll, r, new OverscanPolicy(3, 3, Adaptive: false));
        list.SetViewportRows(20, 8);
        var vp = new L.Rect(0, 5, 40, 8);

        Render(list, r, vp);

        // The item at the scroll offset (index 20) must land exactly on the viewport top row (Y=5),
        // NOT the first overscan item (index 17). This is the core bug being fixed.
        var onTop = r.Rendered.Single(x => x.Y == 5);
        Assert.Equal(20, onTop.Index);
        // The before-overscan items 17,18,19 render ABOVE the viewport top (Y < 5).
        foreach (int idx in new[] { 17, 18, 19 })
            Assert.True(r.Rendered.Single(x => x.Index == idx).Y < 5);
    }

    [Fact]
    public void Render_Pushes_A_Viewport_Clip_And_Pops_It()
    {
        var coll = new IntCollection(100);
        var r = new RecordingRenderer();
        var list = new VirtualizedList<int>(coll, r, new OverscanPolicy(3, 3, Adaptive: false));
        list.SetViewportRows(20, 8);
        var vp = new L.Rect(2, 5, 40, 8);

        var dl = Render(list, r, vp);

        var clip = dl.Ops.OfType<DL.ClipPush>().Single();
        Assert.Equal(2, clip.X);
        Assert.Equal(5, clip.Y);
        Assert.Equal(40, clip.Width);
        Assert.Equal(8, clip.Height);
        // The clip is balanced by a Pop so it does not leak into later drawing.
        Assert.Single(dl.Ops.OfType<DL.Pop>());
    }

    [Fact]
    public void Measured_Items_Receive_Their_Full_Height_Slots_And_Advance_By_Height()
    {
        var coll = new IntCollection(50);
        var r = new RecordingRenderer();
        var list = new VirtualizedList<int>(coll, r, new OverscanPolicy(0, 0, Adaptive: false));
        list.SetMeasureByIndex(_ => 2); // every item is two rows tall
        list.SetViewportRows(0, 6);
        var vp = new L.Rect(0, 0, 40, 6);

        Render(list, r, vp);

        // Each rendered slot is 2 rows tall and successive items advance by 2 rows, not 1.
        var ordered = r.Rendered.OrderBy(x => x.Index).ToList();
        Assert.All(ordered, x => Assert.Equal(2, x.Height));
        Assert.Equal(0, ordered[0].Y);
        Assert.Equal(2, ordered[1].Y);
        Assert.Equal(4, ordered[2].Y);
    }

    [Fact]
    public void Empty_Collection_Renders_Nothing_And_Does_Not_Throw()
    {
        var coll = new IntCollection(0);
        var r = new RecordingRenderer();
        var list = new VirtualizedList<int>(coll, r, new OverscanPolicy(2, 2, Adaptive: false));
        list.SetViewportRows(5, 8); // scrolled offset with an empty collection
        var vp = new L.Rect(0, 0, 40, 8);

        var dl = Render(list, r, vp);

        Assert.Empty(r.Rendered);
        Assert.Empty(dl.Ops); // no clip, no draws
    }

    [Fact]
    public void Fixed_Height_Rendering_Matches_NonVirtualized_Reference()
    {
        const int count = 100;
        var coll = new IntCollection(count);
        var r = new RecordingRenderer();
        var list = new VirtualizedList<int>(coll, r, new OverscanPolicy(2, 2, Adaptive: false));
        int scroll = 37, rows = 9;
        list.SetViewportRows(scroll, rows);
        var vp = new L.Rect(0, 0, 40, rows);

        Render(list, r, vp);

        // Reference: lay all items out at 1 row each, keep the ones whose row falls in [0, rows).
        var expected = Enumerable.Range(0, count)
            .Select(i => (Index: i, Y: i - scroll))
            .Where(x => x.Y >= 0 && x.Y < rows)
            .ToList();
        var actualVisible = r.Rendered
            .Where(x => x.Y >= 0 && x.Y < rows)
            .Select(x => (x.Index, x.Y))
            .OrderBy(x => x.Y)
            .ToList();
        Assert.Equal(expected, actualVisible);
    }

    [Fact]
    public void Width_Change_Invalidates_Cached_Measurements()
    {
        var coll = new IntCollection(50);
        var r = new RecordingRenderer();
        var list = new VirtualizedList<int>(coll, r, new OverscanPolicy(0, 0, Adaptive: false));

        // Height source the "measure" reads. It is mutated between renders to prove caching.
        int height = 1;
        list.SetMeasureByIndex(_ => height);
        list.SetViewportRows(0, 8);

        // First render at width 40 caches height=1 for each key.
        Render(list, r, new L.Rect(0, 0, 40, 8));
        Assert.All(r.Rendered, x => Assert.Equal(1, x.Height));
        r.Rendered.Clear();

        // Change the underlying height, then re-render at the SAME width: cache still holds 1.
        height = 3;
        Render(list, r, new L.Rect(0, 0, 40, 8));
        Assert.All(r.Rendered, x => Assert.Equal(1, x.Height));
        r.Rendered.Clear();

        // Re-render at a NEW width: the width change clears the cache, so height=3 is now used.
        Render(list, r, new L.Rect(0, 0, 30, 8));
        Assert.All(r.Rendered, x => Assert.Equal(3, x.Height));
    }

    [Fact]
    public void Bottom_Scroll_Never_Renders_Past_The_Last_Item()
    {
        var coll = new IntCollection(30);
        var r = new RecordingRenderer();
        var list = new VirtualizedList<int>(coll, r, new OverscanPolicy(5, 5, Adaptive: false));
        list.SetViewportRows(28, 10);
        var vp = new L.Rect(0, 0, 40, 10);

        Render(list, r, vp);

        Assert.All(r.Rendered, x => Assert.InRange(x.Index, 0, 29));
        Assert.Equal(29, r.Rendered.Max(x => x.Index));
    }
}
