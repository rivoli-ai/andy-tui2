using System;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Compositor;

public readonly record struct Cell(string Grapheme, byte Width, Rgb24 Fg, Rgb24 Bg, CellAttrFlags Attrs);

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
