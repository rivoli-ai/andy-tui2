using Andy.Tui.Style;

namespace Andy.Tui.Style.Tests;

public class ThemeCssTests
{
    [Fact]
    public void VarName_KebabCasesTokens()
    {
        Assert.Equal("--accent", ThemeCss.VarName(ThemeToken.Accent));
        Assert.Equal("--surface-hover", ThemeCss.VarName(ThemeToken.SurfaceHover));
        Assert.Equal("--foreground-disabled", ThemeCss.VarName(ThemeToken.ForegroundDisabled));
    }

    [Fact]
    public void UniversalSelector_MatchesEveryNode()
    {
        var sheet = CssParser.Parse("* { color: rgb(1,2,3); }");
        var resolver = new StyleResolver();
        foreach (var type in new[] { "div", "button", "anything" })
        {
            var style = resolver.Compute(new Node(type), new[] { sheet });
            Assert.Equal(RgbaColor.FromRgb(1, 2, 3), style.Color);
        }
    }

    [Fact]
    public void ThemeVariables_ResolveThroughVarReference()
    {
        var themeSheet = BuiltinThemes.Dark.ToVariableSheet();
        var appSheet = CssParser.Parse("div { color: var(--accent); background-color: var(--surface); }");
        var resolver = new StyleResolver();

        var style = resolver.Compute(new Node("div"), new[] { themeSheet, appSheet });

        Assert.Equal(BuiltinThemes.Dark.Get(ThemeToken.Accent), style.Color);
        Assert.Equal(BuiltinThemes.Dark.Get(ThemeToken.Surface), style.BackgroundColor);
    }

    [Fact]
    public void SwappingThemeSheet_RestylesViaSameVars()
    {
        var appSheet = CssParser.Parse("div { color: var(--accent); }");
        var resolver = new StyleResolver();

        var dark = resolver.Compute(new Node("div"), new[] { BuiltinThemes.Dark.ToVariableSheet(), appSheet });
        var light = resolver.Compute(new Node("div"), new[] { BuiltinThemes.Light.ToVariableSheet(), appSheet });

        Assert.Equal(BuiltinThemes.Dark.Get(ThemeToken.Accent), dark.Color);
        Assert.Equal(BuiltinThemes.Light.Get(ThemeToken.Accent), light.Color);
        Assert.NotEqual(dark.Color, light.Color);
    }

    [Fact]
    public void AppRule_OverridesThemeVariable_ViaCascade()
    {
        // App defines a more specific value; theme sheet is prepended (lower priority).
        var themeSheet = BuiltinThemes.Dark.ToVariableSheet();
        var appSheet = CssParser.Parse("#x { --accent: rgb(7,7,7); } #x { color: var(--accent); }");
        var resolver = new StyleResolver();

        var style = resolver.Compute(new Node("div", id: "x"), new[] { themeSheet, appSheet });

        Assert.Equal(RgbaColor.FromRgb(7, 7, 7), style.Color);
    }
}
