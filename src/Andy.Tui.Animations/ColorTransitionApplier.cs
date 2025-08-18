using Andy.Tui.DisplayList;

namespace Andy.Tui.Animations;

public static class ColorTransitionApplier
{
    public static TextRun Apply(TextRun run, long startMs, long nowMs, TransitionColor transition)
    {
        var t = Math.Clamp((nowMs - startMs) / (double)transition.DurationMs, 0.0, 1.0);
        var fg = Interpolators.Lerp(transition.From, transition.To, t);
        return new TextRun(run.X, run.Y, run.Content, fg, run.Bg, run.Attrs);
    }
}
