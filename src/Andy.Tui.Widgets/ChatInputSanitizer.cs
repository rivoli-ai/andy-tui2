namespace Andy.Tui.Widgets;

public static class ChatInputSanitizer
{
    // Preserve leading/trailing spaces; only strip CR/LF that come from Enter handling
    public static string SanitizeForSend(string input)
        => input.Trim('\r', '\n');
}
