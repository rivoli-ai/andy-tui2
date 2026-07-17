using System.Text;
using Andy.Tui.Backend.Terminal;
using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Rendering.Tests;

public static class VirtualScreenOracle
{
    public static CellGrid Decode(ReadOnlySpan<byte> bytes, (int Width, int Height) viewport)
        => Decode(bytes, viewport, initial: null);

    /// <summary>
    /// Interprets an encoded frame against a virtual screen. When
    /// <paramref name="initial"/> is provided the screen starts from that frame
    /// (modelling incremental output on top of what is already displayed);
    /// otherwise it starts blank. Understands cursor moves, SGR, printable text
    /// and the SU/SD scroll operations (CSI S / CSI T).
    /// </summary>
    public static CellGrid Decode(ReadOnlySpan<byte> bytes, (int Width, int Height) viewport, CellGrid? initial)
    {
        var grid = new CellGrid(viewport.Width, viewport.Height);
        if (initial is not null)
        {
            for (int y = 0; y < viewport.Height; y++)
                for (int x = 0; x < viewport.Width; x++)
                    grid[x, y] = initial[x, y];
        }
        int row = 0, col = 0; // 0-based
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
                                row = Math.Clamp(r - 1, 0, viewport.Height - 1);
                                col = Math.Clamp(c - 1, 0, viewport.Width - 1);
                            }
                            break;
                        case 'm':
                            // Parse SGR: sequences separated by ;
                            var codes = param.ToString().Split(';', StringSplitOptions.RemoveEmptyEntries);
                            if (codes.Length == 0) { currentFg = new Rgb24(255, 255, 255); currentBg = new Rgb24(0, 0, 0); currentAttrs = CellAttrFlags.None; break; }
                            int idx = 0;
                            while (idx < codes.Length)
                            {
                                if (!int.TryParse(codes[idx], out int code)) { idx++; continue; }
                                switch (code)
                                {
                                    case 0: currentFg = new Rgb24(255, 255, 255); currentBg = new Rgb24(0, 0, 0); currentAttrs = CellAttrFlags.None; idx++; break;
                                    case 1: currentAttrs |= CellAttrFlags.Bold; idx++; break;
                                    case 3: currentAttrs |= CellAttrFlags.Italic; idx++; break;
                                    case 4: currentAttrs |= CellAttrFlags.Underline; idx++; break;
                                    case 9: currentAttrs |= CellAttrFlags.Strikethrough; idx++; break;
                                    case 2: currentAttrs |= CellAttrFlags.Dim; idx++; break;
                                    case 5: currentAttrs |= CellAttrFlags.Blink; idx++; break;
                                    case 7: currentAttrs |= CellAttrFlags.Reverse; idx++; break;
                                    case 39: currentFg = new Rgb24(255, 255, 255); idx++; break; // default foreground
                                    case 49: currentBg = new Rgb24(0, 0, 0); idx++; break;       // default background
                                    case 38: // foreground
                                        if (idx + 1 < codes.Length && codes[idx + 1] == "2" && idx + 4 < codes.Length)
                                        {
                                            // 38;2;r;g;b
                                            var r8 = byte.Parse(codes[idx + 2]);
                                            var g8 = byte.Parse(codes[idx + 3]);
                                            var b8 = byte.Parse(codes[idx + 4]);
                                            currentFg = new Rgb24(r8, g8, b8);
                                            idx += 5;
                                        }
                                        else if (idx + 1 < codes.Length && codes[idx + 1] == "5" && idx + 2 < codes.Length)
                                        {
                                            // 38;5;n — approximate to rgb cube for oracle parity: not needed for cell FG equality
                                            idx += 3;
                                        }
                                        else { idx++; }
                                        break;
                                    case 48: // background
                                        if (idx + 1 < codes.Length && codes[idx + 1] == "2" && idx + 4 < codes.Length)
                                        {
                                            var r8 = byte.Parse(codes[idx + 2]);
                                            var g8 = byte.Parse(codes[idx + 3]);
                                            var b8 = byte.Parse(codes[idx + 4]);
                                            currentBg = new Rgb24(r8, g8, b8);
                                            idx += 5;
                                        }
                                        else if (idx + 1 < codes.Length && codes[idx + 1] == "5" && idx + 2 < codes.Length)
                                        {
                                            idx += 3;
                                        }
                                        else { idx++; }
                                        break;
                                    default:
                                        // Basic 16 colors — ignore for now
                                        idx++;
                                        break;
                                }
                            }
                            break;
                        case 'S': // SU — scroll up: content moves up, blank rows at the bottom
                            {
                                int n = ParseCount(param.ToString());
                                ScrollUp(grid, viewport, n);
                            }
                            break;
                        case 'T': // SD — scroll down: content moves down, blank rows at the top
                            {
                                int n = ParseCount(param.ToString());
                                ScrollDown(grid, viewport, n);
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            else
            {
                var ch = Encoding.UTF8.GetString(bytes.Slice(i, 1));
                if (row >= 0 && row < viewport.Height && col >= 0 && col < viewport.Width)
                {
                    grid[col, row] = new Cell(ch, 1, currentFg, currentBg, currentAttrs);
                }
                col++;
                i++;
            }
        }
        return grid;
    }

    private static int ParseCount(string param)
    {
        return int.TryParse(param, out var n) && n > 0 ? n : 1;
    }

    private static void ScrollDown(CellGrid grid, (int Width, int Height) vp, int n)
    {
        n = Math.Min(n, vp.Height);
        for (int y = vp.Height - 1; y >= n; y--)
            for (int x = 0; x < vp.Width; x++)
                grid[x, y] = grid[x, y - n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < vp.Width; x++)
                grid[x, y] = default;
    }

    private static void ScrollUp(CellGrid grid, (int Width, int Height) vp, int n)
    {
        n = Math.Min(n, vp.Height);
        for (int y = 0; y < vp.Height - n; y++)
            for (int x = 0; x < vp.Width; x++)
                grid[x, y] = grid[x, y + n];
        for (int y = vp.Height - n; y < vp.Height; y++)
            for (int x = 0; x < vp.Width; x++)
                grid[x, y] = default;
    }
}
