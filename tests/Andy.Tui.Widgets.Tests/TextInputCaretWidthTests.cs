using Xunit;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;
using TT = Andy.Tui.Text;

namespace Andy.Tui.Widgets.Tests;

/// <summary>
/// Guards that the TextInput caret column is measured in terminal cells (via
/// <see cref="TT.TerminalText"/>) rather than UTF-16 code-unit counts, so the
/// caret stays aligned with wide-glyph (2-column) rendering.
/// </summary>
public class TextInputCaretWidthTests
{
    private static int CaretX(TextInput input, L.Rect rect)
    {
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        input.Render(rect, baseDl, b);
        var dl = b.Build();
        // The caret is the "|" text run.
        foreach (var op in dl.Ops)
        {
            if (op is DL.TextRun tr && tr.Content == "|")
            {
                return tr.X;
            }
        }
        return -1;
    }

    [Fact]
    public void Caret_After_WideGlyph_Advances_By_Two_Columns()
    {
        // "世" is a single UTF-16 code unit but renders as 2 terminal columns.
        // Sanity-check the divergence the caret math must account for.
        Assert.Equal(1, "世".Length);
        Assert.Equal(2, TT.TerminalText.MeasureWidth("世"));

        var input = new TextInput();
        input.SetText("世");
        input.SetCursor(1); // caret sits after the wide glyph
        input.SetFocused(true);
        input.SetShowCaret(true);

        // rect at x=0, wide enough that the caret is not clamped.
        int caretX = CaretX(input, new L.Rect(0, 0, 10, 1));

        // x(0) + border(1) + width-of("世")(2) == 3.
        // If the caret counted UTF-16 units it would land at column 2.
        Assert.Equal(3, caretX);
    }

    [Fact]
    public void Caret_Mid_String_Uses_Cell_Width_Of_Prefix()
    {
        var input = new TextInput();
        input.SetText("a世b");
        input.SetCursor(2); // after "a世"
        input.SetFocused(true);
        input.SetShowCaret(true);

        int caretX = CaretX(input, new L.Rect(0, 0, 12, 1));

        // x(0) + border(1) + width-of("a世")(1+2=3) == 4.
        // UTF-16 counting would place it at column 3.
        Assert.Equal(4, caretX);
    }

    [Fact]
    public void Caret_With_Ascii_Prefix_Matches_Character_Count()
    {
        var input = new TextInput();
        input.SetText("hello");
        input.SetCursor(3);
        input.SetFocused(true);
        input.SetShowCaret(true);

        int caretX = CaretX(input, new L.Rect(0, 0, 12, 1));

        // Pure ASCII: cells == characters, so x(0) + border(1) + 3 == 4.
        Assert.Equal(4, caretX);
    }
}
