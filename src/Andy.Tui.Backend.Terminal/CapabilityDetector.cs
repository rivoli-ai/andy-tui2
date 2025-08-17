namespace Andy.Tui.Backend.Terminal;

public static class CapabilityDetector
{
    public static TerminalCapabilities DetectFromEnvironment()
    {
        var term = (System.Environment.GetEnvironmentVariable("TERM") ?? string.Empty).ToLowerInvariant();
        var colorterm = (System.Environment.GetEnvironmentVariable("COLORTERM") ?? string.Empty).ToLowerInvariant();
        bool trueColor = colorterm.Contains("truecolor") || colorterm.Contains("24bit") || term.Contains("direct") || term.Contains("24bit");
        bool pal256 = trueColor || term.Contains("256color") || term.Contains("xterm");
        return new TerminalCapabilities
        {
            TrueColor = trueColor,
            Palette256 = pal256,
            Hyperlinks = term.Contains("xterm"),
            Underline = UnderlineMode.Single
        };
    }
}
