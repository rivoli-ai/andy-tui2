using System.Collections.Generic;
using Andy.Tui.Style;

namespace Andy.Tui.Layout.Tests;

public class FlexLayoutTests
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
    public void Respects_Order_In_Row_Direction()
    {
        var container = ResolvedStyle.Default; // use defaults; row direction
        var n1 = new FixedNode(10, 5);
        var n2 = new FixedNode(10, 5);
        var n3 = new FixedNode(10, 5);
        var children = new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default with { Order = 2 }),
            (n2, ResolvedStyle.Default with { Order = 0 }),
            (n3, ResolvedStyle.Default with { Order = 1 })
        };

        FlexLayout.Layout(new Size(100, 20), container, children);

        // After layout we expect arrange calls in order 0,1,2 at x positions 0,10,20.
        Assert.True(n2.ArrangedRect.X <= n3.ArrangedRect.X && n3.ArrangedRect.X <= n1.ArrangedRect.X);
    }

    [Fact]
    public void Applies_Column_Gap_And_JustifyContent_Center()
    {
        var container = ResolvedStyle.Default with { ColumnGap = new Length(2), JustifyContent = JustifyContent.Center };
        var n1 = new FixedNode(10, 5);
        var n2 = new FixedNode(10, 5);
        var children = new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default),
            (n2, ResolvedStyle.Default)
        };

        // Container width 30; content width = 10 + 2 + 10 = 22; left offset should be (30-22)/2 = 4
        FlexLayout.Layout(new Size(30, 20), container, children);
        Assert.Equal(4, n1.ArrangedRect.X);
        Assert.Equal(4 + 10 + 2, n2.ArrangedRect.X);
    }

    [Fact]
    public void Wraps_To_Next_Line_When_Width_Exceeded()
    {
        var container = ResolvedStyle.Default with { ColumnGap = new Length(2), RowGap = new Length(3), FlexWrap = FlexWrap.Wrap, AlignContent = AlignContent.FlexStart };
        var n1 = new FixedNode(8, 2);
        var n2 = new FixedNode(8, 2);
        var n3 = new FixedNode(8, 2);
        var children = new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default),
            (n2, ResolvedStyle.Default),
            (n3, ResolvedStyle.Default)
        };

        // width=18 fits two (8 + 2 + 8 = 18), third wraps; row gap = 3
        FlexLayout.Layout(new Size(18, 10), container, children);
        Assert.Equal(0, n1.ArrangedRect.Y);
        Assert.Equal(0, n2.ArrangedRect.Y);
        Assert.Equal(2 + 3, n3.ArrangedRect.Y); // next line starts after first line height (2) + row gap (3)
    }

    [Theory]
    [InlineData(AlignContent.FlexStart, 0, 5 + 3)]
    [InlineData(AlignContent.Center, 1, 1 + 5 + 3)]
    [InlineData(AlignContent.FlexEnd, 2, 2 + 5 + 3)]
    public void AlignContent_MultiLine_Distributes_Space(AlignContent ac, double expectedFirstY, double expectedSecondY)
    {
        // Two lines: each line height=5, row-gap=3, container height=13..15 depending on alignment expectation
        var container = ResolvedStyle.Default with { ColumnGap = new Length(2), RowGap = new Length(3), FlexWrap = FlexWrap.Wrap, AlignContent = ac };
        var n1 = new FixedNode(8, 5);
        var n2 = new FixedNode(8, 5);
        var n3 = new FixedNode(8, 5);
        var n4 = new FixedNode(8, 5);
        var children = new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default), (n2, ResolvedStyle.Default),
            (n3, ResolvedStyle.Default), (n4, ResolvedStyle.Default)
        };

        // width=18 => 8+2+8 fits two per line; two lines. Height chosen so there is extra space for center/end cases.
        var containerHeight = ac switch
        {
            AlignContent.FlexStart => 13, // 5 + 3 + 5
            AlignContent.Center => 15,    // add 2 extra => split equally: 1 top/bottom
            AlignContent.FlexEnd => 15,   // add 2 extra => all on top per flex-end (y start = 2)
            _ => 13
        };
        FlexLayout.Layout(new Size(18, containerHeight), container, children);

        // Top-left of first item on first line and first item on second line
        Assert.Equal(expectedFirstY, n1.ArrangedRect.Y);
        Assert.Equal(expectedSecondY, n3.ArrangedRect.Y);
    }

    [Fact]
    public void ColumnDirection_JustifyContent_Center_Vertically_Centers()
    {
        var container = ResolvedStyle.Default with { FlexDirection = FlexDirection.Column, JustifyContent = JustifyContent.Center, RowGap = new Length(2), AlignItems = AlignItems.FlexStart };
        var n1 = new FixedNode(4, 3);
        var n2 = new FixedNode(4, 3);
        // total base height = 3 + 2 + 3 = 8; container 14 -> top offset (14-8)/2 = 3
        FlexLayout.Layout(new Size(10, 14), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default), (n2, ResolvedStyle.Default)
        });
        Assert.Equal(3, n1.ArrangedRect.Y);
        Assert.Equal(3 + 3 + 2, n2.ArrangedRect.Y);
    }

    [Fact]
    public void ColumnDirection_AlignItems_Center_Horizontally_Centers()
    {
        var container = ResolvedStyle.Default with { FlexDirection = FlexDirection.Column, AlignItems = AlignItems.Center };
        var n1 = new FixedNode(4, 3);
        var n2 = new FixedNode(6, 3);
        // container width 20: max item width = 6; centered => (20-6)/2 = 7
        FlexLayout.Layout(new Size(20, 20), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default), (n2, ResolvedStyle.Default)
        });
        Assert.InRange(n1.ArrangedRect.X, 7 - 1e-6, 7 + 1e-6);
        Assert.InRange(n2.ArrangedRect.X, 7 - 1e-6, 7 + 1e-6);
    }

    [Fact]
    public void ColumnDirection_Wraps_Into_Columns_With_AlignContent()
    {
        var container = ResolvedStyle.Default with { FlexDirection = FlexDirection.Column, FlexWrap = FlexWrap.Wrap, RowGap = new Length(1), ColumnGap = new Length(2), AlignContent = AlignContent.Center };
        var n1 = new FixedNode(4, 3);
        var n2 = new FixedNode(4, 3);
        var n3 = new FixedNode(4, 3);
        var n4 = new FixedNode(4, 3);
        // Height 7 fits two (3 + 1 + 3), third wraps to next column
        FlexLayout.Layout(new Size(20, 7), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default), (n2, ResolvedStyle.Default), (n3, ResolvedStyle.Default), (n4, ResolvedStyle.Default)
        });
        // Two columns centered with column gap 2
        Assert.Equal(0, n1.ArrangedRect.Y);
        Assert.Equal(0, n3.ArrangedRect.Y);
        Assert.True(n3.ArrangedRect.X > n1.ArrangedRect.X);
    }

    [Fact]
    public void AlignSelf_Overrides_AlignItems()
    {
        var container = ResolvedStyle.Default with { AlignItems = AlignItems.FlexEnd };
        var n1 = new FixedNode(5, 2);
        var n2 = new FixedNode(5, 4);
        FlexLayout.Layout(new Size(20, 10), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default with { AlignSelf = AlignSelf.Center }),
            (n2, ResolvedStyle.Default) // inherits container AlignItems:FlexEnd
        });
        var lineHeight = Math.Max(n1.ArrangedRect.Height, n2.ArrangedRect.Height);
        var expectedEndY = 10 - lineHeight;
        var expectedCenterY = expectedEndY + (lineHeight - n1.ArrangedRect.Height) / 2.0;
        Assert.InRange(n1.ArrangedRect.Y, expectedCenterY - 1e-6, expectedCenterY + 1e-6);
        Assert.InRange(n2.ArrangedRect.Y, expectedEndY - 1e-6, expectedEndY + 1e-6);
    }

    [Fact]
    public void FlexGrow_Distributes_Free_Space_Proportionally()
    {
        var container = ResolvedStyle.Default with { ColumnGap = new Length(0) };
        var n1 = new FixedNode(10, 2);
        var n2 = new FixedNode(10, 2);
        // width=40; base sum 20; free 20; grow 1:3 => adds 5 and 15 respectively
        FlexLayout.Layout(new Size(40, 10), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default with { FlexGrow = 1 }),
            (n2, ResolvedStyle.Default with { FlexGrow = 3 })
        });
        Assert.InRange(n1.ArrangedRect.Width, 15 - 1e-6, 15 + 1e-6);
        Assert.InRange(n2.ArrangedRect.Width, 25 - 1e-6, 25 + 1e-6);
        Assert.Equal(15, n2.ArrangedRect.X);
    }

    [Fact]
    public void FlexShrink_Reduces_Space_Proportionally_To_Weight()
    {
        var container = ResolvedStyle.Default with { ColumnGap = new Length(0) };
        var n1 = new FixedNode(30, 2);
        var n2 = new FixedNode(30, 2);
        // width=40; base sum 60; deficit 20; shrink weights 1*30 and 3*30 => 30 and 90 => 1:3
        // reductions 5 and 15 => widths 25 and 15
        FlexLayout.Layout(new Size(40, 10), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default with { FlexShrink = 1 }),
            (n2, ResolvedStyle.Default with { FlexShrink = 3 })
        });
        Assert.InRange(n1.ArrangedRect.Width, 25 - 1e-6, 25 + 1e-6);
        Assert.InRange(n2.ArrangedRect.Width, 15 - 1e-6, 15 + 1e-6);
        Assert.Equal(25, n2.ArrangedRect.X);
    }

    [Fact]
    public void FlexBasis_Overrides_Measured_Width_As_Base()
    {
        var container = ResolvedStyle.Default with { ColumnGap = new Length(0) };
        var n1 = new FixedNode(10, 2); // measure 10
        var n2 = new FixedNode(10, 2);
        // width=40; base = 5 + 10 = 15; free 25; grow 1:1 => +12.5 each -> widths 17.5 and 22.5 (but first starts at 5 base)
        FlexLayout.Layout(new Size(40, 10), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default with { FlexBasis = LengthOrAuto.FromPixels(5), FlexGrow = 1 }),
            (n2, ResolvedStyle.Default with { FlexGrow = 1 })
        });
        Assert.InRange(n1.ArrangedRect.Width, 17.5 - 1e-6, 17.5 + 1e-6);
        Assert.InRange(n2.ArrangedRect.Width, 22.5 - 1e-6, 22.5 + 1e-6);
        Assert.InRange(n2.ArrangedRect.X, 17.5 - 1e-6, 17.5 + 1e-6);
    }

    [Fact]
    public void MinMax_Constraints_Clamp_Size()
    {
        var container = ResolvedStyle.Default with { ColumnGap = new Length(0) };
        var n1 = new FixedNode(10, 2);
        var n2 = new FixedNode(10, 2);
        // Force grow but clamp n1 maxWidth to 12 and minHeight to 3
        FlexLayout.Layout(new Size(50, 10), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default with { FlexGrow = 1, MaxWidth = LengthOrAuto.FromPixels(12), MinHeight = LengthOrAuto.FromPixels(3) }),
            (n2, ResolvedStyle.Default with { FlexGrow = 1 })
        });
        Assert.InRange(n1.ArrangedRect.Width, 12 - 1e-6, 12 + 1e-6);
        Assert.InRange(n1.ArrangedRect.Height, 3 - 1e-6, 3 + 1e-6);
        Assert.True(n2.ArrangedRect.Width > n1.ArrangedRect.Width);
    }

    [Fact]
    public void Overflow_Hidden_Clips_Items_Exceeding_Container()
    {
        var container = ResolvedStyle.Default with { Overflow = Overflow.Hidden };
        var n1 = new FixedNode(100, 5);
        FlexLayout.Layout(new Size(10, 3), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default)
        });
        Assert.InRange(n1.ArrangedRect.Width, 10 - 1e-6, 10 + 1e-6);
        Assert.InRange(n1.ArrangedRect.Height, 3 - 1e-6, 3 + 1e-6);
    }

    [Fact]
    public void Golden_MultiLine_Snapshot_Positions()
    {
        var container = ResolvedStyle.Default with { ColumnGap = new Length(1), RowGap = new Length(2), FlexWrap = FlexWrap.Wrap, JustifyContent = JustifyContent.Center, AlignContent = AlignContent.SpaceBetween };
        var n1 = new FixedNode(3, 2);
        var n2 = new FixedNode(3, 3);
        var n3 = new FixedNode(3, 1);
        var n4 = new FixedNode(3, 2);
        var n5 = new FixedNode(3, 2);
        // width=10 => can fit 3+1+3 = 7 on first line, then +1 gap + 3 = 11 -> wrap after two items per line likely
        FlexLayout.Layout(new Size(10, 12), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default), (n2, ResolvedStyle.Default), (n3, ResolvedStyle.Default), (n4, ResolvedStyle.Default), (n5, ResolvedStyle.Default)
        });

        // Simple golden assertions (stable, not pixel-perfect to browsers, but parity-like)
        Assert.True(n1.ArrangedRect.X <= n2.ArrangedRect.X);
        Assert.True(n3.ArrangedRect.Y >= n1.ArrangedRect.Y);
        Assert.True(n5.ArrangedRect.Y >= n4.ArrangedRect.Y);
    }

    [Theory]
    [InlineData(JustifyContent.FlexStart)]
    [InlineData(JustifyContent.Center)]
    [InlineData(JustifyContent.FlexEnd)]
    [InlineData(JustifyContent.SpaceBetween)]
    [InlineData(JustifyContent.SpaceAround)]
    [InlineData(JustifyContent.SpaceEvenly)]
    public void Supports_JustifyContent_Variants(JustifyContent jc)
    {
        var container = ResolvedStyle.Default with { JustifyContent = jc, ColumnGap = new Length(0) };
        var n1 = new FixedNode(5, 1);
        var n2 = new FixedNode(5, 1);
        FlexLayout.Layout(new Size(20, 5), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default),
            (n2, ResolvedStyle.Default)
        });
        // Smoke asserts: items remain within bounds [0, width] and order preserved
        Assert.True(n1.ArrangedRect.X <= n2.ArrangedRect.X);
        Assert.True(n2.ArrangedRect.X + n2.ArrangedRect.Width <= 20);
        Assert.True(n1.ArrangedRect.X >= 0);
    }

    [Fact]
    public void JustifyContent_SpaceBetween_Computes_Exact_Positions()
    {
        var container = ResolvedStyle.Default with { JustifyContent = JustifyContent.SpaceBetween };
        var n1 = new FixedNode(5, 1);
        var n2 = new FixedNode(5, 1);
        FlexLayout.Layout(new Size(20, 5), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default),
            (n2, ResolvedStyle.Default)
        });
        Assert.Equal(0, n1.ArrangedRect.X);
        Assert.Equal(15, n2.ArrangedRect.X);
    }

    [Fact]
    public void JustifyContent_SpaceAround_Computes_Exact_Positions()
    {
        var container = ResolvedStyle.Default with { JustifyContent = JustifyContent.SpaceAround };
        var n1 = new FixedNode(5, 1);
        var n2 = new FixedNode(5, 1);
        FlexLayout.Layout(new Size(20, 5), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default),
            (n2, ResolvedStyle.Default)
        });
        Assert.InRange(n1.ArrangedRect.X, 2.5 - 1e-6, 2.5 + 1e-6);
        Assert.InRange(n2.ArrangedRect.X, 12.5 - 1e-6, 12.5 + 1e-6);
    }

    [Fact]
    public void JustifyContent_SpaceEvenly_Computes_Exact_Positions()
    {
        var container = ResolvedStyle.Default with { JustifyContent = JustifyContent.SpaceEvenly };
        var n1 = new FixedNode(5, 1);
        var n2 = new FixedNode(5, 1);
        FlexLayout.Layout(new Size(20, 5), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default),
            (n2, ResolvedStyle.Default)
        });
        var s = (20 - 10) / 3.0; // 3.333...
        Assert.InRange(n1.ArrangedRect.X, s - 1e-6, s + 1e-6);
        Assert.InRange(n2.ArrangedRect.X, s + 5 + s - 1e-6, s + 5 + s + 1e-6);
    }

    [Fact]
    public void AlignItems_Center_Positions_Y_Centered_SingleLine()
    {
        var container = ResolvedStyle.Default with { AlignItems = AlignItems.Center };
        var n1 = new FixedNode(5, 2);
        var n2 = new FixedNode(5, 3); // max height = 3
        FlexLayout.Layout(new Size(20, 10), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default),
            (n2, ResolvedStyle.Default)
        });
        var expectedY = (10 - 3) / 2.0; // 3.5
        Assert.InRange(n1.ArrangedRect.Y, expectedY - 1e-6, expectedY + 1e-6);
        Assert.InRange(n2.ArrangedRect.Y, expectedY - 1e-6, expectedY + 1e-6);
    }

    [Fact]
    public void AlignItems_FlexEnd_Positions_Y_At_Bottom_SingleLine()
    {
        var container = ResolvedStyle.Default with { AlignItems = AlignItems.FlexEnd };
        var n1 = new FixedNode(5, 2);
        var n2 = new FixedNode(5, 3); // max height = 3
        FlexLayout.Layout(new Size(20, 10), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default),
            (n2, ResolvedStyle.Default)
        });
        var expectedY = 10 - 3; // 7
        Assert.InRange(n1.ArrangedRect.Y, expectedY - 1e-6, expectedY + 1e-6);
        Assert.InRange(n2.ArrangedRect.Y, expectedY - 1e-6, expectedY + 1e-6);
    }

    [Fact]
    public void AlignItems_Stretch_Uses_LineHeight()
    {
        var container = ResolvedStyle.Default with { AlignItems = AlignItems.Stretch };
        var n1 = new FixedNode(5, 2);
        var n2 = new FixedNode(5, 3); // lineHeight = 3
        FlexLayout.Layout(new Size(20, 10), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default),
            (n2, ResolvedStyle.Default)
        });
        Assert.Equal(3, n1.ArrangedRect.Height);
        Assert.Equal(3, n2.ArrangedRect.Height);
    }

    [Fact]
    public void AlignItems_Baseline_Aligns_Bottoms_As_Approximation()
    {
        // Baseline approximated by aligning bottoms in a line
        var container = ResolvedStyle.Default with { AlignItems = AlignItems.Baseline };
        var n1 = new FixedNode(5, 2);
        var n2 = new FixedNode(5, 4);
        FlexLayout.Layout(new Size(30, 10), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default), (n2, ResolvedStyle.Default)
        });
        Assert.InRange(n1.ArrangedRect.Bottom, n2.ArrangedRect.Bottom - 1e-6, n2.ArrangedRect.Bottom + 1e-6);
    }
}
