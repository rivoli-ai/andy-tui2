using System;
using System.Collections.Generic;
using System.Linq;
using Andy.Tui.Style;

namespace Andy.Tui.Style.Tests;

/// <summary>Edge-case and guard coverage for the theme types.</summary>
public class ThemeCoverageTests
{
    [Fact]
    public void Theme_Rename_KeepsColorsChangesName()
    {
        var t = BuiltinThemes.Dark.Rename("dark-copy");
        Assert.Equal("dark-copy", t.Name);
        Assert.Equal(BuiltinThemes.Dark.Get(ThemeToken.Accent), t.Get(ThemeToken.Accent));
    }

    [Fact]
    public void Theme_Constructor_RejectsNulls()
    {
        Assert.Throws<ArgumentNullException>(() => new Theme(null!, new Dictionary<ThemeToken, RgbaColor>()));
        Assert.Throws<ArgumentNullException>(() => new Theme("x", null!));
    }

    [Fact]
    public void ThemeContext_Set_RejectsNull_AndRestores()
    {
        try
        {
            Assert.Throws<ArgumentNullException>(() => ThemeContext.Set(null!));
            ThemeContext.Set(BuiltinThemes.Light);
            Assert.Same(BuiltinThemes.Light, ThemeContext.Current);
        }
        finally
        {
            ThemeContext.Set(BuiltinThemes.Dark);
        }
    }

    [Fact]
    public void BuiltinThemes_ByName_UnknownIsNull_KnownCaseInsensitive()
    {
        Assert.Null(BuiltinThemes.ByName("does-not-exist"));
        Assert.Same(BuiltinThemes.HighContrast, BuiltinThemes.ByName("High-Contrast"));
    }

    [Fact]
    public void Themes_Registry_ByName_IsCaseInsensitive_AndUnknownNull()
    {
        Assert.Equal("gruvbox-dark", Themes.ByName("GRUVBOX-DARK")!.Name);
        Assert.Null(Themes.ByName("nope"));
    }

    [Fact]
    public void Themes_All_NamesAreUnique_AcrossBuiltinsAndPopular()
    {
        var names = Themes.All.Select(t => t.Name).ToList();
        Assert.Equal(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void ToVariableSheet_TransparentToken_ResolvesToTerminalDefault()
    {
        // A theme whose Accent is transparent should emit `--accent: transparent`,
        // and a var() consumer must resolve to a transparent (terminal-default) color.
        var theme = new Theme("t", new Dictionary<ThemeToken, RgbaColor>
        {
            [ThemeToken.Accent] = RgbaColor.Transparent,
        });
        var sheet = theme.ToVariableSheet();
        var app = CssParser.Parse("div { color: var(--accent); }");
        var style = new StyleResolver().Compute(new Node("div"), new[] { sheet, app });
        Assert.True(style.Color.IsTransparent);
    }

    [Fact]
    public void UniversalSelector_HasZeroSpecificity_AndMatchesAll()
    {
        var sel = new UniversalSelector();
        Assert.True(sel.Matches(new Node("anything", id: "x", classes: new[] { "y" })));
        Assert.Equal(new Specificity(0, 0, 0), sel.Specificity);
    }

    [Fact]
    public void TypeSelector_Star_MatchesAnyType()
    {
        var star = new TypeSelector("*");
        Assert.True(star.Matches(new Node("div")));
        Assert.True(star.Matches(new Node("button")));
    }

    [Fact]
    public void GetRgb_DefinedToken_ConvertsToRgb24()
    {
        var theme = new Theme("t", new Dictionary<ThemeToken, RgbaColor>
        {
            [ThemeToken.Border] = RgbaColor.FromRgb(5, 6, 7),
        });
        Assert.Equal(new DisplayList.Rgb24(5, 6, 7), theme.GetRgb(ThemeToken.Border, new DisplayList.Rgb24(0, 0, 0)));
    }
}
