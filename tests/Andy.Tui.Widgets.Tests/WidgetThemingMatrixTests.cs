using System;
using System.Collections.Generic;
using Andy.Tui.Style;
using Andy.Tui.Widgets;
using DL = Andy.Tui.DisplayList;

namespace Andy.Tui.Widgets.Tests;

/// <summary>
/// Verifies each migrated widget seeds its color knobs from the correct theme token.
/// A sentinel theme gives every token a unique color so a wrong mapping is caught.
/// </summary>
public class WidgetThemingMatrixTests
{
    private static Theme Sentinel()
    {
        // Distinct value per token (token ordinal encoded in the channels).
        var d = new Dictionary<ThemeToken, RgbaColor>();
        foreach (ThemeToken t in Enum.GetValues(typeof(ThemeToken)))
        {
            byte v = (byte)(10 + (int)t);
            d[t] = RgbaColor.FromRgb(v, (byte)(v + 1), (byte)(v + 2));
        }
        return new Theme("sentinel", d);
    }

    private static DL.Rgb24 Tok(Theme t, ThemeToken token) => t.GetRgb(token, new DL.Rgb24(0, 0, 0));

    [Fact]
    public void AllMigratedWidgets_SeedExpectedTokens()
    {
        var th = Sentinel();
        try
        {
            ThemeContext.Set(th);

            var checkbox = new Checkbox("c");
            Assert.Equal(Tok(th, ThemeToken.Surface), checkbox.Bg);
            Assert.Equal(Tok(th, ThemeToken.Foreground), checkbox.Fg);
            Assert.Equal(Tok(th, ThemeToken.Border), checkbox.Border);

            var radio = new RadioGroup();
            Assert.Equal(Tok(th, ThemeToken.Surface), radio.Bg);

            var toggle = new Toggle(true, "t");
            Assert.Equal(Tok(th, ThemeToken.Success), toggle.BgOn);
            Assert.Equal(Tok(th, ThemeToken.Foreground), toggle.Fg);

            var input = new TextInput();
            Assert.Equal(Tok(th, ThemeToken.SurfaceSunken), input.Bg);

            var slider = new Slider();
            Assert.Equal(Tok(th, ThemeToken.SurfaceSunken), slider.Bg);
            Assert.Equal(Tok(th, ThemeToken.Border), slider.Border);

            var progress = new ProgressBar();
            Assert.Equal(Tok(th, ThemeToken.Surface), progress.Bg);
            Assert.Equal(Tok(th, ThemeToken.Info), progress.Fill);

            var list = new ListBox();
            Assert.Equal(Tok(th, ThemeToken.SurfaceSunken), list.Bg);
            Assert.Equal(Tok(th, ThemeToken.SurfaceSelected), list.SelectedBg);

            var panel = new Panel();
            Assert.Equal(Tok(th, ThemeToken.Background), panel.Bg);
            Assert.Equal(Tok(th, ThemeToken.Border), panel.Border);

            var select = new Select();
            Assert.Equal(Tok(th, ThemeToken.Background), select.Bg);
            Assert.Equal(Tok(th, ThemeToken.Foreground), select.Fg);

            var button = new Button("b");
            Assert.Equal(Tok(th, ThemeToken.Surface), button.Bg);
            Assert.Equal(Tok(th, ThemeToken.SurfaceHover), button.BgHover);
            Assert.Equal(Tok(th, ThemeToken.SurfaceActive), button.BgActive);
            Assert.Equal(Tok(th, ThemeToken.SurfaceDisabled), button.BgDisabled);
        }
        finally
        {
            ThemeContext.Set(BuiltinThemes.Dark);
        }
    }
}
