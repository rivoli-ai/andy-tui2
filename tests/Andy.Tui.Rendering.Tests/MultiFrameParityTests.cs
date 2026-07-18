using Andy.Tui.Backend.Terminal;
using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;
using Dl = Andy.Tui.DisplayList.DisplayList;

namespace Andy.Tui.Rendering.Tests;

/// <summary>
/// Adversarial multi-frame parity: drive a sequence of frames through the real pipeline
/// (DisplayList -> Composite -> Damage -> RowRuns -> AnsiEncoder) emitting only the damaged
/// runs each frame, replay those frames onto a persistent <see cref="StatefulTerminalOracle"/>,
/// and assert the accumulated terminal state equals a full repaint of the final frame.
///
/// This is exactly the class of defect that per-op unit tests miss: a damage computation that
/// forgets to repaint a cell shows up only once several frames have layered on top of each other.
/// </summary>
public class MultiFrameParityTests
{
    private static readonly TerminalCapabilities Caps = new() { TrueColor = true, Palette256 = true };

    private static Dl Frame(int w, int h, params (int x, int y, string text, Rgb24 fg)[] texts)
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, w, h));
        b.DrawRect(new Rect(0, 0, w, h, new Rgb24(0, 0, 0)));
        foreach (var t in texts)
            b.DrawText(new TextRun(t.x, t.y, t.text, t.fg, new Rgb24(0, 0, 0), CellAttrFlags.None));
        b.Pop();
        return b.Build();
    }

    /// <summary>
    /// Replays frames incrementally (damage-only) and returns the final oracle snapshot along with
    /// the full-repaint reference grid for the final frame.
    /// </summary>
    private static (CellGrid incremental, CellGrid reference, string trace) Replay(int w, int h, IReadOnlyList<Dl> frames)
    {
        var comp = new TtyCompositor();
        var oracle = new StatefulTerminalOracle(w, h);
        var prev = new CellGrid(w, h);
        var trace = new System.Text.StringBuilder();
        CellGrid last = prev;

        for (int f = 0; f < frames.Count; f++)
        {
            var next = comp.Composite(frames[f], (w, h));
            var dirty = comp.Damage(prev, next);
            var runs = comp.RowRuns(next, dirty);
            var bytes = new AnsiEncoder().Encode(runs, Caps);
            oracle.ApplyFrame(bytes.Span);
            trace.Append($"frame {f}: {dirty.Count} dirty rects, {bytes.Length} bytes\n");
            prev = next;
            last = next;
        }

        return (oracle.Snapshot(), last, trace.ToString());
    }

    [Fact]
    public void Incremental_Frames_Converge_To_Full_Repaint()
    {
        int w = 20, h = 5;
        var white = new Rgb24(255, 255, 255);
        var red = new Rgb24(255, 0, 0);
        var frames = new List<Dl>
        {
            Frame(w, h, (1, 0, "alpha", white)),
            Frame(w, h, (1, 0, "alpha", white), (1, 2, "beta", red)),
            Frame(w, h, (1, 0, "gamma", red), (1, 2, "beta", red)),
            Frame(w, h, (1, 0, "gamma", red)), // beta removed -> must be cleared
        };

        var (incremental, reference, trace) = Replay(w, h, frames);
        var diff = StatefulTerminalOracle.Diff(reference, incremental);
        Assert.True(diff.Length == 0,
            $"Incremental rendering diverged from full repaint.\n{trace}\nDiff:\n{diff}\n" +
            $"Expected:\n{StatefulTerminalOracle.Dump(reference)}\nActual:\n{StatefulTerminalOracle.Dump(incremental)}");
    }

    [Fact]
    public void Cell_Cleared_When_Text_Shrinks_Across_Frames()
    {
        int w = 16, h = 3;
        var white = new Rgb24(255, 255, 255);
        var frames = new List<Dl>
        {
            Frame(w, h, (0, 0, "longlabel", white)),
            Frame(w, h, (0, 0, "hi", white)), // trailing "nglabel" must be erased
        };

        var (incremental, reference, trace) = Replay(w, h, frames);
        var diff = StatefulTerminalOracle.Diff(reference, incremental);
        Assert.True(diff.Length == 0, $"{trace}\nDiff:\n{diff}");

        // Explicit check: column 2 onward must be blank (black bg), not stale glyphs.
        for (int x = 2; x < 9; x++)
        {
            var g = incremental[x, 0].Grapheme ?? " ";
            Assert.True(string.IsNullOrWhiteSpace(g), $"cell ({x},0) still holds stale glyph '{g}'");
        }
    }

    [Fact]
    public void Oracle_Reconstructs_Multibyte_Graphemes_Verbatim()
    {
        // Non-ASCII content spans several UTF-8 bytes per grapheme. Replaying the encoded frame
        // onto the oracle must reproduce the exact graphemes the compositor drew, not one
        // U+FFFD replacement char per byte. Uses precomposed, single-width code points so the
        // comparison stays on the oracle's UTF-8 decoding, not cell-width modelling.
        int w = 24, h = 3;
        var white = new Rgb24(255, 255, 255);
        var frames = new List<Dl>
        {
            Frame(w, h, (0, 1, "café résumé €ñ", white)),
        };

        var (incremental, reference, trace) = Replay(w, h, frames);
        var diff = StatefulTerminalOracle.Diff(reference, incremental);
        Assert.True(diff.Length == 0,
            $"Oracle corrupted multibyte text.\n{trace}\nDiff:\n{diff}\n" +
            $"Expected:\n{StatefulTerminalOracle.Dump(reference)}\nActual:\n{StatefulTerminalOracle.Dump(incremental)}");

        // Spot-check the accented graphemes survived intact (no U+FFFD, correct code points).
        var snap = incremental;
        var text = string.Concat(Enumerable.Range(0, 14).Select(i => snap[i, 1].Grapheme));
        Assert.Equal("café résumé €ñ", text);
        Assert.DoesNotContain("�", text);
    }

    [Fact]
    public void Damage_Only_Frame_Preserves_Untouched_Cells()
    {
        int w = 24, h = 4;
        var white = new Rgb24(255, 255, 255);
        var comp = new TtyCompositor();
        var oracle = new StatefulTerminalOracle(w, h);

        // Frame 0: full paint of two labels.
        var f0 = Frame(w, h, (0, 0, "top", white), (0, 3, "bottom", white));
        var g0 = comp.Composite(f0, (w, h));
        var d0 = comp.Damage(new CellGrid(w, h), g0);
        oracle.ApplyFrame(new AnsiEncoder().Encode(comp.RowRuns(g0, d0), Caps).Span);

        // Frame 1: only the top label changes; bottom label untouched.
        var f1 = Frame(w, h, (0, 0, "TOP", white), (0, 3, "bottom", white));
        var g1 = comp.Composite(f1, (w, h));
        var d1 = comp.Damage(g0, g1);
        Assert.All(d1, r => Assert.NotEqual(3, r.Y)); // bottom row should not be damaged
        oracle.ApplyFrame(new AnsiEncoder().Encode(comp.RowRuns(g1, d1), Caps).Span);

        var snap = oracle.Snapshot();
        // Bottom label survived even though frame 1 never re-emitted it.
        var bottom = string.Concat(Enumerable.Range(0, 6).Select(i => snap[i, 3].Grapheme));
        Assert.Equal("bottom", bottom);
    }
}
