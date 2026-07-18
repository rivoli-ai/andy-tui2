using System;
using System.Collections.Generic;
using System.Linq;

namespace Andy.Tui.Virtualization.Tests;

/// <summary>
/// Covers the prefix-sum (<see cref="CumulativeHeightIndex"/>) fast path of
/// <see cref="ViewportComputer.ComputeLayout{T}(IVirtualizedCollection{T}, int, int, OverscanPolicy, Func{int, int}?, CumulativeHeightIndex?, int)"/>:
/// it must produce a layout identical to the linear walk while keeping per-frame measurement
/// bounded and independent of scroll depth (issue #76).
/// </summary>
public class ComputeLayoutPrefixSumTests
{
    private sealed class ListCollection : IVirtualizedCollection<int>
    {
        private readonly int _count;
        public ListCollection(int count) => _count = count;
        public int Count => _count;
        public int this[int index] => index;
        public string GetKey(int index) => index.ToString();
    }

    private static int Measure(int i) => (i % 4) switch { 0 => 3, 1 => 1, 2 => 2, _ => 4 };

    // A mutable, keyed collection whose stable key encodes content, and whose height is derived
    // from that same content. Reordering or replacing an item therefore changes both its key and
    // its measured height together — exactly the case a same-count/count-guarded index would miss.
    private sealed class KeyedCollection : IVirtualizedCollection<string>
    {
        // Each item is (id, height); the id is the stable key, the height is content-dependent.
        public readonly List<(string Id, int Height)> Items = new();
        public int Count => Items.Count;
        public string this[int index] => Items[index].Id;
        public string GetKey(int index) => Items[index].Id;
        public int MeasureAt(int index) => Items[index].Height;
    }

    [Fact]
    public void Indexed_Path_Matches_Linear_Path_Across_Scroll_Offsets()
    {
        var coll = new ListCollection(500);
        var over = new OverscanPolicy(2, 5, Adaptive: false);
        var index = new CumulativeHeightIndex();

        foreach (int scroll in new[] { 0, 5, 37, 120, 400, 900, 5000 })
        {
            var linear = ViewportComputer.ComputeLayout(coll, scroll, 12, over, Measure, recentDeltaRows: 0);
            var indexed = ViewportComputer.ComputeLayout(coll, scroll, 12, over, Measure, index, recentDeltaRows: 0);

            Assert.Equal(linear.FirstIndex, indexed.FirstIndex);
            Assert.Equal(linear.LastIndex, indexed.LastIndex);
            Assert.Equal(linear.Slots.ToList(), indexed.Slots.ToList());
        }
    }

    [Fact]
    public void Indexed_Path_Matches_Linear_Path_With_Adaptive_Overscan()
    {
        var coll = new ListCollection(300);
        var over = new OverscanPolicy(1, 1, Adaptive: true);
        var index = new CumulativeHeightIndex();

        foreach (int delta in new[] { -8, 0, 8 })
        {
            var linear = ViewportComputer.ComputeLayout(coll, 150, 10, over, Measure, recentDeltaRows: delta);
            var indexed = ViewportComputer.ComputeLayout(coll, 150, 10, over, Measure, index, recentDeltaRows: delta);
            Assert.Equal(linear.Slots.ToList(), indexed.Slots.ToList());
        }
    }

    [Fact]
    public void Deep_Scroll_Does_Not_Re_Measure_After_Index_Is_Built()
    {
        var coll = new ListCollection(10_000);
        var over = new OverscanPolicy(2, 4, Adaptive: false);
        var index = new CumulativeHeightIndex();

        int calls = 0;
        int Counting(int i) { calls++; return Measure(i); }

        // First frame builds the prefix sums once (bounded by the item count, not scroll depth).
        ViewportComputer.ComputeLayout(coll, 0, 20, over, Counting, index);
        int afterBuild = calls;
        Assert.Equal(coll.Count, afterBuild);

        // Subsequent frames — including a very deep scroll — must not measure again: locating the
        // first visible item is O(log n) over the cached prefix sums, independent of scroll depth.
        ViewportComputer.ComputeLayout(coll, 25_000, 20, over, Counting, index);
        ViewportComputer.ComputeLayout(coll, 9_000, 20, over, Counting, index);
        Assert.Equal(afterBuild, calls);
    }

    [Fact]
    public void Measurement_Work_Is_Independent_Of_Scroll_Depth()
    {
        var coll = new ListCollection(20_000);
        var over = new OverscanPolicy(2, 4, Adaptive: false);

        // A shallow scroll and a very deep scroll must measure the same number of items when each
        // starts from a freshly built index — proving the cost is O(window + n-build), not
        // O(scroll-depth) as the pre-fix linear walk was.
        int shallowCalls = 0, deepCalls = 0;
        var shallowIndex = new CumulativeHeightIndex();
        var deepIndex = new CumulativeHeightIndex();

        ViewportComputer.ComputeLayout(coll, 3, 15, over, i => { shallowCalls++; return Measure(i); }, shallowIndex);
        ViewportComputer.ComputeLayout(coll, 40_000, 15, over, i => { deepCalls++; return Measure(i); }, deepIndex);

        Assert.Equal(shallowCalls, deepCalls);
    }

