namespace Andy.Tui.DisplayList;

/// <summary>
/// A run of text. <paramref name="Fg"/> and <paramref name="Bg"/> are nullable:
/// <c>null</c> means "transparent" — use the terminal's default foreground /
/// background (emitted as <c>ESC[39m</c> / <c>ESC[49m</c>) rather than an explicit
/// color. A null <paramref name="Bg"/> keeps whatever is already under the glyph.
///
/// <para>
/// <paramref name="Content"/> is <b>plain display text</b>: it is treated as
/// untrusted and any terminal control characters it contains are rewritten to
/// visible, inert placeholders before reaching the terminal — they are never
/// executed. Trusted terminal control is expressed through the typed fields
/// (coordinates, <paramref name="Fg"/>/<paramref name="Bg"/>/<paramref name="Attrs"/>),
/// not by embedding escape sequences in <paramref name="Content"/>. See
/// <see cref="TerminalText"/> for the trust boundary contract.
/// </para>
/// </summary>
public readonly record struct TextRun(int X, int Y, string Content, Rgb24? Fg, Rgb24? Bg, CellAttrFlags Attrs) : IDisplayOp;