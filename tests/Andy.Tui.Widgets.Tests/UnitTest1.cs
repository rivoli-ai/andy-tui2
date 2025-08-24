using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;
using Andy.Tui.Virtualization;

namespace Andy.Tui.Widgets.Tests;

file sealed class StringCollection : IVirtualizedCollection<string>
{
    private readonly List<string> _items;
    public StringCollection(IEnumerable<string> items) { _items = items.ToList(); }
    public int Count => _items.Count;
    public string this[int index] => _items[index];
    public string GetKey(int index) => index.ToString();
}

file sealed class StringRenderer : IItemRenderer<string>
{
    public void Render(in string item, int index, in L.Rect slot, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        builder.PushClip(new DL.ClipPush((int)slot.X, (int)slot.Y, (int)slot.Width, (int)slot.Height));
        builder.DrawText(new DL.TextRun((int)slot.X, (int)slot.Y, item, new DL.Rgb24(200, 200, 200), null, DL.CellAttrFlags.None));
        builder.Pop();
    }
}

public class VirtualizedListTests
{
    [Fact]
    public void Renders_Within_Viewport_With_Overscan()
    {
        var items = new StringCollection(Enumerable.Range(0, 100).Select(i => $"Item {i}"));
        var renderer = new StringRenderer();
        var list = new VirtualizedList<string>(items, renderer);
        list.SetViewportRows(10, 5); // rows 10..14

        var baseDl = new DL.DisplayListBuilder();
        var builder = new DL.DisplayListBuilder();
        list.Render(new L.Rect(0, 0, 20, 10), baseDl.Build(), builder);
        var dl = builder.Build();
        var texts = dl.Ops.OfType<DL.TextRun>().Select(tr => tr.Content).ToList();
        Assert.Contains("Item 10", texts);
        Assert.Contains("Item 14", texts);
    }

    [Fact]
    public void Adaptive_Overscan_Applied_In_List_Render()
    {
        var items = new StringCollection(Enumerable.Range(0, 100).Select(i => $"Item {i}"));
        var renderer = new StringRenderer();
        var list = new VirtualizedList<string>(items, renderer, new OverscanPolicy(1, 1, Adaptive: true));
        list.SetViewportRows(20, 5);
        list.UpdateScrollDelta(6); // scrolling down

        var baseDl = new DL.DisplayListBuilder();
        var builder = new DL.DisplayListBuilder();
        list.Render(new L.Rect(0, 0, 20, 10), baseDl.Build(), builder);
        var dl = builder.Build();
        var texts = dl.Ops.OfType<DL.TextRun>().Select(tr => tr.Content).ToList();
        Assert.Contains("Item 26", texts); // 20 + 5 + (after 1 + delta 6) - 1 => 26 visible
    }

    [Fact]
    public void Variable_Heights_Affect_Render_Window()
    {
        var items = new StringCollection(Enumerable.Range(0, 50).Select(i => $"Row {i}"));
        var renderer = new StringRenderer();
        var list = new VirtualizedList<string>(items, renderer, new OverscanPolicy(2, 2, Adaptive: false));
        list.SetViewportRows(5, 3);
        // heights: even rows = 2, odd = 1
        list.SetMeasureByIndex(i => (i % 2 == 0) ? 2 : 1);

        var baseDl = new DL.DisplayListBuilder();
        var builder = new DL.DisplayListBuilder();
        list.Render(new L.Rect(0, 0, 20, 10), baseDl.Build(), builder);
        var dl = builder.Build();
        var texts = dl.Ops.OfType<DL.TextRun>().Select(tr => tr.Content).ToList();
        Assert.Contains("Row 5", texts);
        Assert.Contains("Row 6", texts);
        Assert.Contains("Row 7", texts);
        // With overscan + variable heights, Row 8 should likely be included
        Assert.Contains("Row 8", texts);
    }
}

public class LabelTests
{
    [Fact]
    public void Label_Renders_Text()
    {
        var label = new Andy.Tui.Widgets.Label("Hello");
        var baseDl = new DL.DisplayListBuilder();
        var builder = new DL.DisplayListBuilder();
        label.Render(new L.Rect(0, 0, 20, 1), baseDl.Build(), builder);
        var dl = builder.Build();
        var texts = dl.Ops.OfType<DL.TextRun>().Select(tr => tr.Content).ToList();
        Assert.Contains("Hello", texts);
    }
}

public class ButtonTests
{
    [Fact]
    public void Focused_Button_Uses_Bold_Attr()
    {
        var btn = new Andy.Tui.Widgets.Button("Click");
        btn.SetFocused(true);
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        btn.Render(new L.Rect(0, 0, 10, 1), baseDl, b);
        var dl = b.Build();
        var text = dl.Ops.OfType<DL.TextRun>().FirstOrDefault();
        Assert.True((text!.Attrs & DL.CellAttrFlags.Bold) == DL.CellAttrFlags.Bold);
    }

    [Fact]
    public void Active_Button_Uses_Active_Background()
    {
        var btn = new Andy.Tui.Widgets.Button("Click");
        btn.SetActive(true);
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        btn.Render(new L.Rect(0, 0, 10, 1), baseDl, b);
        var dl = b.Build();
        var hasActiveBg = dl.Ops.OfType<DL.Rect>().Any(r => r.Fill.R == 80 && r.Fill.G == 80 && r.Fill.B == 120);
        Assert.True(hasActiveBg);
    }
}
