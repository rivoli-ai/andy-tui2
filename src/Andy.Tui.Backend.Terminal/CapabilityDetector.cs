namespace Andy.Tui.Backend.Terminal;

public static class CapabilityDetector
{
    public static TerminalCapabilities DetectFromEnvironment()
    {
        var term = (System.Environment.GetEnvironmentVariable("TERM") ?? string.Empty).ToLowerInvariant();
        var colorterm = (System.Environment.GetEnvironmentVariable("COLORTERM") ?? string.Empty).ToLowerInvariant();
        bool trueColor = colorterm.Contains("truecolor") || colorterm.Contains("24bit") || term.Contains("direct") || term.Contains("24bit");
        bool pal256 = trueColor || term.Contains("256color") || term.Contains("xterm");
        // SU/SD (CSI S / CSI T) are ECMA-48 operations implemented by every
        // mainstream terminal emulator; only refuse them for an unknown or
        // explicitly "dumb" terminal where scroll semantics are unspecified.
        bool scrollRegion =
            term.Contains("xterm") || term.Contains("screen") || term.Contains("tmux") ||
            term.Contains("vt") || term.Contains("rxvt") || term.Contains("linux") ||
            term.Contains("ansi") || term.Contains("256color") || term.Contains("direct");
        return new TerminalCapabilities
        {
            TrueColor = trueColor,
            Palette256 = pal256,
            Hyperlinks = term.Contains("xterm"),
            Underline = UnderlineMode.Single,
            ScrollRegion = scrollRegion
        };
    }
}
