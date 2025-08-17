namespace Andy.Tui.Backend.Terminal;

/// <summary>
/// Color mapping helpers adapted from v1 for 16/256/truecolor.
/// </summary>
public static class AnsiColorMapping
{
    public static int RgbTo256Color(byte r, byte g, byte b)
    {
        if (r == g && g == b)
        {
            if (r < 8) return 16;
            if (r > 248) return 231;
            return (int)System.Math.Round(((r - 8) / 247.0) * 24) + 232;
        }
        var ri = (int)System.Math.Round(r / 255.0 * 5);
        var gi = (int)System.Math.Round(g / 255.0 * 5);
        var bi = (int)System.Math.Round(b / 255.0 * 5);
        return 16 + (36 * ri) + (6 * gi) + bi;
    }

    public static int RgbTo16Color(byte r, byte g, byte b)
    {
        var brightness = (r + g + b) / 3;
        var isBright = brightness > 127;

        var max = System.Math.Max(r, System.Math.Max(g, b));
        var min = System.Math.Min(r, System.Math.Min(g, b));
        var delta = max - min;

        if (delta < 30)
        {
            if (brightness < 64) return 0; // Black
            if (brightness < 192) return 8; // Dark Gray
            if (brightness < 224) return 7; // Light Gray
            return 15; // White
        }

        int baseColor;
        if (r == max)
        {
            baseColor = g > b ? 3 : 1; // Yellow or Red
        }
        else if (g == max)
        {
            if (r > b) baseColor = 3; // Yellow
            else if (b > r) baseColor = 6; // Cyan
            else baseColor = 2; // Green
        }
        else
        {
            if (r > g) baseColor = 5; // Magenta
            else if (g > r) baseColor = 6; // Cyan
            else baseColor = 4; // Blue
        }

        return isBright ? baseColor + 8 : baseColor;
    }
}
