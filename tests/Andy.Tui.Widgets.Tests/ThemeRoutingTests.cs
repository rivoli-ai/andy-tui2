using System;
using System.Collections.Generic;
using Andy.Tui.Style;
using Andy.Tui.Widgets;
using DL = Andy.Tui.DisplayList;

namespace Andy.Tui.Widgets.Tests;

/// <summary>
/// Contract tests for issue #34: resolved styles and runtime theme changes must reach
/// every themeable widget without reconstructing it. Covers <see cref="IThemeable.ApplyTheme"/>,
/// <see cref="IStyleable.ApplyStyle"/>, and <see cref="ThemeRegistry"/> propagation across
/// the built-in dark, light, and high-contrast themes.
/// </summary>
public class ThemeRoutingTests
{
    // One representative instance per themeable widget category.
    private static IEnumerable<IThemeable> AllThemeableWidgets()
    {
        yield return new Button("ok");
        yield return new Checkbox("c");
        yield return new RadioGroup();
        yield return new Toggle();
        yield return new Slider();
        yield return new ProgressBar();
        yield return new TextInput();
        yield return new Select();
        yield return new ListBox();
        yield return new ListView();
        yield return new ScrollView();
        yield return new Panel();
        yield return new MenuBar();
        yield return new MenuPopup();
        yield return new ContextMenu();
        yield return new CommandPalette();
        yield return new RealTimeLogView();
        yield return new LargeText();
        yield return new CodeViewer();
        yield return new DiffViewer();
    }

    public static IEnumerable<object[]> ThemeCases()
    {
        yield return new object[] { BuiltinThemes.Dark };
        yield return new object[] { BuiltinThemes.Light };
        yield return new object[] { BuiltinThemes.HighContrast };
    }

    [Theory]
    [MemberData(nameof(ThemeCases))]
    public void ApplyTheme_ReSeedsButtonPalette_ForEveryBuiltinTheme(Theme theme)
    {
        var button = new Button("ok");
        button.ApplyTheme(theme);

        Assert.Equal(theme.GetRgb(ThemeToken.Surface, default), button.Bg);
        Assert.Equal(theme.GetRgb(ThemeToken.Foreground, default), button.Fg);
        Assert.Equal(theme.GetRgb(ThemeToken.Border, default), button.Border);
    }

    [Theory]
    [MemberData(nameof(ThemeCases))]
    public void ApplyTheme_DoesNotThrow_ForEveryThemeableWidget(Theme theme)
    {
        foreach (var widget in AllThemeableWidgets())
        {
            var ex = Record.Exception(() => widget.ApplyTheme(theme));
            Assert.Null(ex);
        }
    }

    [Fact]
    public void ApplyTheme_SwitchingThemes_ChangesAnExistingWidgetInPlace()
    {
        // Construct under dark, then switch to light without rebuilding the widget.
        var button = new Button("ok");
        button.ApplyTheme(BuiltinThemes.Dark);
        var darkBg = button.Bg;

        button.ApplyTheme(BuiltinThemes.Light);

        Assert.NotEqual(darkBg, button.Bg);
        Assert.Equal(BuiltinThemes.Light.GetRgb(ThemeToken.Surface, default), button.Bg);
    }

    [Fact]
    public void ThemeRegistry_Set_RestylesRegisteredWidgets_InOnePass()
    {
        var restore = ThemeContext.Current;
        try
        {
            ThemeContext.Set(BuiltinThemes.Dark);
            var button = new Button("ok");
            var checkbox = new Checkbox("c");
            var panel = new Panel();
            ThemeRegistry.Register(button);
            ThemeRegistry.Register(checkbox);
            ThemeRegistry.Register(panel);

            // A single ambient theme switch propagates to every registered widget.
            ThemeContext.Set(BuiltinThemes.Light);

            Assert.Equal(BuiltinThemes.Light.GetRgb(ThemeToken.Surface, default), button.Bg);
            Assert.Equal(BuiltinThemes.Light.GetRgb(ThemeToken.Surface, default), checkbox.Bg);
            Assert.Equal(BuiltinThemes.Light.GetRgb(ThemeToken.Background, default), panel.Bg);

            ThemeRegistry.Unregister(button);
            ThemeRegistry.Unregister(checkbox);
            ThemeRegistry.Unregister(panel);
        }
        finally
        {
            ThemeContext.Set(restore);
        }
    }

