using Andy.Tui.DisplayList;
using Andy.Tui.Compositor;

namespace Andy.Tui.Rendering.Tests;

/// <summary>
/// Tests to ensure wrapped text properly clears previous content and doesn't leave artifacts
/// </summary>
public class WrappedTextCleanupTests
{
    [Fact]
    public void WrappedText_WithIndent_ShouldNotLeaveLeftMarginArtifacts()
    {
        // Reproduce the bug where "genera" is left on the left margin
        // This happens when text is drawn at an indent but previous content at x=0 isn't cleared

        // Frame 1: Draw long text starting at column 0
        var b1 = new DisplayListBuilder();
        b1.PushClip(new ClipPush(0, 0, 80, 3));
        b1.DrawRect(new Rect(0, 0, 80, 3, new Rgb24(0, 0, 0)));
        b1.DrawText(new TextRun(0, 0, "generative", new Rgb24(200, 200, 200), null, CellAttrFlags.None));
        b1.Pop();
        var grid1 = new TtyCompositor().Composite(b1.Build(), (80, 3));

        // Verify frame 1 has "generative" at column 0
        Assert.Equal("g", grid1[0, 0].Grapheme);

        // Frame 2: Now draw text at indent=8, simulating scrolled comment content
        // This should clear columns 0-7 but currently doesn't
        var b2 = new DisplayListBuilder();
        b2.PushClip(new ClipPush(0, 0, 80, 3));
        b2.DrawRect(new Rect(0, 0, 80, 3, new Rgb24(0, 0, 0))); // Background clear
        b2.DrawText(new TextRun(8, 0, "This is a comment", new Rgb24(200, 200, 200), null, CellAttrFlags.None));
        b2.Pop();
        var grid2 = new TtyCompositor().Composite(b2.Build(), (80, 3));

        // Check that columns 0-7 are cleared (space with black background)
        // The bug is that "genera" would still be visible at columns 0-6
        for (int x = 0; x < 8; x++)
        {
            Assert.True(
                grid2[x, 0].Grapheme == null || grid2[x, 0].Grapheme == " ",
                $"Column {x} should be cleared but has grapheme '{grid2[x, 0].Grapheme}'"
            );
        }

        // Check that the actual text starts at column 8
        Assert.Equal("T", grid2[8, 0].Grapheme);
    }

    [Fact]
    public void WrappedText_WithDifferentLengths_ShouldClearEntireLine()
    {
        // Test that a shorter line properly clears leftover content from a longer previous line

        // Frame 1: Long line
        var b1 = new DisplayListBuilder();
        b1.PushClip(new ClipPush(0, 0, 80, 2));
        b1.DrawRect(new Rect(0, 0, 80, 2, new Rgb24(0, 0, 0)));
        b1.DrawText(new TextRun(0, 0, "This is a very long line that takes up most of the width", new Rgb24(200, 200, 200), null, CellAttrFlags.None));
        b1.Pop();
        var grid1 = new TtyCompositor().Composite(b1.Build(), (80, 2));

        // Frame 2: Short line in same position
        var b2 = new DisplayListBuilder();
        b2.PushClip(new ClipPush(0, 0, 80, 2));
        b2.DrawRect(new Rect(0, 0, 80, 2, new Rgb24(0, 0, 0)));
        b2.DrawText(new TextRun(0, 0, "Short", new Rgb24(200, 200, 200), null, CellAttrFlags.None));
        b2.Pop();
        var grid2 = new TtyCompositor().Composite(b2.Build(), (80, 2));

        // Verify "Short" is rendered
        Assert.Equal("S", grid2[0, 0].Grapheme);
        Assert.Equal("h", grid2[1, 0].Grapheme);

        // Verify that positions after "Short" are cleared, not showing remnants of the long line
        // Position 5 should be clear (not 't' from "Short"), position 6+ should be clear
        for (int x = 6; x < 60; x++)
        {
            Assert.True(
                grid2[x, 0].Grapheme == null || grid2[x, 0].Grapheme == " ",
                $"Column {x} should be cleared but has grapheme '{grid2[x, 0].Grapheme}'"
            );
        }
    }

