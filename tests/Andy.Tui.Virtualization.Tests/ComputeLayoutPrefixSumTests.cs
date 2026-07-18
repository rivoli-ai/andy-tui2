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
}
