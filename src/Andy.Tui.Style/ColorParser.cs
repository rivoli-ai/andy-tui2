namespace Andy.Tui.Style;

/// <summary>
/// Tiny color parser for named colors and hex literals.
/// </summary>
internal static class ColorParser
{
    private static readonly Dictionary<string, RgbaColor> Named = new(StringComparer.OrdinalIgnoreCase)
    {
        ["black"] = RgbaColor.FromRgb(0, 0, 0),
        ["white"] = RgbaColor.FromRgb(255, 255, 255),
        ["red"] = RgbaColor.FromRgb(255, 0, 0),
        ["green"] = RgbaColor.FromRgb(0, 128, 0),
        ["blue"] = RgbaColor.FromRgb(0, 0, 255),
        ["yellow"] = RgbaColor.FromRgb(255, 255, 0),
        ["magenta"] = RgbaColor.FromRgb(255, 0, 255),
        ["cyan"] = RgbaColor.FromRgb(0, 255, 255),
        ["gray"] = RgbaColor.FromRgb(128, 128, 128),
        ["grey"] = RgbaColor.FromRgb(128, 128, 128),
        ["silver"] = RgbaColor.FromRgb(192, 192, 192),
        ["maroon"] = RgbaColor.FromRgb(128, 0, 0),
        ["olive"] = RgbaColor.FromRgb(128, 128, 0),
        ["lime"] = RgbaColor.FromRgb(0, 255, 0),
        ["teal"] = RgbaColor.FromRgb(0, 128, 128),
        ["navy"] = RgbaColor.FromRgb(0, 0, 128),
        ["purple"] = RgbaColor.FromRgb(128, 0, 128),
        ["orange"] = RgbaColor.FromRgb(255, 165, 0),
        ["brown"] = RgbaColor.FromRgb(165, 42, 42),
        ["pink"] = RgbaColor.FromRgb(255, 192, 203),
        ["aqua"] = RgbaColor.FromRgb(0, 255, 255),
        ["fuchsia"] = RgbaColor.FromRgb(255, 0, 255)
    };

    public static bool TryParse(string value, out RgbaColor color)
    {
        value = value.Trim();
        if (value.Length == 0) { color = default; return false; }
        if (value[0] == '#')
        {
            return TryParseHex(value, out color);
        }
        if (Named.TryGetValue(value, out color)) return true;
        if (value.StartsWith("rgb(", StringComparison.OrdinalIgnoreCase) && TryParseRgb(value, out color)) return true;
        if (value.StartsWith("rgba(", StringComparison.OrdinalIgnoreCase) && TryParseRgba(value, out color)) return true;
        color = default;
        return false;
    }

    private static bool TryParseHex(string hex, out RgbaColor color)
    {
        string h = hex[1..];
        if (h.Length == 3)
        {
            // #rgb
            if (TryHex(h[0], out var r) && TryHex(h[1], out var g) && TryHex(h[2], out var b))
            {
                color = RgbaColor.FromRgb((byte)(r * 17), (byte)(g * 17), (byte)(b * 17));
                return true;
            }
        }
        else if (h.Length == 6)
        {
            if (byte.TryParse(h[..2], System.Globalization.NumberStyles.HexNumber, null, out var r) &&
                byte.TryParse(h.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out var g) &&
                byte.TryParse(h.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
            {
                color = RgbaColor.FromRgb(r, g, b);
                return true;
            }
        }
        color = default;
        return false;
    }

    private static bool TryHex(char c, out int v)
    {
        if (c >= '0' && c <= '9') { v = c - '0'; return true; }
        if (c >= 'a' && c <= 'f') { v = 10 + c - 'a'; return true; }
        if (c >= 'A' && c <= 'F') { v = 10 + c - 'A'; return true; }
        v = 0; return false;
    }

    private static bool TryParseRgb(string s, out RgbaColor color)
    {
        // rgb(r,g,b) with integers 0-255
        color = default;
        var inner = s.AsSpan(4, s.Length - 5).Trim();
        var parts = inner.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3) return false;
        if (byte.TryParse(parts[0].Trim(), out var r) && byte.TryParse(parts[1].Trim(), out var g) && byte.TryParse(parts[2].Trim(), out var b))
        {
            color = RgbaColor.FromRgb(r, g, b);
            return true;
        }
        return false;
    }

    private static bool TryParseRgba(string s, out RgbaColor color)
    {
        // rgba(r,g,b,a) where a is 0..1
        color = default;
        var inner = s.AsSpan(5, s.Length - 6).Trim();
        var parts = inner.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4) return false;
        if (byte.TryParse(parts[0].Trim(), out var r) && byte.TryParse(parts[1].Trim(), out var g) && byte.TryParse(parts[2].Trim(), out var b) && double.TryParse(parts[3].Trim(), out var a))
        {
            if (a < 0) a = 0; if (a > 1) a = 1;
            color = new RgbaColor(r, g, b, (byte)Math.Round(a * 255));
            return true;
        }
        return false;
    }
}
