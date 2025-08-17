namespace Andy.Tui.DisplayList;

/// <summary>
/// 24-bit RGB color.
/// </summary>
public readonly record struct Rgb24(byte R, byte G, byte B)
{
    public static readonly Rgb24 Transparent = new(0, 0, 0);
}