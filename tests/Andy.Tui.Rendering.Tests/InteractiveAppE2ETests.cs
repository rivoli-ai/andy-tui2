using Andy.Tui.Backend.Terminal;
using Andy.Tui.Compositor;
using Andy.Tui.Input;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;
using W = Andy.Tui.Widgets;

namespace Andy.Tui.Rendering.Tests;

/// <summary>
/// A realistic interactive application driven end to end. A <see cref="W.ListBox"/> (widget +
/// theme/style + layout) is wired to raw keyboard bytes decoded by <see cref="TtyStreamDecoder"/>
/// (input), re-rendered every frame through the compositor and ANSI encoder (rendering), and
/// replayed onto a persistent <see cref="StatefulTerminalOracle"/> that mirrors a real terminal.
/// The test asserts the visible terminal state after a scripted session — exercising input,
/// state, style, layout, and rendering together, which no single-layer unit test does.
/// </summary>
public class InteractiveAppE2ETests
{
    private static readonly TerminalCapabilities Caps = new() { TrueColor = true, Palette256 = true };

    /// <summary>The application under test: an interactive selectable list.</summary>
    private sealed class ListApp
    {
        private readonly W.ListBox _list = new();
        private readonly L.Rect _rect;
        public ListApp((int W, int H) size, IEnumerable<string> items)
        {
            _rect = new L.Rect(0, 0, size.W, size.H);
            _list.SetItems(items);
            _list.SetSelectedIndex(0);
        }

        public DL.Rgb24 SelectedBg => _list.SelectedBg;
        public int SelectedIndex => _list.SelectedIndex;

        /// <summary>Applies one decoded input event to the app's state.</summary>
        public void Handle(IInputEvent ev)
        {
            int viewportRows = (int)_rect.Height;
            switch (ev)
            {
                case KeyEvent { Key: "ArrowDown" }: _list.MoveSelection(+1, viewportRows); break;
                // Clamp at the top: keep a valid selection rather than dropping to "none" (-1).
                case KeyEvent { Key: "ArrowUp" } when _list.SelectedIndex > 0: _list.MoveSelection(-1, viewportRows); break;
            }
        }

        /// <summary>Builds this frame's display list from current state.</summary>
        public DL.DisplayList Render()
        {
            var b = new DL.DisplayListBuilder();
            _list.Render(_rect, new DL.DisplayListBuilder().Build(), b);
            return b.Build();
        }
    }

    private static CellGrid RunSession(int w, int h, IEnumerable<string> items, byte[] keystrokes)
    {
        var app = new ListApp((w, h), items);
        var comp = new TtyCompositor();
        var oracle = new StatefulTerminalOracle(w, h);
        var decoder = new TtyStreamDecoder();
        var prev = new CellGrid(w, h);

        // Frame 0: initial paint.
        void PaintFrame()
        {
            var next = comp.Composite(app.Render(), (w, h));
            var dirty = comp.Damage(prev, next);
            var runs = comp.RowRuns(next, dirty);
            oracle.ApplyFrame(new AnsiEncoder().Encode(runs, Caps).Span);
            prev = next;
        }

        PaintFrame();

        // Deliver the keystroke burst and repaint after every decoded event, so the persistent
        // oracle accumulates one frame per input event exactly as an event loop would drive it.
        foreach (var ev in decoder.Push(keystrokes))
        {
            app.Handle(ev);
            PaintFrame();
        }

        return oracle.Snapshot();
    }

    private static string RowText(CellGrid g, int row)
    {
        var sb = new System.Text.StringBuilder();
        for (int x = 0; x < g.Width; x++) sb.Append(g[x, row].Grapheme ?? " ");
        return sb.ToString();
    }

    [Fact]
    public void Arrow_Navigation_Highlights_Selected_Row_End_To_End()
    {
        int w = 24, h = 8;
        var items = new[] { "Alpha", "Bravo", "Charlie", "Delta", "Echo" };

        // ArrowDown x2 lands on index 2 ("Charlie").
        var keys = "[B[B"u8.ToArray();
        var screen = RunSession(w, h, items, keys);

        // The list draws rows starting at y=0; item i sits on row i.
        // Row 2 ("Charlie") must carry the selection background; a neighbour must not.
        var selectedBg = new W.ListBox().SelectedBg;
        var normalBg = new W.ListBox().Bg;

        Assert.Equal(selectedBg, screen[1, 2].Bg); // interior cell of the "Charlie" row
        Assert.Equal(normalBg, screen[1, 1].Bg);   // "Bravo" row is not selected

        // The item text is actually visible on its row.
        Assert.Contains("Charlie", RowText(screen, 2));
        Assert.Contains("Bravo", RowText(screen, 1));
    }

    [Fact]
    public void Selection_Clamps_At_Boundaries_Over_Full_Session()
    {
        int w = 20, h = 8;
        var items = new[] { "one", "two", "three" };

        // Five ArrowUps from index 0 must clamp at 0, not underflow.
        var up = "[A[A[A[A[A"u8.ToArray();
        var screen = RunSession(w, h, items, up);
        var selectedBg = new W.ListBox().SelectedBg;
        Assert.Equal(selectedBg, screen[1, 0].Bg);
        Assert.Contains("one", RowText(screen, 0));

        // Ten ArrowDowns must clamp at the last item (index 2, "three").
        var down = System.Text.Encoding.ASCII.GetBytes(string.Concat(Enumerable.Repeat("[B", 10)));
        var screen2 = RunSession(w, h, items, down);
        Assert.Equal(selectedBg, screen2[1, 2].Bg);
        Assert.Contains("three", RowText(screen2, 2));
    }
}
