using System;
using System.Collections.Generic;
using System.Linq;

namespace Andy.Tui.Virtualization.Tests;

public class VirtualLayoutTests
{
    private sealed class ListCollection : IVirtualizedCollection<int>
    {
        private readonly int _count;
        public ListCollection(int count) => _count = count;
        public int Count => _count;
        public int this[int index] => index;
        public string GetKey(int index) => index.ToString();
    }

    // Non-virtualized reference: lay out EVERY item top-to-bottom, then keep only the ones that
    // land inside the viewport rows [0, viewportRows). Slots are expressed relative to the
    // scrolled viewport top, exactly like ComputeLayout's output.
    private static List<ItemSlot> ReferenceVisible(int count, int scrollTop, int viewportRows, Func<int, int>? measure)
    {
        var result = new List<ItemSlot>();
        int top = 0;
        for (int i = 0; i < count; i++)
        {
            int h = Math.Max(1, measure?.Invoke(i) ?? 1);
            int relTop = top - scrollTop;
            // Item is visible if any of its rows fall within [0, viewportRows).
            if (relTop < viewportRows && relTop + h > 0)
                result.Add(new ItemSlot(i, relTop, h));
            top += h;
        }
        return result;
    }

    private static List<ItemSlot> Visible(VirtualLayout layout, int viewportRows) =>
        layout.Slots.Where(s => s.Top < viewportRows && s.Top + s.Height > 0).ToList();

    [Fact]
    public void Fixed_Heights_Match_NonVirtualized_Reference_Top_Middle_Bottom()
    {
        var coll = new ListCollection(100);
        var over = new OverscanPolicy(3, 3, Adaptive: false);
        foreach (int scroll in new[] { 0, 40, 95 })
        {
            var layout = ViewportComputer.ComputeLayout(coll, scroll, 10, over, measureByIndex: null);
            var reference = ReferenceVisible(100, scroll, 10, measure: null);
            Assert.Equal(reference, Visible(layout, 10));
        }
    }

    [Fact]
    public void Variable_Heights_Match_NonVirtualized_Reference()
    {
        var coll = new ListCollection(60);
        int Measure(int i) => (i % 3 == 0) ? 3 : (i % 3 == 1 ? 1 : 2);
        var over = new OverscanPolicy(2, 5, Adaptive: false);
        foreach (int scroll in new[] { 0, 7, 30, 110 })
        {
            var layout = ViewportComputer.ComputeLayout(coll, scroll, 12, over, Measure);
            var reference = ReferenceVisible(60, scroll, 12, Measure);
            Assert.Equal(reference, Visible(layout, 12));
        }
    }

    [Fact]
    public void First_Item_Row_Position_Accounts_For_Partial_Scroll()
    {
        // Variable heights: item 0 is 3 rows tall, item 1 is 2, item 2 is 4 ...
        var coll = new ListCollection(20);
        int Measure(int i) => new[] { 3, 2, 4, 1, 5 }[i % 5];
        // Scroll 4 rows in: item0 (rows 0..2) is fully above; item1 (rows 3..4) begins at row 3,
        // one row above the viewport top -> its slot Top must be -1, not 0.
        var over = new OverscanPolicy(0, 0, Adaptive: false);
        var layout = ViewportComputer.ComputeLayout(coll, 4, 6, over, Measure);
        var firstVisible = layout.Slots.First(s => s.Top + s.Height > 0);
        Assert.Equal(1, firstVisible.Index);
        Assert.Equal(-1, firstVisible.Top);
        Assert.Equal(2, firstVisible.Height);
    }

    [Fact]
    public void Before_Overscan_Items_Are_Positioned_Above_The_Viewport()
    {
        var coll = new ListCollection(100);
        var over = new OverscanPolicy(3, 0, Adaptive: false);
        var layout = ViewportComputer.ComputeLayout(coll, 20, 10, over, measureByIndex: null);
        // First rendered item is 3 rows before the scroll top and sits above the viewport (Top < 0).
        Assert.Equal(17, layout.FirstIndex);
        var first = layout.Slots[0];
        Assert.Equal(17, first.Index);
        Assert.Equal(-3, first.Top);
        // The three before-overscan items (17,18,19) sit entirely above the viewport top row.
        Assert.All(layout.Slots.Where(s => s.Index < 20), s => Assert.True(s.Top + s.Height <= 0));
    }

    [Fact]
    public void Overscan_Adds_Work_But_Not_Visible_Content()
    {
        var coll = new ListCollection(100);
        var noOver = ViewportComputer.ComputeLayout(coll, 30, 10, new OverscanPolicy(0, 0, false), null);
        var withOver = ViewportComputer.ComputeLayout(coll, 30, 10, new OverscanPolicy(4, 4, false), null);
        // More items are rendered with overscan...
        Assert.True(withOver.Slots.Count > noOver.Slots.Count);
        // ...but the VISIBLE slice (rows within the viewport) is identical.
        Assert.Equal(Visible(noOver, 10), Visible(withOver, 10));
    }

    [Fact]
    public void Empty_Collection_Produces_No_Slots()
    {
        var coll = new ListCollection(0);
        var layout = ViewportComputer.ComputeLayout(coll, 5, 10, new OverscanPolicy(2, 2, false), null);
        Assert.True(layout.IsEmpty);
        Assert.Empty(layout.Slots);
        Assert.Equal(-1, layout.FirstIndex);
        Assert.Equal(-1, layout.LastIndex);
    }

    [Fact]
    public void Shrinking_Collection_Never_Indexes_Out_Of_Range()
    {
        // Scroll far down, then shrink drastically. Every produced index must be valid.
        for (int count = 200; count >= 0; count -= 7)
        {
            var coll = new ListCollection(count);
            var layout = ViewportComputer.ComputeLayout(coll, 180, 10, new OverscanPolicy(5, 5, true), null, recentDeltaRows: 20);
            foreach (var s in layout.Slots)
                Assert.InRange(s.Index, 0, Math.Max(0, count - 1));
            if (count == 0) Assert.True(layout.IsEmpty);
        }
    }

    [Fact]
    public void Adaptive_Overscan_Expands_In_Scroll_Direction()
    {
        var coll = new ListCollection(200);
        var over = new OverscanPolicy(1, 1, Adaptive: true);
        var down = ViewportComputer.ComputeLayout(coll, 100, 10, over, null, recentDeltaRows: 6);
        var up = ViewportComputer.ComputeLayout(coll, 100, 10, over, null, recentDeltaRows: -6);
        // Scrolling down extends the tail; scrolling up extends the head.
        Assert.True(down.LastIndex > 100 + 10);      // after-overscan grew
        Assert.True(up.FirstIndex < 100 - 1);        // before-overscan grew
    }

    [Fact]
    public void Zero_Height_Measurement_Is_Clamped_And_Does_Not_Stall()
    {
        var coll = new ListCollection(10);
        // A mis-measured 0 must be treated as at least 1 row so advancement never stalls.
        var layout = ViewportComputer.ComputeLayout(coll, 0, 5, new OverscanPolicy(0, 0, false), _ => 0);
        Assert.All(layout.Slots, s => Assert.True(s.Height >= 1));
        Assert.Equal(0, layout.FirstIndex);
    }

    [Fact]
    public void Bottom_Scroll_Does_Not_Exceed_Last_Index()
    {
        var coll = new ListCollection(50);
        var layout = ViewportComputer.ComputeLayout(coll, 48, 10, new OverscanPolicy(4, 4, false), null);
        Assert.Equal(49, layout.LastIndex);
        Assert.All(layout.Slots, s => Assert.InRange(s.Index, 0, 49));
    }
}
