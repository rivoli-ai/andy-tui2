using System.Text;
using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Rendering.Tests;

/// <summary>
/// A stateful terminal oracle. Unlike <see cref="VirtualScreenOracle"/> which decodes a single
/// self-contained frame, this oracle keeps a persistent cell grid and applies successive encoded
/// frames on top of it, exactly like a real terminal emulator. This lets adversarial tests verify
/// that incremental (damage-only) rendering, applied frame after frame, converges to the same
/// visible state as a full repaint.
///
/// The oracle understands the subset of ANSI that <see cref="Andy.Tui.Backend.Terminal.AnsiEncoder"/>
/// emits: CUP (ESC[row;colH), SGR (ESC[...m) including truecolor 38/48;2;r;g;b, and printable text.
/// Cells that a frame does not touch retain their previous value, so a dirty-only frame that only
/// rewrites changed runs leaves the rest of the screen intact.
/// </summary>
public sealed class StatefulTerminalOracle
{
    private readonly int _width;
    private readonly int _height;
    private CellGrid _grid;

    // Cursor is persisted across frames like a real terminal.
    private int _row;
    private int _col;

    public StatefulTerminalOracle(int width, int height)
    {
        _width = width;
        _height = height;
        _grid = new CellGrid(width, height);
    }

    public int Width => _width;
    public int Height => _height;

    /// <summary>A snapshot copy of the current visible grid.</summary>
    public CellGrid Snapshot()
    {
        var copy = new CellGrid(_width, _height);
        for (int y = 0; y < _height; y++)
            for (int x = 0; x < _width; x++)
                copy[x, y] = _grid[x, y];
        return copy;
    }

    /// <summary>Applies one encoded frame's bytes on top of the persistent grid.</summary>
    public void ApplyFrame(ReadOnlySpan<byte> bytes)
    {
        var currentFg = new Rgb24(255, 255, 255);
        var currentBg = new Rgb24(0, 0, 0);
        var currentAttrs = CellAttrFlags.None;
        int i = 0;
        while (i < bytes.Length)
        {
            byte b = bytes[i];
            if (b == 0x1B) // ESC
            {
                i++;
                if (i < bytes.Length && bytes[i] == (byte)'[')
                {
                    i++;
                    var param = new StringBuilder();
                    while (i < bytes.Length && (bytes[i] < (byte)'@' || bytes[i] > (byte)'~'))
                    {
                        param.Append((char)bytes[i]);
                        i++;
                    }
                    if (i >= bytes.Length) break;
                    char final = (char)bytes[i++];
                    switch (final)
                    {
                        case 'H':
                        case 'f':
                            var parts = param.ToString().Split(';');
                            if (parts.Length >= 2 && int.TryParse(parts[0], out var r) && int.TryParse(parts[1], out var c))
                            {
                                _row = Math.Clamp(r - 1, 0, _height - 1);
                                _col = Math.Clamp(c - 1, 0, _width - 1);
                            }
                            break;
                        case 'm':
                            ApplySgr(param.ToString(), ref currentFg, ref currentBg, ref currentAttrs);
                            break;
                    }
                }
            }
            else
            {
                // Decode a whole UTF-8 sequence (not one byte at a time): a multibyte
                // grapheme such as an accented letter or emoji occupies several bytes and
                // must be reassembled into a single cell, otherwise each continuation byte
                // decodes in isolation to U+FFFD and corrupts the visible text.
                int len = Utf8SequenceLength(b);
                if (i + len > bytes.Length) len = bytes.Length - i; // truncated tail: take what's left
                var ch = Encoding.UTF8.GetString(bytes.Slice(i, len));
                if (_row >= 0 && _row < _height && _col >= 0 && _col < _width)
                {
                    _grid[_col, _row] = new Cell(ch, 1, currentFg, currentBg, currentAttrs);
                }
                _col++;
                i += len;
            }
        }
    }

    // Number of bytes in the UTF-8 sequence introduced by leading byte <paramref name="lead"/>.
    // A stray continuation byte (0x80-0xBF) or otherwise invalid lead is treated as a single byte.
    private static int Utf8SequenceLength(byte lead)
    {
        if (lead < 0x80) return 1;
        if (lead >= 0xC0 && lead < 0xE0) return 2;
        if (lead >= 0xE0 && lead < 0xF0) return 3;
        if (lead >= 0xF0 && lead < 0xF8) return 4;
        return 1;
    }

