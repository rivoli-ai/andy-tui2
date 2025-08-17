namespace Andy.Tui.Style;

/// <summary>
/// Represents a value that can either be a concrete <see cref="Length"/> or Auto.
/// </summary>
public readonly record struct LengthOrAuto(Length? Value)
{
    public bool IsAuto => Value is null;
    public static LengthOrAuto Auto() => new(null);
    public static LengthOrAuto FromPixels(double px) => new(new Length(px));
    public static LengthOrAuto FromPercent(double percent) => new(Length.FromPercent(percent));
}