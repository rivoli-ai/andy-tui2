namespace Andy.Tui.Widgets.Tests;

public class ChatReproTests
{
    [Fact]
    public void Chat_Send_Should_Not_Eat_Trailing_Spaces_Repro()
    {
        // Reproduces the chat example behavior where input.Trim() eats spaces
        string input = "hello  "; // two trailing spaces intended
        // Simulate Program.cs chat loop behavior (current implementation)
        string sent = Andy.Tui.Widgets.ChatInputSanitizer.SanitizeForSend(input);
        // Expectation: trailing spaces should be preserved by chat send logic
        Assert.Equal("hello  ", sent);
    }

    [Fact]
    public void Chat_Send_Should_Not_Eat_Leading_Spaces_Repro()
    {
        string input = "  indented";
        string sent = Andy.Tui.Widgets.ChatInputSanitizer.SanitizeForSend(input);
        Assert.Equal("  indented", sent);
    }
}
