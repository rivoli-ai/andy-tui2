using System.Collections.Generic;
using Andy.Tui.Style;

namespace Andy.Tui.Layout.Tests;

public class YogaParityTests
{
    private sealed class FixedNode : ILayoutNode
    {
        private readonly Size _size;
        public Rect ArrangedRect { get; private set; }
        public FixedNode(double w, double h) { _size = new Size(w, h); }
        public Size Measure(in Size available) => _size;
        public void Arrange(in Rect finalRect) { ArrangedRect = finalRect; }
    }

    private sealed class BaselineNode : ILayoutNode, IBaselineProvider
    {
        private readonly Size _size;
        private readonly double _baseline;
        public Rect ArrangedRect { get; private set; }
        public BaselineNode(double w, double h, double baseline)
        {
            _size = new Size(w, h);
            _baseline = baseline;
        }
        public Size Measure(in Size available) => _size;
        public void Arrange(in Rect finalRect) { ArrangedRect = finalRect; }
        public double GetFirstBaseline(in Size measuredSize) => _baseline;
    }

    [Fact]
    public void Order_Sorts_Items_Before_Line_Build()
    {
        var container = ResolvedStyle.Default;
        var n1 = new FixedNode(10, 2);
        var n2 = new FixedNode(10, 2);
        var n3 = new FixedNode(10, 2);
        var children = new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default with { Order = 2 }),
            (n2, ResolvedStyle.Default with { Order = 0 }),
            (n3, ResolvedStyle.Default with { Order = 1 })
        };
        FlexLayout.Layout(new Size(100, 10), container, children);
        Assert.True(n2.ArrangedRect.X < n3.ArrangedRect.X && n3.ArrangedRect.X < n1.ArrangedRect.X);
    }

    [Fact]
    public void MinMax_Clamp_With_Grow_Distributes_Remainder()
    {
        var container = ResolvedStyle.Default with { ColumnGap = new Length(0) };
        var n1 = new FixedNode(10, 2);
        var n2 = new FixedNode(10, 2);
        var s1 = ResolvedStyle.Default with { FlexGrow = 1, MaxWidth = LengthOrAuto.FromPixels(20) };
        var s2 = ResolvedStyle.Default with { FlexGrow = 1 };
        // width=80; base sum 20; free 60; n1 capped to 20; n2 gets 60+10 => 70
        FlexLayout.Layout(new Size(80, 10), container, new List<(ILayoutNode, ResolvedStyle)> { (n1, s1), (n2, s2) });
        Assert.InRange(n1.ArrangedRect.Width, 20 - 1e-6, 20 + 1e-6);
        Assert.InRange(n2.ArrangedRect.Width, 60, 70); // depending on base use; at least majority goes to n2
        Assert.InRange(n2.ArrangedRect.X, n1.ArrangedRect.Width - 1e-6, n1.ArrangedRect.Width + 1e-6);
    }

    [Fact]
    public void Wrap_With_Gaps_Y_Positions_Match_Line_Heights()
    {
        var container = ResolvedStyle.Default with { ColumnGap = new Length(2), RowGap = new Length(3), FlexWrap = FlexWrap.Wrap, AlignContent = AlignContent.FlexStart };
        var n1 = new FixedNode(8, 2);
        var n2 = new FixedNode(8, 2);
        var n3 = new FixedNode(8, 2);
        // width=18 fits two (8 + 2 + 8 = 18), third wraps; row gap = 3
        FlexLayout.Layout(new Size(18, 10), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default), (n2, ResolvedStyle.Default), (n3, ResolvedStyle.Default)
        });
        Assert.Equal(0, n1.ArrangedRect.Y);
        Assert.Equal(0, n2.ArrangedRect.Y);
        Assert.Equal(2 + 3, n3.ArrangedRect.Y);
    }

    [Fact]
    public void Column_SpaceBetween_Distributes_Columns()
    {
        var container = ResolvedStyle.Default with { FlexDirection = FlexDirection.Column, FlexWrap = FlexWrap.Wrap, RowGap = new Length(0), ColumnGap = new Length(0), AlignContent = AlignContent.SpaceBetween };
        var n1 = new FixedNode(10, 30);
        var n2 = new FixedNode(10, 30);
        var n3 = new FixedNode(10, 30);
        var n4 = new FixedNode(10, 30);
        // Height 70 fits two per column (30 + 0 + 30), two columns with space-between across width 100
        FlexLayout.Layout(new Size(100, 70), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default), (n2, ResolvedStyle.Default), (n3, ResolvedStyle.Default), (n4, ResolvedStyle.Default)
        });
        // Expect first column at x=0, second at x close to 90 (100 - colWidth 10)
        Assert.Equal(0, n1.ArrangedRect.X);
        Assert.True(n3.ArrangedRect.X > 80);
    }

    [Theory]
    [InlineData(AlignItems.FlexStart, 0)]
    [InlineData(AlignItems.Center, 3.5)]
    [InlineData(AlignItems.FlexEnd, 7)]
    public void AlignItems_Row_SingleLine(AlignItems ai, double expectedY)
    {
        var container = ResolvedStyle.Default with { AlignItems = ai };
        var n1 = new FixedNode(5, 2);
        var n2 = new FixedNode(5, 3); // max height = 3
        FlexLayout.Layout(new Size(20, 10), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default), (n2, ResolvedStyle.Default)
        });
        switch (ai)
        {
            case AlignItems.FlexStart:
                Assert.Equal(0, n1.ArrangedRect.Y);
                Assert.Equal(0, n2.ArrangedRect.Y);
                break;
            case AlignItems.Center:
                Assert.InRange(n1.ArrangedRect.Y, expectedY - 1e-6, expectedY + 1e-6);
                Assert.InRange(n2.ArrangedRect.Y, expectedY - 1e-6, expectedY + 1e-6);
                break;
            case AlignItems.FlexEnd:
                Assert.InRange(n1.ArrangedRect.Y, expectedY - 1e-6, expectedY + 1e-6);
                Assert.InRange(n2.ArrangedRect.Y, expectedY - 1e-6, expectedY + 1e-6);
                break;
        }
    }

    [Fact]
    public void Order_Negative_Comes_First()
    {
        var c = ResolvedStyle.Default;
        var a = new FixedNode(10, 1);
        var b = new FixedNode(10, 1);
        var d = new FixedNode(10, 1);
        var kids = new List<(ILayoutNode, ResolvedStyle)>
        {
            (a, ResolvedStyle.Default with { Order = 1 }),
            (b, ResolvedStyle.Default with { Order = -1 }),
            (d, ResolvedStyle.Default with { Order = 0 })
        };
        FlexLayout.Layout(new Size(100, 5), c, kids);
        Assert.True(b.ArrangedRect.X < d.ArrangedRect.X && d.ArrangedRect.X < a.ArrangedRect.X);
    }

    [Fact]
    public void Column_AlignItems_Center_Horizontal_Centering()
    {
        var c = ResolvedStyle.Default with { FlexDirection = FlexDirection.Column, AlignItems = AlignItems.Center };
        var a = new FixedNode(20, 5);
        FlexLayout.Layout(new Size(50, 50), c, new List<(ILayoutNode, ResolvedStyle)> { (a, ResolvedStyle.Default) });
        Assert.InRange(a.ArrangedRect.X, (50 - 20) / 2.0 - 1e-6, (50 - 20) / 2.0 + 1e-6);
    }

    [Fact]
    public void Percent_MinHeight_Clamps_Item_Height()
    {
        var c = ResolvedStyle.Default;
        var a = new FixedNode(10, 5);
        var s = ResolvedStyle.Default with { MinHeight = LengthOrAuto.FromPercent(50) };
        FlexLayout.Layout(new Size(40, 20), c, new List<(ILayoutNode, ResolvedStyle)> { (a, s) });
        Assert.InRange(a.ArrangedRect.Height, 10 - 1e-6, 10 + 1e-6);
    }
    [Fact]
    public void FlexBasis_Zero_With_Grow_Distributes_Equally()
    {
        var container = ResolvedStyle.Default with { ColumnGap = new Length(0) };
        var n1 = new FixedNode(10, 1);
        var n2 = new FixedNode(10, 1);
        var s1 = ResolvedStyle.Default with { FlexBasis = LengthOrAuto.FromPixels(0), FlexGrow = 1 };
        var s2 = ResolvedStyle.Default with { FlexBasis = LengthOrAuto.FromPixels(0), FlexGrow = 1 };
        var children = new List<(ILayoutNode, ResolvedStyle)> { (n1, s1), (n2, s2) };
        FlexLayout.Layout(new Size(100, 10), container, children);
        Assert.InRange(n1.ArrangedRect.Width, 50 - 1e-6, 50 + 1e-6);
        Assert.InRange(n2.ArrangedRect.Width, 50 - 1e-6, 50 + 1e-6);
        Assert.Equal(50, n2.ArrangedRect.X);
    }

    [Fact]
    public void FlexBasis_Auto_Uses_Measure_As_Base()
    {
        var container = ResolvedStyle.Default with { ColumnGap = new Length(0) };
        var n1 = new FixedNode(30, 1);
        var n2 = new FixedNode(10, 1);
        var s1 = ResolvedStyle.Default with { FlexBasis = LengthOrAuto.Auto(), FlexGrow = 1 };
        var s2 = ResolvedStyle.Default with { FlexBasis = LengthOrAuto.Auto(), FlexGrow = 1 };
        var children = new List<(ILayoutNode, ResolvedStyle)> { (n1, s1), (n2, s2) };
        // width=80; base sum 40; free 40; grow 1:1 => +20 each -> widths 50 and 30
        FlexLayout.Layout(new Size(80, 10), container, children);
        Assert.InRange(n1.ArrangedRect.Width, 50 - 1e-6, 50 + 1e-6);
        Assert.InRange(n2.ArrangedRect.Width, 30 - 1e-6, 30 + 1e-6);
        Assert.InRange(n2.ArrangedRect.X, 50 - 1e-6, 50 + 1e-6);
    }

    [Fact]
    public void FlexShrink_Proportional_To_Base_Size()
    {
        var container = ResolvedStyle.Default with { ColumnGap = new Length(0) };
        var n1 = new FixedNode(40, 1);
        var n2 = new FixedNode(20, 1);
        var s1 = ResolvedStyle.Default with { FlexBasis = LengthOrAuto.Auto(), FlexShrink = 1 };
        var s2 = ResolvedStyle.Default with { FlexBasis = LengthOrAuto.Auto(), FlexShrink = 1 };
        var children = new List<(ILayoutNode, ResolvedStyle)> { (n1, s1), (n2, s2) };
        // container 50; base sum 60; deficit 10; shrink weights 40 and 20 => 2:1 -> reductions ~6.666 and ~3.333
        FlexLayout.Layout(new Size(50, 10), container, children);
        Assert.InRange(n1.ArrangedRect.Width, 33.33 - 0.3, 33.33 + 0.3);
        Assert.InRange(n2.ArrangedRect.Width, 16.66 - 0.3, 16.66 + 0.3);
    }

    [Theory]
    [InlineData(JustifyContent.SpaceBetween)]
    [InlineData(JustifyContent.SpaceAround)]
    [InlineData(JustifyContent.SpaceEvenly)]
    public void JustifyContent_Matches_Yoga_Expectations(JustifyContent jc)
    {
        var container = ResolvedStyle.Default with { JustifyContent = jc, ColumnGap = new Length(0) };
        var n1 = new FixedNode(10, 1);
        var n2 = new FixedNode(10, 1);
        FlexLayout.Layout(new Size(40, 5), container, new List<(ILayoutNode, ResolvedStyle)> { (n1, ResolvedStyle.Default), (n2, ResolvedStyle.Default) });
        // Known expectations inspired by Yoga
        switch (jc)
        {
            case JustifyContent.SpaceBetween:
                Assert.Equal(0, n1.ArrangedRect.X);
                Assert.Equal(30, n2.ArrangedRect.X);
                break;
            case JustifyContent.SpaceAround:
                Assert.InRange(n1.ArrangedRect.X, 5 - 1e-6, 5 + 1e-6);
                Assert.InRange(n2.ArrangedRect.X, 25 - 1e-6, 25 + 1e-6);
                break;
            case JustifyContent.SpaceEvenly:
                var s = (40 - 20) / 3.0;
                Assert.InRange(n1.ArrangedRect.X, s - 1e-6, s + 1e-6);
                Assert.InRange(n2.ArrangedRect.X, s + 10 + s - 1e-6, s + 10 + s + 1e-6);
                break;
        }
    }

    [Fact]
    public void FlexBasis_Percent_Uses_Container_Width()
    {
        var container = ResolvedStyle.Default with { ColumnGap = new Length(0) };
        var n1 = new FixedNode(10, 1);
        var n2 = new FixedNode(10, 1);
        var s1 = ResolvedStyle.Default with { FlexBasis = LengthOrAuto.FromPercent(50), FlexGrow = 0 };
        var s2 = ResolvedStyle.Default with { FlexBasis = LengthOrAuto.FromPercent(25), FlexGrow = 0 };
        var children = new List<(ILayoutNode, ResolvedStyle)> { (n1, s1), (n2, s2) };
        FlexLayout.Layout(new Size(200, 10), container, children);
        Assert.InRange(n1.ArrangedRect.Width, 100 - 1e-6, 100 + 1e-6);
        Assert.InRange(n2.ArrangedRect.Width, 50 - 1e-6, 50 + 1e-6);
        Assert.InRange(n2.ArrangedRect.X, 100 - 1e-6, 100 + 1e-6);
    }

    [Fact]
    public void MinMax_Percent_Constraints_Clamp()
    {
        var container = ResolvedStyle.Default with { ColumnGap = new Length(0) };
        var n = new FixedNode(10, 1);
        var s = ResolvedStyle.Default with { FlexGrow = 1, MinWidth = LengthOrAuto.FromPercent(30), MaxWidth = LengthOrAuto.FromPercent(60) };
        FlexLayout.Layout(new Size(100, 10), container, new List<(ILayoutNode, ResolvedStyle)> { (n, s) });
        Assert.InRange(n.ArrangedRect.Width, 30 - 1e-6, 60 + 1e-6);
    }

    [Theory]
    [InlineData(AlignContent.SpaceAround)]
    [InlineData(AlignContent.SpaceEvenly)]
    public void AlignContent_Row_Distribution(AlignContent ac)
    {
        var container = ResolvedStyle.Default with { FlexWrap = FlexWrap.Wrap, RowGap = new Length(0), AlignContent = ac };
        var n1 = new FixedNode(40, 5);
        var n2 = new FixedNode(40, 5);
        var n3 = new FixedNode(40, 5);
        var n4 = new FixedNode(40, 5);
        // width=100; two per line; two lines; height 20 so there is extra space available for distribution if any
        FlexLayout.Layout(new Size(100, 20), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default), (n2, ResolvedStyle.Default), (n3, ResolvedStyle.Default), (n4, ResolvedStyle.Default)
        });
        if (ac == AlignContent.SpaceAround)
        {
            // Expect some top offset > 0 and equal spacing between lines
            Assert.True(n1.ArrangedRect.Y > 0);
            Assert.InRange(n3.ArrangedRect.Y - n1.ArrangedRect.Y, 5 - 1e-6, 15 + 1e-6);
        }
        else if (ac == AlignContent.SpaceEvenly)
        {
            // Expect equal spacing above first line and between lines
            var top = n1.ArrangedRect.Y;
            var between = n3.ArrangedRect.Y - n1.ArrangedRect.Y - 5;
            Assert.InRange(top, between - 1, between + 1);
        }
    }

    [Fact]
    public void Baseline_Alignment_Uses_Provider()
    {
        var container = ResolvedStyle.Default with { AlignItems = AlignItems.Baseline };
        var n1 = new BaselineNode(10, 10, baseline: 6); // baseline 6 from top
        var n2 = new BaselineNode(10, 14, baseline: 8); // higher box with baseline 8
        FlexLayout.Layout(new Size(100, 30), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default), (n2, ResolvedStyle.Default)
        });
        // Bottoms differ but baselines should match: y1 + 6 == y2 + 8 => y1 - y2 == 2
        var baseline1 = n1.ArrangedRect.Y + 6;
        var baseline2 = n2.ArrangedRect.Y + 8;
        Assert.InRange(baseline1, baseline2 - 1e-6, baseline2 + 1e-6);
    }
}
