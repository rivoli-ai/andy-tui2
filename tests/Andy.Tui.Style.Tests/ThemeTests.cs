using System.Collections.Generic;
using Andy.Tui.Style;

namespace Andy.Tui.Style.Tests;

public class ThemeTests
{
    [Fact]
    public void Get_ReturnsDefinedToken()
    {
        var theme = new Theme("t", new Dictionary<ThemeToken, RgbaColor>
        {
            [ThemeToken.Accent] = RgbaColor.FromRgb(10, 20, 30),
        });
        Assert.Equal(RgbaColor.FromRgb(10, 20, 30), theme.Get(ThemeToken.Accent));
        Assert.True(theme.Has(ThemeToken.Accent));
    }

    [Fact]
    public void Get_UndefinedToken_IsTransparent()
    {
        var theme = new Theme("t", new Dictionary<ThemeToken, RgbaColor>());
        Assert.True(theme.Get(ThemeToken.Accent).IsTransparent);
        Assert.False(theme.Has(ThemeToken.Accent));
    }

    [Fact]
    public void GetRgb_UsesFallback_WhenTokenUndefinedOrTransparent()
    {
        var theme = new Theme("t", new Dictionary<ThemeToken, RgbaColor>
        {
            [ThemeToken.Surface] = RgbaColor.Transparent,
        });
        var fallback = new DisplayList.Rgb24(1, 2, 3);
        Assert.Equal(fallback, theme.GetRgb(ThemeToken.Surface, fallback)); // transparent -> fallback
        Assert.Equal(fallback, theme.GetRgb(ThemeToken.Accent, fallback));  // undefined -> fallback
    }

    [Fact]
    public void GetRgb_ReturnsTokenColor_WhenDefined()
    {
        var theme = BuiltinThemes.Dark;
        var fallback = new DisplayList.Rgb24(0, 0, 0);
        Assert.Equal(new DisplayList.Rgb24(40, 40, 40), theme.GetRgb(ThemeToken.Surface, fallback));
    }

    [Fact]
    public void With_DerivesIndependentVariant()
    {
        var baseTheme = BuiltinThemes.Dark;
        var variant = baseTheme.With(ThemeToken.Accent, RgbaColor.FromRgb(1, 1, 1));
        Assert.Equal(RgbaColor.FromRgb(1, 1, 1), variant.Get(ThemeToken.Accent));
        Assert.NotEqual(RgbaColor.FromRgb(1, 1, 1), baseTheme.Get(ThemeToken.Accent)); // original untouched
    }

    [Theory]
    [InlineData(ThemeToken.Surface, 40, 40, 40)]
    [InlineData(ThemeToken.SurfaceHover, 55, 55, 55)]
    [InlineData(ThemeToken.SurfaceActive, 80, 80, 120)]
    [InlineData(ThemeToken.SurfaceDisabled, 30, 30, 30)]
    [InlineData(ThemeToken.SurfaceSunken, 20, 20, 20)]
    [InlineData(ThemeToken.SurfaceSelected, 60, 60, 100)]
    [InlineData(ThemeToken.Background, 12, 12, 12)]
    [InlineData(ThemeToken.Foreground, 220, 220, 220)]
    [InlineData(ThemeToken.Border, 100, 100, 100)]
    [InlineData(ThemeToken.Success, 60, 120, 70)]
    [InlineData(ThemeToken.Info, 60, 140, 220)]
    public void DarkTheme_ReproducesHistoricWidgetPalette(ThemeToken token, byte r, byte g, byte b)
    {
        // Guard: these values are what migrated widgets fall back to, so Dark must
        // match them exactly for theming to be a visual no-op under the default theme.
        Assert.Equal(RgbaColor.FromRgb(r, g, b), BuiltinThemes.Dark.Get(token));
    }

    [Fact]
    public void ByName_IsCaseInsensitive_AndUnknownIsNull()
    {
        Assert.Same(BuiltinThemes.Dark, BuiltinThemes.ByName("DARK"));
        Assert.Same(BuiltinThemes.Light, BuiltinThemes.ByName("light"));
        Assert.Null(BuiltinThemes.ByName("nope"));
    }
}
