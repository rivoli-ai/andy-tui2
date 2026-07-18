using System;
using System.Collections.Generic;
using System.Linq;

namespace Andy.Tui.Virtualization.Tests;

public class CumulativeHeightIndexTests
{
    private sealed class ListCollection : IVirtualizedCollection<int>
    {
        private readonly int _count;
        public ListCollection(int count) => _count = count;
        public int Count => _count;
        public int this[int index] => index;
        public string GetKey(int index) => index.ToString();
    }

    // Same non-virtualized reference layout used by VirtualLayoutTests: lay out every item
    // top-to-bottom and keep the ones intersecting [0, viewportRows).
    private static List<ItemSlot> ReferenceVisible(int count, int scrollTop, int viewportRows, Func<int, int>? measure)
    {
        var result = new List<ItemSlot>();
        int top = 0;
        for (int i = 0; i < count; i++)
        {
            int h = Math.Max(1, measure?.Invoke(i) ?? 1);
            int relTop = top - scrollTop;
            if (relTop < viewportRows && relTop + h > 0)
                result.Add(new ItemSlot(i, relTop, h));
            top += h;
        }
        return result;
    }

    private static List<ItemSlot> Visible(VirtualLayout layout, int viewportRows) =>
        layout.Slots.Where(s => s.Top < viewportRows && s.Top + s.Height > 0).ToList();

    [Fact]
    public void FirstIntersecting_Locates_Item_Containing_Row()
    {
        var idx = new CumulativeHeightIndex();
        // heights: 3,1,2,3,1,2,... prefix: 0,3,4,6,9,10,12,...
        int Measure(int i) => (i % 3 == 0) ? 3 : (i % 3 == 1 ? 1 : 2);
        idx.EnsureBuilt(60, Measure);

        Assert.Equal(0, idx.FirstIntersecting(-5));   // above content -> first item
        Assert.Equal(0, idx.FirstIntersecting(0));    // item 0 spans rows [0,3)
        Assert.Equal(0, idx.FirstIntersecting(2));    // still item 0
        Assert.Equal(1, idx.FirstIntersecting(3));    // item 1 spans [3,4)
        Assert.Equal(2, idx.FirstIntersecting(4));    // item 2 spans [4,6)
        Assert.Equal(3, idx.FirstIntersecting(6));    // item 3 spans [6,9)
        Assert.Equal(60, idx.FirstIntersecting(idx.TotalHeight));       // at/after end -> Count
        Assert.Equal(60, idx.FirstIntersecting(idx.TotalHeight + 100)); // well past end -> Count
    }

    [Fact]
    public void EnsureBuilt_Is_Idempotent_And_Only_Measures_On_Rebuild()
    {
        int calls = 0;
        int Measure(int i) { calls++; return 2; }
        var idx = new CumulativeHeightIndex();

        idx.EnsureBuilt(50, Measure);
        Assert.Equal(50, calls);

        // Repeated builds at the same count do not re-measure.
        idx.EnsureBuilt(50, Measure);
        idx.EnsureBuilt(50, Measure);
        Assert.Equal(50, calls);

        // Invalidation forces a rebuild.
        idx.Invalidate();
        idx.EnsureBuilt(50, Measure);
        Assert.Equal(100, calls);

        // A count change also forces a rebuild.
        idx.EnsureBuilt(60, Measure);
        Assert.Equal(160, calls);
    }

    [Fact]
    public void Zero_And_Negative_Heights_Are_Clamped_To_One()
    {
        var idx = new CumulativeHeightIndex();
        idx.EnsureBuilt(4, _ => 0);
        Assert.Equal(4, idx.TotalHeight);          // 4 items x clamp(1)
        for (int i = 0; i < 4; i++)
            Assert.Equal(1, idx.HeightOf(i));
    }

    [Fact]
    public void Empty_Index_Is_Safe()
    {
        var idx = new CumulativeHeightIndex();
        idx.EnsureBuilt(0, _ => 5);
        Assert.Equal(0, idx.TotalHeight);
        Assert.Equal(0, idx.FirstIntersecting(0));
        Assert.Equal(0, idx.FirstIntersecting(10));
    }
}
