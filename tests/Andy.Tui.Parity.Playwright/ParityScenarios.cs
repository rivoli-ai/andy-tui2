using System.Globalization;
using System.Text;
using Andy.Tui.Layout;
using Andy.Tui.Style;

namespace Andy.Tui.Parity.Playwright;

/// <summary>
/// A single flex child in a parity scenario. The same spec is used to emit
/// reference CSS (for the browser) and an Andy.Tui <see cref="ResolvedStyle"/>
/// (for <see cref="FlexLayout"/>), so both sides are generated from one source
/// of truth.
/// </summary>
public sealed record ItemSpec(
    double Width,
    double Height,
    double FlexGrow = 0,
    double FlexShrink = 1,
    double? MinWidth = null,
    double? MaxWidth = null,
    double? MinHeight = null,
    double? MaxHeight = null,
    bool DisplayNone = false);

/// <summary>
/// Declarative description of a flex container plus its children. Drives both
/// the Playwright browser parity tests and the browser-free deterministic
/// tests, guaranteeing that reference HTML and Andy.Tui inputs stay in sync.
/// </summary>
public sealed record ParityScenario(
    string Name,
    double ContainerWidth,
    double ContainerHeight,
    FlexDirection Direction,
    FlexWrap Wrap,
    JustifyContent Justify,
    AlignItems AlignItems,
    AlignContent AlignContent,
    double RowGap,
    double ColumnGap,
    IReadOnlyList<ItemSpec> Items)
{
    public Size ContainerSize => new(ContainerWidth, ContainerHeight);

    /// <summary>Container style translated into an Andy.Tui resolved style.</summary>
    public ResolvedStyle BuildContainerStyle() => ResolvedStyle.Default with
    {
        Display = Display.Flex,
        FlexDirection = Direction,
        FlexWrap = Wrap,
        JustifyContent = Justify,
        AlignItems = AlignItems,
        AlignContent = AlignContent,
        RowGap = new Length(RowGap),
        ColumnGap = new Length(ColumnGap),
    };

    /// <summary>Per-item style translated into an Andy.Tui resolved style.</summary>
    public static ResolvedStyle BuildItemStyle(ItemSpec item) => ResolvedStyle.Default with
    {
        Display = item.DisplayNone ? Display.None : Display.Flex,
        FlexGrow = item.FlexGrow,
        FlexShrink = item.FlexShrink,
        MinWidth = item.MinWidth is { } minW ? LengthOrAuto.FromPixels(minW) : LengthOrAuto.Auto(),
        MaxWidth = item.MaxWidth is { } maxW ? LengthOrAuto.FromPixels(maxW) : LengthOrAuto.Auto(),
        MinHeight = item.MinHeight is { } minH ? LengthOrAuto.FromPixels(minH) : LengthOrAuto.Auto(),
        MaxHeight = item.MaxHeight is { } maxH ? LengthOrAuto.FromPixels(maxH) : LengthOrAuto.Auto(),
    };

    /// <summary>Indices of items that participate in layout (display:none excluded), preserving DOM order.</summary>
    public IReadOnlyList<int> VisibleItemIndices()
    {
        var list = new List<int>();
        for (int i = 0; i < Items.Count; i++)
        {
            if (!Items[i].DisplayNone) list.Add(i);
        }
        return list;
    }

    /// <summary>Runs Andy.Tui's <see cref="FlexLayout"/> and returns each visible item's arranged rect, in DOM order.</summary>
    public Rect[] RunAndyTui()
    {
        var nodes = Items
            .Select(it => ((ILayoutNode)new FixedNode(it.Width, it.Height), BuildItemStyle(it)))
            .ToList<(ILayoutNode, ResolvedStyle)>();
        FlexLayout.Layout(ContainerSize, BuildContainerStyle(), nodes);
        return VisibleItemIndices()
            .Select(i => ((FixedNode)nodes[i].Item1).ArrangedRect)
            .ToArray();
    }

    /// <summary>Emits a self-contained reference HTML document that reproduces this scenario in a browser.</summary>
    public string ToReferenceHtml()
    {
        static string Px(double v) => v.ToString("0.####", CultureInfo.InvariantCulture) + "px";
        var sb = new StringBuilder();
        sb.Append("<style>html,body{margin:0;padding:0}*{box-sizing:border-box}");
        sb.Append(".c{display:flex;");
        sb.Append("flex-direction:").Append(Direction == FlexDirection.Row ? "row" : "column").Append(';');
        sb.Append("flex-wrap:").Append(Wrap switch
        {
            FlexWrap.Nowrap => "nowrap",
            FlexWrap.Wrap => "wrap",
            FlexWrap.WrapReverse => "wrap-reverse",
            _ => "nowrap",
        }).Append(';');
        sb.Append("justify-content:").Append(CssJustify(Justify)).Append(';');
        sb.Append("align-items:").Append(CssAlignItems(AlignItems)).Append(';');
        sb.Append("align-content:").Append(CssAlignContent(AlignContent)).Append(';');
        sb.Append("row-gap:").Append(Px(RowGap)).Append(';');
        sb.Append("column-gap:").Append(Px(ColumnGap)).Append(';');
        sb.Append("width:").Append(Px(ContainerWidth)).Append(';');
        sb.Append("height:").Append(Px(ContainerHeight)).Append(";}");
        sb.Append("</style>");
        sb.Append("<div class=\"c\">");
        foreach (var it in Items)
        {
            sb.Append("<div class=\"i\" style=\"");
            sb.Append("width:").Append(Px(it.Width)).Append(';');
            sb.Append("height:").Append(Px(it.Height)).Append(';');
            sb.Append("flex-grow:").Append(it.FlexGrow.ToString(CultureInfo.InvariantCulture)).Append(';');
            sb.Append("flex-shrink:").Append(it.FlexShrink.ToString(CultureInfo.InvariantCulture)).Append(';');
            if (it.MinWidth is { } minW) sb.Append("min-width:").Append(Px(minW)).Append(';');
            if (it.MaxWidth is { } maxW) sb.Append("max-width:").Append(Px(maxW)).Append(';');
            if (it.MinHeight is { } minH) sb.Append("min-height:").Append(Px(minH)).Append(';');
            if (it.MaxHeight is { } maxH) sb.Append("max-height:").Append(Px(maxH)).Append(';');
            if (it.DisplayNone) sb.Append("display:none;");
            sb.Append("\"></div>");
        }
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string CssJustify(JustifyContent v) => v switch
    {
        JustifyContent.FlexStart => "flex-start",
        JustifyContent.Center => "center",
        JustifyContent.FlexEnd => "flex-end",
        JustifyContent.SpaceBetween => "space-between",
        JustifyContent.SpaceAround => "space-around",
        JustifyContent.SpaceEvenly => "space-evenly",
        _ => "flex-start",
    };

    private static string CssAlignItems(AlignItems v) => v switch
    {
        AlignItems.Stretch => "stretch",
        AlignItems.FlexStart => "flex-start",
        AlignItems.Center => "center",
        AlignItems.FlexEnd => "flex-end",
        AlignItems.Baseline => "baseline",
        _ => "stretch",
    };

    private static string CssAlignContent(AlignContent v) => v switch
    {
        AlignContent.Stretch => "stretch",
        AlignContent.FlexStart => "flex-start",
        AlignContent.Center => "center",
        AlignContent.FlexEnd => "flex-end",
        AlignContent.SpaceBetween => "space-between",
        AlignContent.SpaceAround => "space-around",
        AlignContent.SpaceEvenly => "space-evenly",
        _ => "stretch",
    };

    /// <summary>Fixed-size layout node used to feed known intrinsic sizes into <see cref="FlexLayout"/>.</summary>
    private sealed class FixedNode : ILayoutNode
    {
        private readonly Size _size;
        public Rect ArrangedRect { get; private set; }
        public FixedNode(double w, double h) { _size = new Size(w, h); }
        public Size Measure(in Size available) => _size;
        public void Arrange(in Rect finalRect) { ArrangedRect = finalRect; }
    }
}

/// <summary>
/// Shared parity matrix. Every scenario is exercised both against a real
/// browser (<see cref="Fixtures"/>) and against hand-derived CSS-spec geometry
/// (<see cref="DeterministicParityTests"/>).
/// </summary>
public static class ParityScenarios
{
    /// <summary>
    /// Position comparison tolerance, in pixels.
    ///
    /// Rationale: browser layout applies sub-pixel rounding, so exact equality is
    /// not achievable. One pixel absorbs that rounding without hiding a real
    /// offset: every case in the matrix separates adjacent items by at least
    /// 10px (gaps) or a full item extent, so a systematic 2px+ drift on any
    /// axis still fails. The tolerance is applied per-axis, per-item against
    /// absolute container-relative positions, never against a summary statistic
    /// or an offset-normalized reference that could mask a consistent shift.
    /// </summary>
    public const double PositionTolerancePx = 1.0;

    public static IReadOnlyList<ParityScenario> All { get; } = BuildAll();

    /// <summary>
    /// Orders a set of container-relative top-left positions top-to-bottom,
    /// left-to-right so two independently produced sets compare in a stable
    /// order. Positions are NOT shifted: the absolute container-relative offset
    /// is preserved so that a systematic alignment offset (justify-content /
    /// align-items applied or ignored) is actually detected rather than being
    /// subtracted away. Callers must supply positions relative to the flex
    /// container's content box (browser side subtracts the container origin;
    /// Andy.Tui's arranged rects are already container-relative).
    /// </summary>
    public static (double X, double Y)[] OrderForComparison(IEnumerable<(double X, double Y)> points)
    {
        return points
            .OrderBy(p => Math.Round(p.Y, 3))
            .ThenBy(p => Math.Round(p.X, 3))
            .ToArray();
    }

    private static IReadOnlyList<ParityScenario> BuildAll()
    {
        var list = new List<ParityScenario>();

        // --- Wrap (previously known-failing cases) ---
        list.Add(new ParityScenario(
            "row_wrap_gaps", 300, 200,
            FlexDirection.Row, FlexWrap.Wrap, JustifyContent.FlexStart,
            AlignItems.FlexStart, AlignContent.FlexStart, 10, 10,
            Enumerable.Repeat(new ItemSpec(50, 20), 6).ToList()));

        list.Add(new ParityScenario(
            "column_wrap_gaps", 200, 200,
            FlexDirection.Column, FlexWrap.Wrap, JustifyContent.FlexStart,
            AlignItems.FlexStart, AlignContent.FlexStart, 10, 10,
            Enumerable.Repeat(new ItemSpec(50, 50), 4).ToList()));

        list.Add(new ParityScenario(
            "row_wrap_three_lines", 120, 300,
            FlexDirection.Row, FlexWrap.Wrap, JustifyContent.FlexStart,
            AlignItems.FlexStart, AlignContent.FlexStart, 10, 10,
            Enumerable.Repeat(new ItemSpec(50, 30), 6).ToList()));

        // --- Gaps, no wrap ---
        list.Add(new ParityScenario(
            "row_gaps_nowrap", 400, 100,
            FlexDirection.Row, FlexWrap.Nowrap, JustifyContent.FlexStart,
            AlignItems.FlexStart, AlignContent.FlexStart, 0, 12,
            Enumerable.Repeat(new ItemSpec(40, 20), 4).ToList()));

        list.Add(new ParityScenario(
            "column_gaps_nowrap", 100, 400,
            FlexDirection.Column, FlexWrap.Nowrap, JustifyContent.FlexStart,
            AlignItems.FlexStart, AlignContent.FlexStart, 15, 0,
            Enumerable.Repeat(new ItemSpec(40, 30), 4).ToList()));

        // --- Grow ---
        list.Add(new ParityScenario(
            "row_grow_even", 300, 60,
            FlexDirection.Row, FlexWrap.Nowrap, JustifyContent.FlexStart,
            AlignItems.FlexStart, AlignContent.FlexStart, 0, 0,
            new List<ItemSpec>
            {
                new(40, 20, FlexGrow: 1),
                new(40, 20, FlexGrow: 1),
                new(40, 20, FlexGrow: 1),
            }));

        list.Add(new ParityScenario(
            "row_grow_weighted", 300, 60,
            FlexDirection.Row, FlexWrap.Nowrap, JustifyContent.FlexStart,
            AlignItems.FlexStart, AlignContent.FlexStart, 0, 0,
            new List<ItemSpec>
            {
                new(30, 20, FlexGrow: 1),
                new(30, 20, FlexGrow: 2),
            }));

        // --- Shrink (constrained by min-width) ---
        list.Add(new ParityScenario(
            "row_shrink", 200, 60,
            FlexDirection.Row, FlexWrap.Nowrap, JustifyContent.FlexStart,
            AlignItems.FlexStart, AlignContent.FlexStart, 0, 0,
            new List<ItemSpec>
            {
                new(120, 20, FlexShrink: 1),
                new(120, 20, FlexShrink: 1),
            }));

        // --- Constraints (max-width caps grow) ---
        list.Add(new ParityScenario(
            "row_grow_max_width", 300, 60,
            FlexDirection.Row, FlexWrap.Nowrap, JustifyContent.FlexStart,
            AlignItems.FlexStart, AlignContent.FlexStart, 0, 0,
            new List<ItemSpec>
            {
                new(40, 20, FlexGrow: 1, MaxWidth: 60),
                new(40, 20, FlexGrow: 1),
            }));

        // --- Justify-content ---
        list.Add(new ParityScenario(
            "justify_center", 300, 60,
            FlexDirection.Row, FlexWrap.Nowrap, JustifyContent.Center,
            AlignItems.FlexStart, AlignContent.FlexStart, 0, 0,
            new List<ItemSpec> { new(50, 20), new(50, 20) }));

        list.Add(new ParityScenario(
            "justify_flex_end", 300, 60,
            FlexDirection.Row, FlexWrap.Nowrap, JustifyContent.FlexEnd,
            AlignItems.FlexStart, AlignContent.FlexStart, 0, 0,
            new List<ItemSpec> { new(50, 20), new(50, 20) }));

        list.Add(new ParityScenario(
            "justify_space_between", 300, 60,
            FlexDirection.Row, FlexWrap.Nowrap, JustifyContent.SpaceBetween,
            AlignItems.FlexStart, AlignContent.FlexStart, 0, 0,
            new List<ItemSpec> { new(40, 20), new(40, 20), new(40, 20) }));

        // --- Alignment (single line -> align-items) ---
        list.Add(new ParityScenario(
            "align_items_center", 300, 100,
            FlexDirection.Row, FlexWrap.Nowrap, JustifyContent.FlexStart,
            AlignItems.Center, AlignContent.FlexStart, 0, 10,
            new List<ItemSpec> { new(40, 20), new(40, 20) }));

        list.Add(new ParityScenario(
            "align_items_flex_end", 300, 100,
            FlexDirection.Row, FlexWrap.Nowrap, JustifyContent.FlexStart,
            AlignItems.FlexEnd, AlignContent.FlexStart, 0, 10,
            new List<ItemSpec> { new(40, 20), new(40, 20) }));

        list.Add(new ParityScenario(
            "column_align_items_center", 100, 300,
            FlexDirection.Column, FlexWrap.Nowrap, JustifyContent.FlexStart,
            AlignItems.Center, AlignContent.FlexStart, 10, 0,
            new List<ItemSpec> { new(40, 30), new(40, 30) }));

        // --- Display:none participation ---
        list.Add(new ParityScenario(
            "row_display_none", 300, 60,
            FlexDirection.Row, FlexWrap.Nowrap, JustifyContent.FlexStart,
            AlignItems.FlexStart, AlignContent.FlexStart, 0, 10,
            new List<ItemSpec>
            {
                new(50, 20),
                new(50, 20, DisplayNone: true),
                new(50, 20),
            }));

        return list;
    }
}
