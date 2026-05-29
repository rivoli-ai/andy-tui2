namespace Andy.Tui.DisplayList;

/// <summary>
/// A filled rectangle. <paramref name="Fill"/> is nullable: <c>null</c> means a
/// transparent fill — the rectangle paints no background, leaving whatever is
/// underneath (ultimately the terminal's default background) visible.
/// </summary>
public readonly record struct Rect(int X, int Y, int Width, int Height, Rgb24? Fill, Rgb24? Stroke = null) : IDisplayOp;