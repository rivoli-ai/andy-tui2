using System.Collections.Generic;
using Andy.Tui.Style;
using Xunit;

namespace Andy.Tui.Style.Tests;

/// <summary>
/// Covers issue #38: complete ResolvedStyle property mapping, cascade ordering
/// without magic offsets, explicit media behavior with no environment, and
/// diagnostics for recognized properties carrying invalid values.
/// </summary>
public class StyleResolverCascadeTests
{
    private static Node Div(params string[] classes) => new("div", classes: classes);

    private static Stylesheet Sheet(Selector selector, int sourceOrder, params (string, object)[] decls)
    {
        var dict = new Dictionary<string, object>();
        foreach (var (k, v) in decls) dict[k] = v;
        return new Stylesheet(new[] { new Rule(selector, dict, sourceOrder) });
    }

    // ---- Cascade ordering without magic numeric offsets ----

    [Fact]
    public void LaterSheetWins_EvenWhenEarlierSheetHasHugeSourceOrderNumbers()
    {
        // The old resolver added a fixed 10_000 offset per sheet; an earlier
        // rule with SourceOrder >= 10_000 could beat a later sheet. It must not.
        var node = Div("x");
        var early = Sheet(new ClassSelector("x"), sourceOrder: 50_000, ("color", RgbaColor.FromRgb(255, 0, 0)));
        var late = Sheet(new ClassSelector("x"), sourceOrder: 0, ("color", RgbaColor.FromRgb(0, 255, 0)));

        var style = new StyleResolver().Compute(node, new[] { early, late });

        Assert.Equal(RgbaColor.FromRgb(0, 255, 0), style.Color);
    }

    [Fact]
    public void HigherSpecificityStillBeatsLaterSheet()
    {
        var node = new Node("div", id: "main", classes: new[] { "x" });
        // Later sheet, lower specificity (class) vs earlier sheet, higher specificity (id).
        var earlyId = Sheet(new IdSelector("main"), sourceOrder: 0, ("color", RgbaColor.FromRgb(1, 1, 1)));
        var lateClass = Sheet(new ClassSelector("x"), sourceOrder: 0, ("color", RgbaColor.FromRgb(2, 2, 2)));

        var style = new StyleResolver().Compute(node, new[] { earlyId, lateClass });

        Assert.Equal(RgbaColor.FromRgb(1, 1, 1), style.Color);
    }

    // ---- Min/Max/AlignSelf now flow through ----

    [Fact]
    public void MinMaxAndAlignSelf_AreMapped()
    {
        var css = ".c { min-width: 10; min-height: 20; max-width: 100; max-height: 200; align-self: center; }";
        var sheet = CssParser.Parse(css);
        var style = new StyleResolver().Compute(Div("c"), new[] { sheet });

        Assert.Equal(10, style.MinWidth.Value!.Value.Pixels);
        Assert.Equal(20, style.MinHeight.Value!.Value.Pixels);
        Assert.Equal(100, style.MaxWidth.Value!.Value.Pixels);
        Assert.Equal(200, style.MaxHeight.Value!.Value.Pixels);
        Assert.Equal(AlignSelf.Center, style.AlignSelf);
    }

    [Fact]
    public void MaxWidth_None_ResolvesToAuto()
    {
        var css = ".c { max-width: none; }";
        var sheet = CssParser.Parse(css);
        var style = new StyleResolver().Compute(Div("c"), new[] { sheet });
        Assert.True(style.MaxWidth.IsAuto);
    }

    [Fact]
    public void MinWidth_Percentage_IsPreserved()
    {
        var css = ".c { min-width: 50%; }";
        var sheet = CssParser.Parse(css);
        var style = new StyleResolver().Compute(Div("c"), new[] { sheet });
        Assert.True(style.MinWidth.Value!.Value.IsPercent);
        Assert.Equal(50, style.MinWidth.Value!.Value.Percentage);
    }

    [Fact]
    public void AlignSelf_KebabKeyword_Parses()
    {
        var css = ".c { align-self: flex-end; }";
        var sheet = CssParser.Parse(css);
        var style = new StyleResolver().Compute(Div("c"), new[] { sheet });
        Assert.Equal(AlignSelf.FlexEnd, style.AlignSelf);
    }

