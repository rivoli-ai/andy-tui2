using Andy.Tui.DisplayList;

namespace Andy.Tui.Animations;

public static class OpacityTransitionApplier
{
    // Simulate opacity by lerping foreground toward background (or black if bg unset)
    public static TextRun Apply(TextRun run, long startMs, long nowMs, int durationMs, double fromOpacity, double toOpacity)
    {
        var t = Math.Clamp((nowMs - startMs) / (double)durationMs, 0.0, 1.0);
        var opacity = fromOpacity + (toOpacity - fromOpacity) * t;
        var bg = run.Bg ?? new Rgb24(0, 0, 0);
        // A null (default/terminal) foreground has no concrete RGB to lerp; approximate
        // it with the terminal's typical default of white for the opacity simulation.
        var fgColor = run.Fg ?? new Rgb24(255, 255, 255);
        var fg = Interpolators.Lerp(bg, fgColor, Math.Clamp(opacity, 0.0, 1.0));
        return new TextRun(run.X, run.Y, run.Content, fg, run.Bg, run.Attrs);
    }
}