    private static void ApplySgr(string param, ref Rgb24 fg, ref Rgb24 bg, ref CellAttrFlags attrs)
    {
        var codes = param.Split(';', StringSplitOptions.RemoveEmptyEntries);
        if (codes.Length == 0)
        {
            fg = new Rgb24(255, 255, 255);
            bg = new Rgb24(0, 0, 0);
            attrs = CellAttrFlags.None;
            return;
        }
        int idx = 0;
        while (idx < codes.Length)
        {
            if (!int.TryParse(codes[idx], out int code)) { idx++; continue; }
            switch (code)
            {
                case 0: fg = new Rgb24(255, 255, 255); bg = new Rgb24(0, 0, 0); attrs = CellAttrFlags.None; idx++; break;
                case 1: attrs |= CellAttrFlags.Bold; idx++; break;
                case 2: attrs |= CellAttrFlags.Dim; idx++; break;
                case 3: attrs |= CellAttrFlags.Italic; idx++; break;
                case 4: attrs |= CellAttrFlags.Underline; idx++; break;
                case 5: attrs |= CellAttrFlags.Blink; idx++; break;
                case 7: attrs |= CellAttrFlags.Reverse; idx++; break;
                case 9: attrs |= CellAttrFlags.Strikethrough; idx++; break;
                case 38:
                    if (idx + 4 < codes.Length && codes[idx + 1] == "2")
                    {
                        fg = new Rgb24(byte.Parse(codes[idx + 2]), byte.Parse(codes[idx + 3]), byte.Parse(codes[idx + 4]));
                        idx += 5;
                    }
                    else if (idx + 2 < codes.Length && codes[idx + 1] == "5") { idx += 3; }
                    else { idx++; }
                    break;
                case 48:
                    if (idx + 4 < codes.Length && codes[idx + 1] == "2")
                    {
                        bg = new Rgb24(byte.Parse(codes[idx + 2]), byte.Parse(codes[idx + 3]), byte.Parse(codes[idx + 4]));
                        idx += 5;
                    }
                    else if (idx + 2 < codes.Length && codes[idx + 1] == "5") { idx += 3; }
                    else { idx++; }
                    break;
                default: idx++; break;
            }
        }
    }

    /// <summary>Renders a human-readable text dump of the grid, one row per line, for diagnostics.</summary>
    public static string Dump(CellGrid grid)
    {
        var sb = new StringBuilder();
        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                var g = grid[x, y].Grapheme;
                sb.Append(string.IsNullOrEmpty(g) ? ' ' : g[0]);
            }
            sb.Append('\n');
        }
        return sb.ToString();
    }

    /// <summary>
    /// Produces a per-cell diff between two grids: coordinates whose grapheme/color/attrs differ.
    /// Returns an empty string when the grids are visibly identical.
    /// </summary>
    public static string Diff(CellGrid expected, CellGrid actual)
    {
        var sb = new StringBuilder();
        int w = Math.Min(expected.Width, actual.Width);
        int h = Math.Min(expected.Height, actual.Height);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                var e = expected[x, y];
                var a = actual[x, y];
                if (!CellsVisiblyEqual(e, a))
                {
                    sb.Append($"({x},{y}) expected [{Describe(e)}] actual [{Describe(a)}]\n");
                }
            }
        }
        return sb.ToString();
    }

    public static bool CellsVisiblyEqual(Cell a, Cell b)
    {
        string ga = a.Grapheme ?? "";
        string gb = b.Grapheme ?? "";
        // Treat empty and single-space as equivalent blanks.
        if (ga.Length == 0) ga = " ";
        if (gb.Length == 0) gb = " ";
        return ga == gb && a.Fg == b.Fg && a.Bg == b.Bg && a.Attrs == b.Attrs;
    }

    private static string Describe(Cell c)
    {
        string g = string.IsNullOrEmpty(c.Grapheme) ? " " : c.Grapheme;
        return $"'{g}' fg={c.Fg} bg={c.Bg} attrs={c.Attrs}";
    }
}
