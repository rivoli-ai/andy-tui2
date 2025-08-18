using Andy.Tui.DisplayList;

namespace Andy.Tui.Animations;

public enum Easing { Linear }

public static class Interpolators
{
    public static Rgb24 Lerp(Rgb24 a, Rgb24 b, double t)
    {
        byte LerpByte(byte x, byte y) => (byte)(x + (y - x) * t);
        return new Rgb24(LerpByte(a.R, b.R), LerpByte(a.G, b.G), LerpByte(a.B, b.B));
    }
}

public sealed record TransitionColor(Rgb24 From, Rgb24 To, int DurationMs, Easing Easing = Easing.Linear);

public static class TransitionParser
{
    // Very minimal parser: "color 200ms linear" -> TransitionColor with placeholders for from/to
    public static TransitionColor? TryParseColor(string s, Rgb24 from, Rgb24 to)
    {
        // tokens: property duration [easing]
        var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 2 && parts[0].Equals("color", StringComparison.OrdinalIgnoreCase))
        {
            if (parts[1].EndsWith("ms") && int.TryParse(parts[1][..^2], out var ms))
            {
                var easing = Easing.Linear;
                if (parts.Length >= 3 && parts[2].Equals("linear", StringComparison.OrdinalIgnoreCase)) easing = Easing.Linear;
                return new TransitionColor(from, to, ms, easing);
            }
        }
        return null;
    }
}