    [Fact]
    public void ThemeRegistry_Register_AppliesCurrentThemeImmediately()
    {
        var restore = ThemeContext.Current;
        try
        {
            ThemeContext.Set(BuiltinThemes.HighContrast);
            var button = new Button("ok");
            // Even if the widget was built earlier under another theme, registering
            // brings it up to date with the current ambient theme.
            button.ApplyTheme(BuiltinThemes.Dark);
            ThemeRegistry.Register(button);

            Assert.Equal(BuiltinThemes.HighContrast.GetRgb(ThemeToken.Surface, default), button.Bg);
            ThemeRegistry.Unregister(button);
        }
        finally
        {
            ThemeContext.Set(restore);
        }
    }

    [Fact]
    public void ThemeRegistry_Unregister_StopsFurtherUpdates()
    {
        var restore = ThemeContext.Current;
        try
        {
            ThemeContext.Set(BuiltinThemes.Dark);
            var button = new Button("ok");
            ThemeRegistry.Register(button);
            ThemeRegistry.Unregister(button);

            var before = button.Bg;
            ThemeContext.Set(BuiltinThemes.Light);
            Assert.Equal(before, button.Bg);
        }
        finally
        {
            ThemeContext.Set(restore);
        }
    }

    [Fact]
    public void ApplyStyle_ResolvedClassRule_OverridesWidgetForeground()
    {
        // A CSS class rule resolved through the cascade must visibly drive the widget.
        var sheet = CssParser.Parse(".danger { color: rgb(200,20,20); background-color: rgb(10,10,10); }");
        var node = new Node("button", classes: new[] { "danger" });
        var resolved = new StyleResolver().Compute(node, new[] { sheet });

        var button = new Button("delete");
        button.ApplyStyle(resolved);

        Assert.Equal(new DL.Rgb24(200, 20, 20), button.Fg);
        Assert.Equal(new DL.Rgb24(10, 10, 10), button.Bg);
    }

    [Fact]
    public void ApplyStyle_PseudoStateRule_AffectsWidget()
    {
        // A :focus pseudo-state rule must resolve and reach the widget.
        var sheet = CssParser.Parse("button { color: rgb(100,100,100); } button:focus { color: rgb(0,200,255); }");
        var focused = new Node("button", classes: null) { IsFocus = true };
        var resolved = new StyleResolver().Compute(focused, new[] { sheet });

        var button = new Button("ok");
        button.ApplyStyle(resolved);

        Assert.Equal(new DL.Rgb24(0, 200, 255), button.Fg);
    }

    [Fact]
    public void ApplyStyle_UnstyledResolvedNode_PreservesThemeForeground()
    {
        var button = new Button("ok");
        button.ApplyTheme(BuiltinThemes.Light);
        var expectedFg = button.Fg;

        // Resolve a real node whose stylesheet sets ONLY a background-color: the
        // resolver must yield a transparent (unset) foreground so ApplyStyle keeps the
        // widget's theme-seeded fg instead of clobbering it with an opaque default.
        var sheet = CssParser.Parse(".danger { background-color: rgb(10, 10, 10); }");
        var resolved = new StyleResolver().Compute(new Node("button", classes: new[] { "danger" }), new[] { sheet });

        button.ApplyStyle(resolved);

        // Foreground survives (no color rule was set), background is applied.
        Assert.Equal(expectedFg, button.Fg);
        Assert.Equal(new DL.Rgb24(10, 10, 10), button.Bg);
    }

    [Fact]
    public void ApplyStyle_FullyUnstyledResolvedNode_PreservesThemeForegroundAndBackground()
    {
        var button = new Button("ok");
        button.ApplyTheme(BuiltinThemes.Light);
        var expectedFg = button.Fg;
        var expectedBg = button.Bg;

        // A node matched by no rules must resolve to transparent color AND
        // background-color, leaving the widget's theme palette fully intact.
        var resolved = new StyleResolver().Compute(new Node("button"), Array.Empty<Stylesheet>());

        button.ApplyStyle(resolved);

        Assert.Equal(expectedFg, button.Fg);
        Assert.Equal(expectedBg, button.Bg);
    }
}
