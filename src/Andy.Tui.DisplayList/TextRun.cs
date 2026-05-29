namespace Andy.Tui.DisplayList;

/// <summary>
/// A run of text. <paramref name="Fg"/> and <paramref name="Bg"/> are nullable:
/// <c>null</c> means "transparent" — use the terminal's default foreground /
/// background (emitted as <c>ESC[39m</c> / <c>ESC[49m</c>) rather than an explicit
/// color. A null <paramref name="Bg"/> keeps whatever is already under the glyph.
/// </summary>
public readonly record struct TextRun(int X, int Y, string Content, Rgb24? Fg, Rgb24? Bg, CellAttrFlags Attrs) : IDisplayOp;