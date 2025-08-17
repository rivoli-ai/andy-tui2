namespace Andy.Tui.Style;

/// <summary>
/// Represents a CSS length as pixels or percentage.
/// </summary>
public readonly record struct Length
{
    /// <summary>
    /// Pixel value when not a percentage. Zero when percentage.
    /// </summary>
    public double Pixels { get; }

    /// <summary>
    /// Percentage value (0..100) when percentage-based; null otherwise.
    /// </summary>
    public double? Percentage { get; }

    public bool IsPercent => Percentage.HasValue;

    public Length(double pixels)
    {
        Pixels = pixels;
        Percentage = null;
    }

    private Length(double pixels, double? percentage)
    {
        Pixels = pixels;
        Percentage = percentage;
    }

    public static Length Zero => new(0);

    public static Length FromPixels(double px) => new(px);
    public static Length FromPercent(double percent) => new(0, percent);

    public double Resolve(double reference)
        => IsPercent ? reference * (Percentage!.Value / 100.0) : Pixels;
}