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
)
{
    /// <summary>
    /// URL of an OSC 8 hyperlink covering every cell in this run, or <c>null</c>
    /// for a plain run. The encoder wraps the run's text in an OSC 8 open/close
    /// pair when this is set and the terminal supports hyperlinks.
    /// </summary>
    public string? Hyperlink { get; init; }
}