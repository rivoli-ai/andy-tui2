using System;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Compositor;

/// <summary>
/// A single terminal cell. <paramref name="Fg"/> and <paramref name="Bg"/> are
/// nullable: <c>null</c> means "transparent" — the cell carries no explicit
/// color and the encoder emits the terminal default (<c>ESC[39m</c> for the
/// foreground, <c>ESC[49m</c> for the background) so the terminal's own colors
/// (including a transparent background) show through. An untouched cell
/// (<c>default(Cell)</c>) is fully transparent.
/// </summary>
/// <remarks>
/// <see cref="Hyperlink"/> carries the URL of an OSC 8 hyperlink that covers this
/// cell, or <c>null</c> for a plain cell. Because the link is stored per-cell it
/// is clipped together with the glyphs it decorates, so the encoder can wrap each
/// surviving run in a properly terminated sequence — a clip can never split a
/// hyperlink control sequence.
/// </remarks>
public readonly record struct Cell(string Grapheme, byte Width, Rgb24? Fg, Rgb24? Bg, CellAttrFlags Attrs)
{
    /// <summary>URL of an OSC 8 hyperlink covering this cell, or <c>null</c> if none.</summary>
    public string? Hyperlink { get; init; }
}

public sealed class CellGrid
{
    private readonly Cell[] _cells;
    public int Width { get; }
    public int Height { get; }

    public CellGrid(int width, int height)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException();
        Width = width;
        Height = height;
        _cells = new Cell[width * height];
    }

    public ref Cell GetRef(int x, int y) => ref _cells[(y * Width) + x];
    public Cell this[int x, int y]
    {
        get => _cells[(y * Width) + x];
        set => _cells[(y * Width) + x] = value;
    }
}
