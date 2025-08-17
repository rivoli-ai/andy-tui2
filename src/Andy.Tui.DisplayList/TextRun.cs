namespace Andy.Tui.DisplayList;

public readonly record struct TextRun(int X, int Y, string Content, Rgb24 Fg, Rgb24? Bg, CellAttrFlags Attrs) : IDisplayOp;