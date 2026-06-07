using System.Collections.Generic;
using Andy.Tui.Style;
using Andy.Tui.Widgets;
using DL = Andy.Tui.DisplayList;

namespace Andy.Tui.Widgets.Tests;

public class ThemeContextWidgetTests
{
    [Fact]
    public void Button_SeedsColorsFromAmbientTheme_AtConstruction()
    {
        var custom = new Theme("custom", new Dictionary<ThemeToken, RgbaColor>
        {
            [ThemeToken.Surface] = RgbaColor.FromRgb(11, 22, 33),
            [ThemeToken.Foreground] = RgbaColor.FromRgb(200, 100, 50),
        });
        try
        {
            ThemeContext.Set(custom);
            var button = new Button("ok");
            Assert.Equal(new DL.Rgb24(11, 22, 33), button.Bg);
            Assert.Equal(new DL.Rgb24(200, 100, 50), button.Fg);
        }
        finally
        {
            ThemeContext.Set(BuiltinThemes.Dark);
        }
    }

    [Fact]
    public void DefaultTheme_IsVisualNoOp_ForButton()
    {
        ThemeContext.Set(BuiltinThemes.Dark);
        var button = new Button("ok");
        // Historic hardcoded defaults must be preserved under Dark.
        Assert.Equal(new DL.Rgb24(40, 40, 40), button.Bg);
        Assert.Equal(new DL.Rgb24(220, 220, 220), button.Fg);
        Assert.Equal(new DL.Rgb24(100, 100, 100), button.Border);
    }

    [Fact]
    public void Set_RaisesChangedEvent()
    {
        Theme? observed = null;
        void Handler(Theme t) => observed = t;
        ThemeContext.Changed += Handler;
        try
        {
            ThemeContext.Set(BuiltinThemes.Light);
            Assert.Same(BuiltinThemes.Light, observed);
        }
        finally
        {
            ThemeContext.Changed -= Handler;
            ThemeContext.Set(BuiltinThemes.Dark);
        }
    }
}
