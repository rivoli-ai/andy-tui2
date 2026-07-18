namespace Andy.Tui.Style;

/// <summary>
/// Holds the computed style after cascade for the supported subset.
/// </summary>
public readonly record struct ResolvedStyle(
    Display Display,
    FlexDirection FlexDirection,
    FlexWrap FlexWrap,
    JustifyContent JustifyContent,
    AlignItems AlignItems,
    AlignSelf AlignSelf,
    AlignContent AlignContent,
    int Order,
    double FlexGrow,
    double FlexShrink,
    LengthOrAuto FlexBasis,
    LengthOrAuto Width,
    LengthOrAuto Height,
    LengthOrAuto MinWidth,
    LengthOrAuto MinHeight,
    LengthOrAuto MaxWidth,
    LengthOrAuto MaxHeight,
    Thickness Padding,
    Thickness Margin,
    Length RowGap,
    Length ColumnGap,
    Overflow Overflow,
    RgbaColor Color,
    RgbaColor BackgroundColor,
    FontWeight FontWeight,
    FontStyle FontStyle,
    TextDecoration TextDecoration)
{
    public static ResolvedStyle Default => new(
        Display: Display.Flex,
        FlexDirection: FlexDirection.Row,
        FlexWrap: FlexWrap.Nowrap,
        JustifyContent: JustifyContent.FlexStart,
        AlignItems: AlignItems.Stretch,
        AlignSelf: AlignSelf.Auto,
        AlignContent: AlignContent.Stretch,
        Order: 0,
        FlexGrow: 0,
        FlexShrink: 1,
        FlexBasis: LengthOrAuto.Auto(),
        Width: LengthOrAuto.Auto(),
        Height: LengthOrAuto.Auto(),
        MinWidth: LengthOrAuto.Auto(),
        MinHeight: LengthOrAuto.Auto(),
        MaxWidth: LengthOrAuto.Auto(),
        MaxHeight: LengthOrAuto.Auto(),
        Padding: Thickness.Zero,
        Margin: Thickness.Zero,
        RowGap: Length.Zero,
        ColumnGap: Length.Zero,
        Overflow: Overflow.Visible,
        // Colors default to transparent (the terminal default) so that a node with no
        // explicit color/background-color rule resolves to a transparent value. IStyleable
        // widgets treat transparent as "unset" and keep their theme-seeded palette instead
        // of being clobbered by an opaque default. See IStyleable.ApplyStyle.
        Color: RgbaColor.Transparent,
        BackgroundColor: RgbaColor.Transparent,
        FontWeight: FontWeight.Normal,
        FontStyle: FontStyle.Normal,
        TextDecoration: TextDecoration.None
    );
}