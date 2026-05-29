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
public readonly record struct Cell(string Grapheme, byte Width, Rgb24? Fg, Rgb24? Bg, CellAttrFlags Attrs);

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
