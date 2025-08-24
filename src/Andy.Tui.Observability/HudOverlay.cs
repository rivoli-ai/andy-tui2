using Andy.Tui.DisplayList;

namespace Andy.Tui.Observability;

public sealed class HudOverlay : IFrameMetricsSink, IFrameTimingsSink
{
    public bool Enabled { get; set; }
    public double Fps { get; set; }
    public double DirtyPercent { get; set; }
    public int BytesPerFrame { get; set; }
    public FrameTimings? LastTimings { get; private set; }
    public int ViewportCols { get; set; }
    public int ViewportRows { get; set; }
    public int PanelWidth { get; set; } = 40;
    public int PanelHeight { get; set; } = 5;
    // CPU metrics for current process
    private TimeSpan _lastProcCpu = TimeSpan.Zero;
    private long _lastWallTicks = 0;
    public double? CpuProcessPercent { get; private set; }

    public void Contribute(DisplayList.DisplayList dl, DisplayListBuilder builder)
    {
        if (!Enabled) return;
        int x0 = Math.Max(0, ViewportCols - PanelWidth - 1); // keep 1 col margin from right
        int y0 = 0;
        int pw = PanelWidth;
        int ph = PanelHeight;
        builder.PushClip(new ClipPush(x0, y0, pw, ph));
        builder.DrawRect(new Rect(x0, y0, pw, ph, new Rgb24(0, 0, 0)));
        var text = $"FPS: {Fps:F1} Dirty: {DirtyPercent:P0} Bytes: {BytesPerFrame}";
        builder.DrawText(new TextRun(x0 + 1, y0 + 1, text, new Rgb24(200, 200, 200), null, CellAttrFlags.None));
        if (LastTimings is FrameTimings t)
        {
            var line2 = $"DL:{t.DisplayListMs}ms Comp:{t.CompositeMs} Dam:{t.DamageMs} Runs:{t.RowRunsMs} Enc:{t.EncodeMs} Wr:{t.WriteMs}";
            builder.DrawText(new TextRun(x0 + 1, y0 + 2, line2, new Rgb24(120, 120, 120), null, CellAttrFlags.None));
        }
        // CPU line
        var cpu = ComputeProcessCpuPercent();
        if (cpu is double pc)
        {
            var line3 = $"CPU(proc): {pc:F1}%";
            builder.DrawText(new TextRun(x0 + 1, y0 + 3, line3, new Rgb24(160, 160, 200), null, CellAttrFlags.None));
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

    private double? ComputeProcessCpuPercent()
    {
        try
        {
            var proc = System.Diagnostics.Process.GetCurrentProcess();
            var cpu = proc.TotalProcessorTime;
            long nowTicks = System.Environment.TickCount64 * TimeSpan.TicksPerMillisecond;
            if (_lastWallTicks == 0)
            {
                _lastWallTicks = nowTicks; _lastProcCpu = cpu; return CpuProcessPercent;
            }
            long wallDeltaTicks = nowTicks - _lastWallTicks;
            if (wallDeltaTicks <= 0) return CpuProcessPercent;
            var cpuDelta = (cpu - _lastProcCpu).Ticks;
            // Normalize by wall time and number of processors
            double pc = 100.0 * (double)cpuDelta / (double)wallDeltaTicks / (double)System.Environment.ProcessorCount;
            _lastWallTicks = nowTicks; _lastProcCpu = cpu; CpuProcessPercent = Math.Max(0, Math.Min(100, pc));
            return CpuProcessPercent;
        }
        catch
        {
            return CpuProcessPercent;
        }
    }
}
