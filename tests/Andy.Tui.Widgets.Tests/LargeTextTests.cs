using System;
using System.Linq;
using Xunit;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class LargeTextTests
{
    [Fact]
    public void Measure_Accounts_For_Scale_And_Spacing()
    {
        var lt = new Andy.Tui.Widgets.LargeText();
        lt.SetText("12");
        lt.SetStyle(Andy.Tui.Widgets.LargeText.LargeTextStyle.Block);
        lt.SetScale(2);
        lt.SetSpacing(1);
        var (w, h) = lt.Measure();
        Assert.True(w > 0 && h > 0);
        // With Block base 5x5, scale 2 => 10x10 per glyph + 1 spacing => 21 width for two glyphs
        Assert.Equal(21, w);
        Assert.Equal(10, h);
    }
}
