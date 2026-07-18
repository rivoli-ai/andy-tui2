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
/// <c>Andy.Tui.Text.TerminalText</c> for the trust boundary contract.
/// </para>
/// </summary>
/// <remarks>
/// <see cref="Hyperlink"/> is optional structured metadata for an OSC 8 terminal
/// hyperlink covering this run's cells. It is <c>null</c> when the run is not a
/// link. The URL is <b>never</b> embedded in <see cref="Content"/>: control
/// sequences must not consume layout cells, and encoding them out-of-band lets
/// the encoder gate output on terminal capabilities and guarantees the sequence
/// is always terminated even when the run is clipped.
/// </remarks>
public readonly record struct TextRun(int X, int Y, string Content, Rgb24? Fg, Rgb24? Bg, CellAttrFlags Attrs) : IDisplayOp
{
    /// <summary>
    /// URL for an OSC 8 hyperlink spanning this run's cells, or <c>null</c> for a
    /// plain (non-link) run. Emitted only when the terminal advertises hyperlink
    /// support; otherwise the run renders as plain text.
    /// </summary>
    public string? Hyperlink { get; init; }
}