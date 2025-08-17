namespace Andy.Tui.Style;

/// <summary>
/// Simple RGBA color.
/// </summary>
public readonly record struct RgbaColor(byte R, byte G, byte B, byte A)
{
    public static RgbaColor FromRgb(byte r, byte g, byte b) => new(r, g, b, 255);
}