    [Fact]
    public void TextDrawnAtIndent_ShouldNotExceedTerminalWidth()
    {
        // This test verifies that when text is drawn at an indent position,
        // the total length (indent + text) doesn't exceed the terminal width
        // which would cause wrapping artifacts

        int terminalWidth = 80;
        int indent = 8;
        int maxTextWidth = terminalWidth - indent - 2; // Leave some margin

        // Create a very long line of text (longer than maxTextWidth)
        var longText = "If you look at year over year chip improvements in 2025 vs 1998 it's clear that modern hardware generative";

        // Wrap the text to maxTextWidth
        var wrapped = WrapTextHelper(longText, maxTextWidth);

        // Render at the indent position
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, terminalWidth, 10));
        b.DrawRect(new Rect(0, 0, terminalWidth, 10, new Rgb24(0, 0, 0)));

        int y = 0;
        foreach (var line in wrapped)
        {
            // Verify each line fits within maxTextWidth
            Assert.True(line.Length <= maxTextWidth,
                $"Wrapped line '{line}' is {line.Length} chars but maxWidth is {maxTextWidth}");

            // Verify indent + line length doesn't exceed terminal width
            Assert.True(indent + line.Length <= terminalWidth,
                $"Indent ({indent}) + line length ({line.Length}) = {indent + line.Length} exceeds terminal width {terminalWidth}");

            b.DrawText(new TextRun(indent, y++, line, new Rgb24(200, 200, 200), null, CellAttrFlags.None));
        }

        b.Pop();
        var grid = new TtyCompositor().Composite(b.Build(), (terminalWidth, 10));

        // Verify no text appears at column 0 (all text should start at indent position)
        for (int row = 0; row < wrapped.Count && row < 10; row++)
        {
            for (int col = 0; col < indent; col++)
            {
                Assert.True(
                    grid[col, row].Grapheme == null || grid[col, row].Grapheme == " ",
                    $"Column {col} row {row} should be empty but has '{grid[col, row].Grapheme}'"
                );
            }
        }
    }

    // Helper method that mimics the HackerNewsDemo WrapText logic
    private static List<string> WrapTextHelper(string text, int maxWidth)
    {
        if (string.IsNullOrEmpty(text)) return new List<string>();
        if (maxWidth <= 0) return new List<string> { text };

        var lines = new List<string>();
        var words = text.Split(' ');
        var currentLine = "";

        foreach (var word in words)
        {
            // If the word itself is longer than maxWidth, we need to break it
            if (word.Length > maxWidth)
            {
                // First, add the current line if it has content
                if (!string.IsNullOrEmpty(currentLine))
                {
                    lines.Add(currentLine);
                    currentLine = "";
                }

                // Break the long word into chunks of maxWidth
                for (int i = 0; i < word.Length; i += maxWidth)
                {
                    var chunk = word.Substring(i, Math.Min(maxWidth, word.Length - i));
                    lines.Add(chunk);
                }
                continue;
            }

            // Check if adding this word would exceed maxWidth
            var testLine = currentLine.Length > 0 ? currentLine + " " + word : word;
            if (testLine.Length > maxWidth)
            {
                // Current line is full, save it and start a new one
                if (!string.IsNullOrEmpty(currentLine))
                {
                    lines.Add(currentLine);
                }
                currentLine = word;
            }
            else
            {
                // Add word to current line
                currentLine = testLine;
            }
        }

        // Add the last line
        if (!string.IsNullOrEmpty(currentLine))
        {
            lines.Add(currentLine);
        }

        return lines;
    }

    [Fact]
    public void DamageDetection_ShouldMarkCellsDirty_WhenBackgroundChanges()
    {
        // This test reproduces the actual bug in the compositor:
        // When previous frame has text and current frame has background,
        // damage detection should mark those cells as dirty

        int width = 80;
        int height = 10;

        // Frame 1: Draw text "generative" at column 0
        var b1 = new DisplayListBuilder();
        b1.PushClip(new ClipPush(0, 0, width, height));
        b1.DrawRect(new Rect(0, 0, width, height, new Rgb24(0, 0, 0))); // Black background
        b1.DrawText(new TextRun(0, 0, "generative", new Rgb24(200, 200, 200), null, CellAttrFlags.None));
        b1.Pop();

        var comp = new TtyCompositor();
        var grid1 = comp.Composite(b1.Build(), (width, height));

        // Frame 2: Draw text "comment" at column 8 with background clear
        var b2 = new DisplayListBuilder();
        b2.PushClip(new ClipPush(0, 0, width, height));
        b2.DrawRect(new Rect(0, 0, width, height, new Rgb24(0, 0, 0))); // Black background - should clear columns 0-7
        b2.DrawText(new TextRun(8, 0, "comment", new Rgb24(200, 200, 200), null, CellAttrFlags.None));
        b2.Pop();

        var grid2 = comp.Composite(b2.Build(), (width, height));

        // Get damage between frames
        var damage = comp.Damage(grid1, grid2);

        // Columns 0-7 should be marked as dirty because they changed from "generative" to background
        bool column0Dirty = false;
        foreach (var rect in damage)
        {
            if (rect.Y == 0 && rect.X <= 0 && rect.X + rect.Width > 0)
            {
                column0Dirty = true;
                break;
            }
        }

        Assert.True(column0Dirty,
            "Column 0 should be marked as dirty when changing from text to background, but damage detection missed it");

        // Also verify the actual grid has the right content
        // Columns 0-7 should be blank (space with black background)
        for (int x = 0; x < 8; x++)
        {
            Assert.True(
                grid2[x, 0].Grapheme == " " || grid2[x, 0].Grapheme == null,
                $"Column {x} should be blank but has '{grid2[x, 0].Grapheme}'"
            );
        }

        // Column 8 onwards should have "comment"
        Assert.Equal("c", grid2[8, 0].Grapheme);
    }

    [Fact]
    public void FrameScheduler_ShouldProperlyUpdate_WhenTextMovesPosition()
    {
        // This test simulates exactly what FrameScheduler does:
        // Store previous grid, render new grid, compare, check damage

        int width = 80;
        int height = 10;
        var comp = new TtyCompositor();
        CellGrid? previousGrid = null;

        // Frame 1: Text at position 0
        var b1 = new DisplayListBuilder();
        b1.PushClip(new ClipPush(0, 0, width, height));
        b1.DrawRect(new Rect(0, 0, width, height, new Rgb24(0, 0, 0)));
        b1.DrawText(new TextRun(0, 0, "generative AI is amazing and will change everything", new Rgb24(200, 200, 200), null, CellAttrFlags.None));
        b1.Pop();

        var grid1 = comp.Composite(b1.Build(), (width, height));
        previousGrid = grid1; // Simulate FrameScheduler._previousGrid = cells

        // Frame 2: Background clear + text at position 8 (simulating scrolled comment)
        var b2 = new DisplayListBuilder();
        b2.PushClip(new ClipPush(0, 0, width, height));
        b2.DrawRect(new Rect(0, 0, width, height, new Rgb24(0, 0, 0)));  // Background
        b2.DrawText(new TextRun(8, 0, "This is different text", new Rgb24(200, 200, 200), null, CellAttrFlags.None));
        b2.Pop();

        var grid2 = comp.Composite(b2.Build(), (width, height));

        // Compare like FrameScheduler does
        var damage = comp.Damage(previousGrid, grid2);

        // Verify damage was detected at column 0
        bool hasColumn0Damage = damage.Any(r => r.Y == 0 && r.X == 0);
        Assert.True(hasColumn0Damage, "Column 0 should be in damage rects");

        // Verify grid2 is correct
        Assert.True(grid2[0, 0].Grapheme == " ", $"Column 0 should be blank but is '{grid2[0, 0].Grapheme}'");
        Assert.True(grid2[7, 0].Grapheme == " ", $"Column 7 should be blank but is '{grid2[7, 0].Grapheme}'");
        Assert.Equal("T", grid2[8, 0].Grapheme);
    }
}
