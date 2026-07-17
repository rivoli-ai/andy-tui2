using System.Collections.Generic;
using Andy.Tui.Style;

namespace Andy.Tui.Layout.Tests;

/// <summary>
/// Covers the flex semantics completed for issue #35: container padding, child margins,
/// display:none, explicit width/height, column grow/shrink, justify recomputed after flex
/// sizing, column placement advancing by the constrained size, percentages, and fractional
/// results.
/// </summary>
public class FlexLayoutSemanticsTests
{
    private sealed class FixedNode : ILayoutNode
    {
        private readonly Size _size;
        public Rect ArrangedRect { get; private set; }
        public bool WasArranged { get; private set; }
        public FixedNode(double w, double h) { _size = new Size(w, h); }
        public Size Measure(in Size available) => _size;
        public void Arrange(in Rect finalRect) { ArrangedRect = finalRect; WasArranged = true; }
    }

    private static Length Px(double v) => new Length(v);
    private static Thickness All(double v) => new Thickness(Px(v), Px(v), Px(v), Px(v));

    [Fact]
    public void Container_Padding_Offsets_Origin_And_Shrinks_Content_Box()
    {
        var container = ResolvedStyle.Default with { Padding = new Thickness(Px(3), Px(2), Px(1), Px(4)), ColumnGap = Length.Zero };
        var n1 = new FixedNode(5, 5);
        // A grow item fills the content width so we can observe the padded content box.
        FlexLayout.Layout(new Size(30, 20), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default with { FlexGrow = 1 })
        });
        // Origin shifts by padding-left(3)/top(2); content width = 30 - 3 - 1 = 26.
        Assert.Equal(3, n1.ArrangedRect.X);
        Assert.Equal(2, n1.ArrangedRect.Y);
        Assert.InRange(n1.ArrangedRect.Width, 26 - 1e-6, 26 + 1e-6);
    }

    [Fact]
    public void Child_Margins_Consume_Main_Axis_Space_And_Offset_Item()
    {
        var container = ResolvedStyle.Default with { JustifyContent = JustifyContent.FlexStart, ColumnGap = Length.Zero };
        var n1 = new FixedNode(5, 2);
        var n2 = new FixedNode(5, 2);
        var child1 = ResolvedStyle.Default with { Margin = new Thickness(Px(2), Px(1), Px(3), Px(0)) };
        FlexLayout.Layout(new Size(40, 10), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, child1), (n2, ResolvedStyle.Default)
        });
        // n1 starts after its left margin (2). n2 starts after n1 outer extent: 2 + 5 + 3 = 10.
        Assert.Equal(2, n1.ArrangedRect.X);
        Assert.Equal(10, n2.ArrangedRect.X);
    }

    [Fact]
    public void Display_None_Items_Are_Skipped_Entirely()
    {
        var container = ResolvedStyle.Default with { ColumnGap = Length.Zero };
        var hidden = new FixedNode(5, 5);
        var visible = new FixedNode(5, 5);
        FlexLayout.Layout(new Size(40, 10), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (hidden, ResolvedStyle.Default with { Display = Display.None }),
            (visible, ResolvedStyle.Default)
        });
        Assert.False(hidden.WasArranged);
        // Visible item takes the first slot despite the hidden item being earlier in source order.
        Assert.Equal(0, visible.ArrangedRect.X);
    }

    [Fact]
    public void Explicit_Width_Overrides_Measured_Base_Size()
    {
        var container = ResolvedStyle.Default with { ColumnGap = Length.Zero };
        var n1 = new FixedNode(5, 3); // measures 5 wide
        FlexLayout.Layout(new Size(40, 10), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default with { Width = LengthOrAuto.FromPixels(12) })
        });
        Assert.Equal(12, n1.ArrangedRect.Width);
    }

    [Fact]
    public void Percentage_Width_Resolves_Against_Content_Box()
    {
        var container = ResolvedStyle.Default with { Padding = All(5), ColumnGap = Length.Zero };
        var n1 = new FixedNode(5, 3);
        // content width = 100 - 5 - 5 = 90; 50% => 45
        FlexLayout.Layout(new Size(100, 20), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default with { Width = LengthOrAuto.FromPercent(50) })
        });
        Assert.InRange(n1.ArrangedRect.Width, 45 - 1e-6, 45 + 1e-6);
    }

    [Fact]
    public void Column_FlexGrow_Distributes_Vertical_Free_Space()
    {
        var container = ResolvedStyle.Default with { FlexDirection = FlexDirection.Column, RowGap = Length.Zero };
        var n1 = new FixedNode(4, 10);
        var n2 = new FixedNode(4, 10);
        // height=40; base sum 20; free 20; grow 1:3 => +5 and +15 -> heights 15 and 25
        FlexLayout.Layout(new Size(20, 40), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default with { FlexGrow = 1 }),
            (n2, ResolvedStyle.Default with { FlexGrow = 3 })
        });
        Assert.InRange(n1.ArrangedRect.Height, 15 - 1e-6, 15 + 1e-6);
        Assert.InRange(n2.ArrangedRect.Height, 25 - 1e-6, 25 + 1e-6);
        // Second item is placed after the FIRST item's constrained (grown) height, not its original.
        Assert.Equal(15, n2.ArrangedRect.Y);
    }

    [Fact]
    public void Column_FlexShrink_Reduces_Vertical_Space_Proportionally()
    {
        var container = ResolvedStyle.Default with { FlexDirection = FlexDirection.Column, RowGap = Length.Zero };
        var n1 = new FixedNode(4, 30);
        var n2 = new FixedNode(4, 30);
        // height=40; deficit 20; shrink weights 1*30 and 3*30 => reduce 5 and 15 -> heights 25 and 15
        FlexLayout.Layout(new Size(20, 40), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default with { FlexShrink = 1 }),
            (n2, ResolvedStyle.Default with { FlexShrink = 3 })
        });
        Assert.InRange(n1.ArrangedRect.Height, 25 - 1e-6, 25 + 1e-6);
        Assert.InRange(n2.ArrangedRect.Height, 15 - 1e-6, 15 + 1e-6);
        Assert.Equal(25, n2.ArrangedRect.Y);
    }

    [Fact]
    public void JustifyContent_Is_Computed_After_Grow_So_No_Overflow()
    {
        // With grow filling all free space, a centered justify must not push items off-origin.
        var container = ResolvedStyle.Default with { JustifyContent = JustifyContent.Center, ColumnGap = Length.Zero };
        var n1 = new FixedNode(5, 2);
        var n2 = new FixedNode(5, 2);
        FlexLayout.Layout(new Size(40, 10), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default with { FlexGrow = 1 }),
            (n2, ResolvedStyle.Default with { FlexGrow = 1 })
        });
        // Items grow to 20 each, fill the container, so first starts exactly at 0.
        Assert.Equal(0, n1.ArrangedRect.X);
        Assert.InRange(n1.ArrangedRect.Width, 20 - 1e-6, 20 + 1e-6);
        Assert.InRange(n2.ArrangedRect.Width, 20 - 1e-6, 20 + 1e-6);
        Assert.Equal(20, n2.ArrangedRect.X);
    }

    [Fact]
    public void Arranged_Rectangles_Never_Overlap_After_Clamping_In_Column()
    {
        // n1 has a MaxHeight smaller than its measured height; the next item must not overlap it.
        var container = ResolvedStyle.Default with { FlexDirection = FlexDirection.Column, RowGap = Length.Zero };
        var n1 = new FixedNode(4, 20);
        var n2 = new FixedNode(4, 5);
        FlexLayout.Layout(new Size(20, 60), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default with { MaxHeight = LengthOrAuto.FromPixels(8) }),
            (n2, ResolvedStyle.Default)
        });
        Assert.InRange(n1.ArrangedRect.Height, 8 - 1e-6, 8 + 1e-6);
        // n2 begins at the clamped bottom of n1 (8), never inside it.
        Assert.True(n2.ArrangedRect.Y >= n1.ArrangedRect.Bottom - 1e-6);
        Assert.Equal(8, n2.ArrangedRect.Y);
    }

    [Fact]
    public void Fractional_Grow_Produces_Fractional_Widths()
    {
        var container = ResolvedStyle.Default with { ColumnGap = Length.Zero };
        var n1 = new FixedNode(0, 2);
        var n2 = new FixedNode(0, 2);
        var n3 = new FixedNode(0, 2);
        // width=10, three equal grow items => 10/3 each (non-integer)
        FlexLayout.Layout(new Size(10, 5), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default with { FlexGrow = 1 }),
            (n2, ResolvedStyle.Default with { FlexGrow = 1 }),
            (n3, ResolvedStyle.Default with { FlexGrow = 1 })
        });
        double third = 10.0 / 3.0;
        Assert.InRange(n1.ArrangedRect.Width, third - 1e-6, third + 1e-6);
        Assert.InRange(n2.ArrangedRect.Width, third - 1e-6, third + 1e-6);
        Assert.InRange(n3.ArrangedRect.Width, third - 1e-6, third + 1e-6);
    }

    [Fact]
    public void Column_Cross_Margins_Offset_Item_Horizontally()
    {
        var container = ResolvedStyle.Default with { FlexDirection = FlexDirection.Column, AlignItems = AlignItems.FlexStart, RowGap = Length.Zero };
        var n1 = new FixedNode(4, 3);
        FlexLayout.Layout(new Size(20, 20), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default with { Margin = new Thickness(Px(6), Px(0), Px(0), Px(0)) })
        });
        // flex-start on the horizontal cross axis + left margin 6.
        Assert.Equal(6, n1.ArrangedRect.X);
    }

    [Fact]
    public void Row_Cross_Margin_Applied_In_Center_Alignment()
    {
        var container = ResolvedStyle.Default with { AlignItems = AlignItems.Center, ColumnGap = Length.Zero };
        var n1 = new FixedNode(5, 4);
        // top margin 2 shifts the centered position down by the margin within the line box.
        FlexLayout.Layout(new Size(20, 10), container, new List<(ILayoutNode, ResolvedStyle)>
        {
            (n1, ResolvedStyle.Default with { Margin = new Thickness(Px(0), Px(2), Px(0), Px(0)) })
        });
        // line cross size = outer cross = 2 + 4 = 6; base offset = (10-6)/2 = 2; itemY = 2 + (6-6)/2 + 2 = 4
        Assert.InRange(n1.ArrangedRect.Y, 4 - 1e-6, 4 + 1e-6);
    }
}
