using Andy.Tui.DisplayList;

namespace Andy.Tui.Compositor;

public readonly record struct RowRun(
    int Row,
    int ColStart,
    int ColEnd,
    CellAttrFlags Attrs,
    // null = transparent: encoder emits the terminal default foreground (ESC[39m).
    Rgb24? Fg,
    // null = transparent: encoder emits the terminal default background (ESC[49m).
    Rgb24? Bg,
    string Text
);