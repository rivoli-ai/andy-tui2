using System.Text;
using Andy.Tui.Compose;
using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;
using Xunit;

namespace Andy.Tui.Compose.Tests;

public class EndToEndTerminalOutputTests
{
    // Composites a composition to a terminal cell grid and reads back a row of text,
    // exercising the full path: reconcile -> render display list -> composite to cells.
    private static string RowText(Composer composer, int row, int width, int height)
    {
        var dl = composer.Render();
        var grid = new TtyCompositor().Composite(dl, (width, height));
        var sb = new StringBuilder();
        for (int x = 0; x < width; x++)
        {
            var g = grid[x, row].Grapheme;
            sb.Append(string.IsNullOrEmpty(g) ? " " : g);
        }
        return sb.ToString().TrimEnd();
    }

    [Fact]
    public void State_Change_Drives_Terminal_Output()
    {
        var scheduler = new ManualFrameScheduler();
        StateRef<int> counter = default;

        var root = new VComponent(ctx =>
        {
            counter = ctx.UseState(0);
            return new VText($"Count: {counter.Value}")
                .WithForeground(new Rgb24(0, 255, 0));
        });

        var composer = new Composer(root, scheduler);
        composer.Recompose();

        // Initial frame reaches the terminal grid.
        Assert.Equal("Count: 0", RowText(composer, 0, 20, 3));

        // Drive a state change; the scheduled frame recomposes and the new value
        // is visible in the composited terminal output.
        counter.Set(1);
        Assert.True(scheduler.HasPendingFrame);
        scheduler.Flush();

        Assert.Equal("Count: 1", RowText(composer, 0, 20, 3));
    }

    [Fact]
    public void Styled_Text_Carries_Foreground_Into_Cells()
    {
        var root = new VComponent(_ => new VText("hi").WithForeground(new Rgb24(10, 20, 30)));
        var composer = new Composer(root);
        composer.Recompose();

        var grid = new TtyCompositor().Composite(composer.Render(), (10, 2));
        Assert.Equal(new Rgb24(10, 20, 30), grid[0, 0].Fg);
        Assert.Equal("h", grid[0, 0].Grapheme);
    }
}
