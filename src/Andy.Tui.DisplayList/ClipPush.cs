namespace Andy.Tui.DisplayList;

public readonly record struct ClipPush(int X, int Y, int Width, int Height) : IDisplayOp;