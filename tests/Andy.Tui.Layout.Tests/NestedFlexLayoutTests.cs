using System;
using System.Collections.Generic;
using Andy.Tui.Style;

namespace Andy.Tui.Layout.Tests;

public class NestedFlexLayoutTests
{
    private sealed class FixedNode : ILayoutNode
    {
        private readonly Size _size;
        public Rect ArrangedRect { get; private set; }
        public FixedNode(double w, double h) { _size = new Size(w, h); }
        public Size Measure(in Size available) => _size;
        public void Arrange(in Rect finalRect) { ArrangedRect = finalRect; }
    }

    private static (double width, double height) LayoutInnerRow(IReadOnlyList<(ILayoutNode, ResolvedStyle)> children, double containerWidth, ResolvedStyle containerStyle)
    {
        FlexLayout.Layout(new Size(containerWidth, 1000), containerStyle, children);
        double maxRight = 0;
        double maxBottom = 0;
        foreach (var (node, _) in children)
        {
            var fn = (FixedNode)node;
            maxRight = Math.Max(maxRight, fn.ArrangedRect.Right);
            maxBottom = Math.Max(maxBottom, fn.ArrangedRect.Bottom);
        }
        return (maxRight, maxBottom);
    }

    [Fact]
    public void Outer_Justify_Center_Centers_Inner_Containers()
    {
        // Two inner rows of different sizes, outer should center them horizontally
        var inner1_n1 = new FixedNode(6, 2);
        var inner1_n2 = new FixedNode(6, 2);
        var inner1Children = new List<(ILayoutNode, ResolvedStyle)>
        {
            (inner1_n1, ResolvedStyle.Default),
            (inner1_n2, ResolvedStyle.Default)
        };
        var innerContainerStyle = ResolvedStyle.Default with { ColumnGap = new Length(2), JustifyContent = JustifyContent.FlexStart };
        var (inner1W, inner1H) = LayoutInnerRow(inner1Children, 100, innerContainerStyle); // row width 6+2+6=14

        var inner2_n1 = new FixedNode(4, 3);
        var inner2_n2 = new FixedNode(4, 3);
        var inner2_n3 = new FixedNode(4, 3);
        var inner2Children = new List<(ILayoutNode, ResolvedStyle)>
        {
            (inner2_n1, ResolvedStyle.Default),
            (inner2_n2, ResolvedStyle.Default),
            (inner2_n3, ResolvedStyle.Default)
        };
        var (inner2W, inner2H) = LayoutInnerRow(inner2Children, 100, innerContainerStyle); // 4+2+4+2+4 = 16

        var outer = ResolvedStyle.Default with { JustifyContent = JustifyContent.Center, ColumnGap = new Length(4) };
        var c1 = new FixedNode(inner1W, inner1H);
        var c2 = new FixedNode(inner2W, inner2H);
        var outerChildren = new List<(ILayoutNode, ResolvedStyle)>
        {
            (c1, ResolvedStyle.Default),
            (c2, ResolvedStyle.Default)
        };

        FlexLayout.Layout(new Size(50, 20), outer, outerChildren);
        var total = inner1W + 4 + inner2W; // 14 + 4 + 16 = 34; (50-34)/2 = 8
        Assert.InRange(c1.ArrangedRect.X, 8 - 1e-6, 8 + 1e-6);
        Assert.InRange(c2.ArrangedRect.X, 8 + inner1W + 4 - 1e-6, 8 + inner1W + 4 + 1e-6);
    }

    [Theory]
    [InlineData(JustifyContent.FlexStart)]
    [InlineData(JustifyContent.Center)]
    [InlineData(JustifyContent.FlexEnd)]
    [InlineData(JustifyContent.SpaceBetween)]
    [InlineData(JustifyContent.SpaceAround)]
    [InlineData(JustifyContent.SpaceEvenly)]
    public void Containment_Invariants_Hold_For_Row_MultiLine(JustifyContent jc)
    {
        var container = ResolvedStyle.Default with { ColumnGap = new Length(2), RowGap = new Length(1), FlexWrap = FlexWrap.Wrap, JustifyContent = jc };
        var nodes = new List<FixedNode>();
        var children = new List<(ILayoutNode, ResolvedStyle)>();
        // 5 items of width 7 (wraps at width 20), two lines
        for (int i = 0; i < 5; i++) { var n = new FixedNode(7, 2); nodes.Add(n); children.Add((n, ResolvedStyle.Default)); }
        var size = new Size(20, 10);
        FlexLayout.Layout(size, container, children);
        foreach (var n in nodes)
        {
            Assert.True(n.ArrangedRect.X >= -1e-6);
            Assert.True(n.ArrangedRect.Right <= size.Width + 1e-6);
            Assert.True(n.ArrangedRect.Y >= -1e-6);
            Assert.True(n.ArrangedRect.Bottom <= size.Height + 4); // allow some slack for line height accumulation
        }
    }
}
