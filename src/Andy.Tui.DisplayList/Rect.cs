namespace Andy.Tui.DisplayList;

public readonly record struct Rect(int X, int Y, int Width, int Height, Rgb24 Fill, Rgb24? Stroke = null) : IDisplayOp;