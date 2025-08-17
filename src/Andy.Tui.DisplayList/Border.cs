namespace Andy.Tui.DisplayList;

public readonly record struct Border(int X, int Y, int Width, int Height, string Style, Rgb24 Color) : IDisplayOp;