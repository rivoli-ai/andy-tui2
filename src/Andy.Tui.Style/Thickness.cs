namespace Andy.Tui.Style;

/// <summary>
/// Edge thickness values for margins and paddings.
/// </summary>
public readonly record struct Thickness(Length Left, Length Top, Length Right, Length Bottom)
{
    public static Thickness Zero => new(Length.Zero, Length.Zero, Length.Zero, Length.Zero);
}