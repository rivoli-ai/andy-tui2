using Andy.Tui.DisplayList;

namespace Andy.Tui.Compositor;

public readonly record struct RowRun(
    int Row,
    int ColStart,
    int ColEnd,
    CellAttrFlags Attrs,
    Rgb24 Fg,
    Rgb24 Bg,
    string Text
);