    // ---- Property matrix: every supported declaration resolves ----

    [Theory]
    [InlineData("display: none;", "Display", Display.None)]
    [InlineData("flex-direction: column;", "FlexDirection", FlexDirection.Column)]
    [InlineData("flex-wrap: wrap-reverse;", "FlexWrap", FlexWrap.WrapReverse)]
    [InlineData("justify-content: space-between;", "JustifyContent", JustifyContent.SpaceBetween)]
    [InlineData("align-items: flex-end;", "AlignItems", AlignItems.FlexEnd)]
    [InlineData("align-self: baseline;", "AlignSelf", AlignSelf.Baseline)]
    [InlineData("align-content: space-around;", "AlignContent", AlignContent.SpaceAround)]
    [InlineData("overflow: hidden;", "Overflow", Overflow.Hidden)]
    [InlineData("font-style: italic;", "FontStyle", FontStyle.Italic)]
    public void PropertyMatrix_EnumDeclarations_Resolve(string css, string field, object expected)
    {
        var sheet = CssParser.Parse(".c { " + css + " }");
        var style = new StyleResolver().Compute(Div("c"), new[] { sheet });
        var actual = typeof(ResolvedStyle).GetProperty(field)!.GetValue(style);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PropertyMatrix_NumericAndLength_Resolve()
    {
        var css = ".c { order: 3; flex-grow: 2; flex-shrink: 0; flex-basis: 40; width: 80; height: 90%; row-gap: 4; column-gap: 5; }";
        var sheet = CssParser.Parse(css);
        var style = new StyleResolver().Compute(Div("c"), new[] { sheet });

        Assert.Equal(3, style.Order);
        Assert.Equal(2, style.FlexGrow);
        Assert.Equal(0, style.FlexShrink);
        Assert.Equal(40, style.FlexBasis.Value!.Value.Pixels);
        Assert.Equal(80, style.Width.Value!.Value.Pixels);
        Assert.True(style.Height.Value!.Value.IsPercent);
        Assert.Equal(4, style.RowGap.Pixels);
        Assert.Equal(5, style.ColumnGap.Pixels);
    }

    [Fact]
    public void FontWeight_NumericAndPx_Length_ParseConsistently()
    {
        var sheet = CssParser.Parse(".c { font-weight: 700; width: 12px; }");
        var style = new StyleResolver().Compute(Div("c"), new[] { sheet });
        Assert.Equal(FontWeight.Bold, style.FontWeight);
        Assert.Equal(12, style.Width.Value!.Value.Pixels);
    }

    // ---- Media behavior when no environment is supplied ----

    [Fact]
    public void MediaGatedRule_DoesNotApply_WhenNoEnvironmentSupplied()
    {
        // Explicit behavior: a media query cannot be evaluated without an
        // environment, so the gated declaration must not apply.
        var css = "@media(min-width: 100) .c { color: blue; }";
        var sheet = CssParser.Parse(css);
        var style = new StyleResolver().Compute(Div("c"), new[] { sheet }, env: null);
        Assert.Equal(ResolvedStyle.Default.Color, style.Color);
    }

    [Fact]
    public void MediaGatedRule_Applies_WhenEnvironmentMatches()
    {
        var css = "@media(min-width: 100) .c { color: blue; }";
        var sheet = CssParser.Parse(css);
        var style = new StyleResolver().Compute(Div("c"), new[] { sheet }, new EnvironmentContext { ViewportWidth = 200 });
        Assert.Equal(RgbaColor.FromRgb(0, 0, 255), style.Color);
    }

    // ---- Diagnostics for recognized properties with invalid values ----

    [Fact]
    public void InvalidValue_OnRecognizedProperty_ReportsDiagnostic_AndFallsBack()
    {
        var css = ".c { width: banana; align-items: sideways; color: notacolor; }";
        var sheet = CssParser.Parse(css);
        var diagnostics = new List<StyleDiagnostic>();
        var style = new StyleResolver().Compute(Div("c"), new[] { sheet }, env: null, parent: null, diagnostics);

        Assert.Equal(ResolvedStyle.Default.Width, style.Width);
        Assert.Equal(ResolvedStyle.Default.AlignItems, style.AlignItems);
        Assert.Equal(ResolvedStyle.Default.Color, style.Color);

        Assert.Contains(diagnostics, d => d.Property == "width");
        Assert.Contains(diagnostics, d => d.Property == "align-items");
        Assert.Contains(diagnostics, d => d.Property == "color");
    }

    [Fact]
    public void ValidValues_ProduceNoDiagnostics()
    {
        var css = ".c { width: 10; align-items: center; color: red; min-width: 5%; align-self: flex-start; }";
        var sheet = CssParser.Parse(css);
        var diagnostics = new List<StyleDiagnostic>();
        new StyleResolver().Compute(Div("c"), new[] { sheet }, env: null, parent: null, diagnostics);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void CssWideKeyword_OnEnum_IsNotDiagnosedAsInvalid()
    {
        var css = ".c { font-style: inherit; }";
        var sheet = CssParser.Parse(css);
        var diagnostics = new List<StyleDiagnostic>();
        new StyleResolver().Compute(Div("c"), new[] { sheet }, env: null, parent: null, diagnostics);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void CssWideKeyword_OnLengthNumberInt_IsNotDiagnosedAsInvalid()
    {
        // inherit/initial/unset are valid on every property. On length/number/int
        // properties they must fall back silently, never emitting a diagnostic.
        var css = ".c { width: inherit; order: unset; flex-grow: initial; height: inherit; row-gap: initial; }";
        var sheet = CssParser.Parse(css);
        var diagnostics = new List<StyleDiagnostic>();
        var style = new StyleResolver().Compute(Div("c"), new[] { sheet }, env: null, parent: null, diagnostics);

        Assert.Empty(diagnostics);
        // Falls back to defaults since no explicit value/keyword resolution applies here.
        Assert.Equal(ResolvedStyle.Default.Width, style.Width);
        Assert.Equal(ResolvedStyle.Default.Order, style.Order);
        Assert.Equal(ResolvedStyle.Default.FlexGrow, style.FlexGrow);
        Assert.Equal(ResolvedStyle.Default.Height, style.Height);
        Assert.Equal(ResolvedStyle.Default.RowGap, style.RowGap);
    }

    [Theory]
    [InlineData("max-width")]
    [InlineData("max-height")]
    public void None_OnMaxConstraint_ResolvesToAuto_WithoutDiagnostic(string property)
    {
        var css = ".c { " + property + ": none; }";
        var sheet = CssParser.Parse(css);
        var diagnostics = new List<StyleDiagnostic>();
        var style = new StyleResolver().Compute(Div("c"), new[] { sheet }, env: null, parent: null, diagnostics);

        Assert.Empty(diagnostics);
        var value = (LengthOrAuto)typeof(ResolvedStyle)
            .GetProperty(property == "max-width" ? "MaxWidth" : "MaxHeight")!
            .GetValue(style)!;
        Assert.True(value.IsAuto);
    }

    [Theory]
    [InlineData("width")]
    [InlineData("height")]
    [InlineData("min-width")]
    [InlineData("min-height")]
    [InlineData("flex-basis")]
    public void None_OnNonMaxLengthProperty_IsDiagnosed_AndFallsBack(string property)
    {
        // 'none' is only valid on max-width/max-height. On any other length
        // property it is invalid CSS and must be reported, not silently accepted.
        var css = ".c { " + property + ": none; }";
        var sheet = CssParser.Parse(css);
        var diagnostics = new List<StyleDiagnostic>();
        var style = new StyleResolver().Compute(Div("c"), new[] { sheet }, env: null, parent: null, diagnostics);

        Assert.Contains(diagnostics, d => d.Property == property);

        var field = property switch
        {
            "width" => "Width",
            "height" => "Height",
            "min-width" => "MinWidth",
            "min-height" => "MinHeight",
            "flex-basis" => "FlexBasis",
            _ => throw new System.ArgumentOutOfRangeException(nameof(property))
        };
        var value = (LengthOrAuto)typeof(ResolvedStyle).GetProperty(field)!.GetValue(style)!;
        var expected = (LengthOrAuto)typeof(ResolvedStyle).GetProperty(field)!.GetValue(ResolvedStyle.Default)!;
        Assert.Equal(expected, value);
    }
}