    [Fact]
    public void Invalidation_Reflects_Changed_Heights()
    {
        var coll = new ListCollection(50);
        var over = new OverscanPolicy(0, 0, Adaptive: false);
        var index = new CumulativeHeightIndex();

        int height = 1;
        int Variable(int i) => height;

        var before = ViewportComputer.ComputeLayout(coll, 20, 10, over, Variable, index);
        Assert.Equal(20, before.FirstIndex); // 1-row items: content row 20 == item 20

        // Change every height without invalidating -> stale index keeps the old mapping.
        height = 2;
        var stale = ViewportComputer.ComputeLayout(coll, 20, 10, over, Variable, index);
        Assert.Equal(before.FirstIndex, stale.FirstIndex);

        // After invalidation the new heights map row 20 to item 10 (each item is now 2 rows).
        index.Invalidate();
        var after = ViewportComputer.ComputeLayout(coll, 20, 10, over, Variable, index);
        Assert.Equal(10, after.FirstIndex);
    }

    // (a) A same-count REORDER without an explicit Invalidate. The linear walk re-derives item
    // positions from the current order every frame, so it renders the reordered content correctly.
    // A count-guarded index would keep stale per-position offsets and diverge; the key-sequence
    // rebuild must keep the indexed path byte-for-byte identical to the linear path.
    [Fact]
    public void Indexed_Path_Matches_Linear_Path_After_Reorder_Without_Invalidate()
    {
        var coll = new KeyedCollection();
        coll.Items.Add(("A", 1));
        coll.Items.Add(("B", 5));
        coll.Items.Add(("C", 2));
        coll.Items.Add(("D", 3));

        var over = new OverscanPolicy(1, 2, Adaptive: false);
        var index = new CumulativeHeightIndex();
        Func<int, int> measure = coll.MeasureAt;

        // Frame 1 builds the index for order [A,B,C,D].
        _ = ViewportComputer.ComputeLayout(coll, 0, 8, over, measure, index);

        // Reorder the underlying collection in place (same count) WITHOUT calling Invalidate.
        var b = coll.Items[1];
        coll.Items[1] = coll.Items[0];
        coll.Items[0] = b; // now [B,A,C,D]

        foreach (int scroll in new[] { 0, 1, 3, 6, 9 })
        {
            var linear = ViewportComputer.ComputeLayout(coll, scroll, 8, over, measure, recentDeltaRows: 0);
            var indexed = ViewportComputer.ComputeLayout(coll, scroll, 8, over, measure, index, recentDeltaRows: 0);

            Assert.Equal(linear.FirstIndex, indexed.FirstIndex);
            Assert.Equal(linear.LastIndex, indexed.LastIndex);
            Assert.Equal(linear.Slots.ToList(), indexed.Slots.ToList());
        }
    }

    // (b) An item's height changes at a fixed index without an explicit Invalidate, modelled as a
    // content change that also changes the item's stable key (a keyed, content-dependent measure).
    // The linear walk reflects the new height immediately; the indexed path must too, by rebuilding
    // when the key sequence changes rather than trusting the unchanged count.
    [Fact]
    public void Indexed_Path_Matches_Linear_Path_After_Height_Change_At_Fixed_Index_Without_Invalidate()
    {
        var coll = new KeyedCollection();
        coll.Items.Add(("A@1", 1));
        coll.Items.Add(("B@2", 2));
        coll.Items.Add(("C@1", 1));
        coll.Items.Add(("D@4", 4));
        coll.Items.Add(("E@2", 2));

        var over = new OverscanPolicy(0, 0, Adaptive: false);
        var index = new CumulativeHeightIndex();
        Func<int, int> measure = coll.MeasureAt;

        // Frame 1 builds the index.
        _ = ViewportComputer.ComputeLayout(coll, 0, 6, over, measure, index);

        // The item at fixed index 2 grows from height 1 to height 6; its content-dependent key
        // changes with it. No Invalidate call is made.
        coll.Items[2] = ("C@6", 6);

        foreach (int scroll in new[] { 0, 1, 2, 4, 8 })
        {
            var linear = ViewportComputer.ComputeLayout(coll, scroll, 6, over, measure, recentDeltaRows: 0);
            var indexed = ViewportComputer.ComputeLayout(coll, scroll, 6, over, measure, index, recentDeltaRows: 0);

            Assert.Equal(linear.FirstIndex, indexed.FirstIndex);
            Assert.Equal(linear.LastIndex, indexed.LastIndex);
            Assert.Equal(linear.Slots.ToList(), indexed.Slots.ToList());
        }
    }
}
