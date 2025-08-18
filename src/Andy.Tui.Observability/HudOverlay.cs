using Andy.Tui.DisplayList;

namespace Andy.Tui.Observability;

public sealed class HudOverlay : IFrameMetricsSink, IFrameTimingsSink
{
    public bool Enabled { get; set; }
    public double Fps { get; set; }
    public double DirtyPercent { get; set; }
    public int BytesPerFrame { get; set; }
    public FrameTimings? LastTimings { get; private set; }

    public void Contribute(DisplayList.DisplayList dl, DisplayListBuilder builder)
    {
        if (!Enabled) return;
        builder.PushClip(new ClipPush(0, 0, 100, 4));
        builder.DrawRect(new Rect(0, 0, 100, 4, new Rgb24(0, 0, 0)));
        var text = $"FPS: {Fps:F1} Dirty: {DirtyPercent:P0} Bytes: {BytesPerFrame}";
        builder.DrawText(new TextRun(1, 1, text, new Rgb24(200, 200, 200), null, CellAttrFlags.None));
        if (LastTimings is FrameTimings t)
        {
            var line2 = $"DL:{t.DisplayListMs}ms Comp:{t.CompositeMs} Dam:{t.DamageMs} Runs:{t.RowRunsMs} Enc:{t.EncodeMs} Wr:{t.WriteMs}";
            builder.DrawText(new TextRun(1, 2, line2, new Rgb24(120, 120, 120), null, CellAttrFlags.None));
        }
        builder.Pop();
    }

    public void Update(double fps, double dirtyPercent, int bytesPerFrame)
    {
        Fps = fps; DirtyPercent = dirtyPercent; BytesPerFrame = bytesPerFrame;
    }

    public void UpdateTimings(FrameTimings timings)
    {
        LastTimings = timings;
    }
}
