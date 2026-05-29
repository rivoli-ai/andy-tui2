using Andy.Tui.DisplayList;

namespace Andy.Tui.Style;

/// <summary>
/// Simple RGBA color. The alpha channel is binary at render time: a fully
/// transparent color (<c>A == 0</c>) maps to "no color" (the terminal default),
/// while any other alpha is treated as opaque. (Terminals cannot blend partial
/// alpha, so intermediate values are not composited.)
/// </summary>
public readonly record struct RgbaColor(byte R, byte G, byte B, byte A)
{
    public static RgbaColor FromRgb(byte r, byte g, byte b) => new(r, g, b, 255);

    /// <summary>A fully transparent color (alpha 0) — renders as the terminal default.</summary>
    public static readonly RgbaColor Transparent = new(0, 0, 0, 0);

    /// <summary>True when this color is fully transparent (alpha 0).</summary>
    public bool IsTransparent => A == 0;

    /// <summary>
    /// Convert to a render-layer color. Returns <c>null</c> when the color is
    /// transparent, which the compositor/encoder treat as "use the terminal
    /// default" (<c>ESC[39m</c> / <c>ESC[49m</c>). Otherwise returns the opaque RGB.
    /// </summary>
    public Rgb24? ToRgb24() => IsTransparent ? (Rgb24?)null : new Rgb24(R, G, B);
}
