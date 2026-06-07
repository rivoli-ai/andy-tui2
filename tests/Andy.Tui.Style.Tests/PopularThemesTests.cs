using System;
using System.Linq;
using Andy.Tui.Style;

namespace Andy.Tui.Style.Tests;

public class PopularThemesTests
{
    [Fact]
    public void Provides32Themes()
    {
        Assert.Equal(32, PopularThemes.All.Count);
    }

    [Fact]
    public void EveryTheme_DefinesEveryToken_Opaquely()
    {
        foreach (var theme in PopularThemes.All)
        {
            foreach (ThemeToken tok in Enum.GetValues(typeof(ThemeToken)))
            {
                Assert.True(theme.Has(tok), $"{theme.Name} missing {tok}");
                Assert.False(theme.Get(tok).IsTransparent, $"{theme.Name}.{tok} is transparent");
            }
        }
    }

    [Fact]
    public void ThemeNames_AreUnique()
    {
        var names = PopularThemes.All.Select(t => t.Name).ToList();
        Assert.Equal(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Theory]
    [InlineData("dracula")]
    [InlineData("nord")]
    [InlineData("gruvbox-dark")]
    [InlineData("catppuccin-mocha")]
    [InlineData("tokyo-night")]
    public void ByName_FindsPopularThemes(string name)
    {
        var t = Themes.ByName(name);
        Assert.NotNull(t);
        Assert.Equal(name, t!.Name);
    }

    [Fact]
    public void DraculaPalette_MatchesCanonicalCoreColors()
    {
        var d = PopularThemes.Dracula;
        Assert.Equal(RgbaColor.FromRgb(0x28, 0x2a, 0x36), d.Get(ThemeToken.Background));
        Assert.Equal(RgbaColor.FromRgb(0xf8, 0xf8, 0xf2), d.Get(ThemeToken.Foreground));
        Assert.Equal(RgbaColor.FromRgb(0xbd, 0x93, 0xf9), d.Get(ThemeToken.Accent));
        Assert.Equal(RgbaColor.FromRgb(0x50, 0xfa, 0x7b), d.Get(ThemeToken.Success));
        Assert.Equal(RgbaColor.FromRgb(0xff, 0x55, 0x55), d.Get(ThemeToken.Error));
    }

    [Fact]
    public void Registry_CombinesBuiltinsAndPopular()
    {
        Assert.Equal(BuiltinThemes.All.Count + PopularThemes.All.Count, Themes.All.Count);
        Assert.Contains(BuiltinThemes.Dark, Themes.All);
        Assert.Contains(PopularThemes.Nord, Themes.All);
    }
}
