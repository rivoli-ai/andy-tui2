using System.Collections.Generic;
using Andy.Tui.Style;
using Xunit;

namespace Andy.Tui.Layout.Tests;

/// <summary>
/// Issue #38: proves that min/max sizes and align-self resolved by StyleResolver
/// from CSS actually reach FlexLayout. Before the fix the resolver never mapped
/// these declarations, so layout only ever saw the defaults.
/// </summary>
public class ResolvedStyleFlowsIntoLayoutTests
{
    private sealed class FixedNode : ILayoutNode
    {
        private readonly Size _size;
        public Rect ArrangedRect { get; private set; }
        public FixedNode(double w, double h) { _size = new Size(w, h); }
        public Size Measure(in Size available) => _size;
        public void Arrange(in Rect finalRect) { ArrangedRect = finalRect; }
    }

    [Fact]
    public void CssMaxWidth_ConstrainsGrowingItem()
    {
        // A flex-grow item that would otherwise take all the free space is capped
        // by max-width resolved from CSS.
        var sheet = CssParser.Parse(".item { flex-grow: 1; max-width: 30; }");
        var resolver = new StyleResolver();
        var itemStyle = resolver.Compute(new Node("div", classes: new[] { "item" }), new[] { sheet });

        Assert.Equal(30, itemStyle.MaxWidth.Value!.Value.Pixels);

        var node = new FixedNode(10, 5);
        var children = new List<(ILayoutNode, ResolvedStyle)> { (node, itemStyle) };

        FlexLayout.Layout(new Size(100, 20), ResolvedStyle.Default, children);

        Assert.True(node.ArrangedRect.Width <= 30, $"expected width <= 30 but was {node.ArrangedRect.Width}");
    }

    [Fact]
    public void CssAlignSelf_OverridesContainerAlignItems()
    {
        var sheet = CssParser.Parse(".item { align-self: flex-end; }");
        var resolver = new StyleResolver();
        var itemStyle = resolver.Compute(new Node("div", classes: new[] { "item" }), new[] { sheet });
        Assert.Equal(AlignSelf.FlexEnd, itemStyle.AlignSelf);

        // A tall sibling defines the line's cross size, giving the short item room
        // for its align-self:flex-end to be observable against the default flex-start.
        var container = ResolvedStyle.Default with { AlignItems = AlignItems.FlexStart };
        var tall = new FixedNode(10, 10);
        var shortItem = new FixedNode(10, 4);
        var children = new List<(ILayoutNode, ResolvedStyle)>
        {
            (tall, ResolvedStyle.Default),
            (shortItem, itemStyle)
        };

        FlexLayout.Layout(new Size(40, 20), container, children);

        // flex-start keeps the tall item at the line top; align-self:flex-end pushes
        // the short item down within the 10-tall line.
        Assert.Equal(0, tall.ArrangedRect.Y);
        Assert.True(shortItem.ArrangedRect.Y > tall.ArrangedRect.Y,
            $"expected align-self:flex-end to push the short item below the tall one, but Y was {shortItem.ArrangedRect.Y}");
    }
}
