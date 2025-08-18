namespace Andy.Tui.Observability;

public interface IFrameMetricsSink
{
    void Update(double fps, double dirtyPercent, int bytesPerFrame);
}

public readonly record struct FrameTimings(
    long ComposeMs,
    long StyleMs,
    long LayoutMs,
    long DisplayListMs,
    long CompositeMs,
    long DamageMs,
    long RowRunsMs,
    long EncodeMs,
    long WriteMs
);

public interface IFrameTimingsSink
{
    void UpdateTimings(FrameTimings timings);
}
