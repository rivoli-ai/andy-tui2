namespace Andy.Tui.DisplayList;

/// <summary>
/// 24-bit RGB color. This type is always opaque.
/// </summary>
/// <remarks>
/// Transparency is not a color value. To represent "no background" (let the
/// terminal's own background / transparency show through), use a <c>null</c>
/// <see cref="Rgb24"/>? — e.g. <see cref="TextRun.Bg"/>, <see cref="Rect.Fill"/>,
/// or a composited cell's background. A <c>null</c> background is emitted as the
/// ANSI "default background" (<c>ESC[49m</c>) rather than an explicit RGB value.
/// </remarks>
public readonly record struct Rgb24(byte R, byte G, byte B);