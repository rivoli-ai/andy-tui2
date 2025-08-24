using System.Diagnostics;
using Andy.Tui.Backend.Terminal;
using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;
using Andy.Tui.Observability;

namespace Andy.Tui.Core;

public interface IClock { long NowMs { get; } }
public sealed class SimpleRealClock : IClock { public long NowMs => Environment.TickCount64; }
public sealed class SimpleManualClock : IClock { public long NowMs { get; private set; } public void Advance(long ms) => NowMs += ms; }

public sealed class FrameScheduler
{
    private readonly IClock _clock;
    private readonly int _targetFrameMs;
    private long _lastFrameEnd;
    private double _emaFps;
    private Andy.Tui.Observability.IFrameMetricsSink? _metricsSink;
    private Andy.Tui.Observability.IFrameTimingsSink? _timingsSink;
    private CellGrid? _previousGrid;
    private bool _forceFullClear;

    public FrameScheduler(IClock? clock = null, int targetFps = 60)
    {
        _clock = clock ?? new SimpleRealClock();
        _targetFrameMs = Math.Max(1, 1000 / Math.Max(1, targetFps));
    }

    public void SetMetricsSink(Andy.Tui.Observability.IFrameMetricsSink sink) => _metricsSink = sink;
    public void SetTimingsSink(Andy.Tui.Observability.IFrameTimingsSink sink) => _timingsSink = sink;
    public void SetForceFullClear(bool enabled) => _forceFullClear = enabled;

    public async Task RenderOnceAsync(DisplayList.DisplayList dl, (int W, int H) viewport, TerminalCapabilities caps, IPtyIo pty, CancellationToken ct)
    {
        await RenderOnceWithMetricsAsync(dl, viewport, caps, pty, ct);
    }

    public async Task<(int bytes, long elapsedMs)> RenderOnceWithMetricsAsync(DisplayList.DisplayList dl, (int W, int H) viewport, TerminalCapabilities caps, IPtyIo pty, CancellationToken ct)
    {
        var start = _clock.NowMs;
        int bytesLen = 0;
        double dirtyPercent = 0.0;
        long compMs = 0, damageMs = 0, runsMs = 0, encMs = 0, writeMs = 0;
        using (Tracer.BeginSpan("Scheduler", "frame"))
        {
            var comp = new TtyCompositor();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var cells = comp.Composite(dl, viewport);
            compMs = sw.ElapsedMilliseconds;
            sw.Restart();
            // If viewport size changed, compare against a fresh empty grid to force full repaint
            bool sizeChangedPre = _previousGrid == null || _previousGrid.Width != viewport.W || _previousGrid.Height != viewport.H;
            var prev = sizeChangedPre ? new CellGrid(viewport.W, viewport.H) : (_previousGrid ?? new CellGrid(viewport.W, viewport.H));
            var dirty = comp.Damage(prev, cells);
            damageMs = sw.ElapsedMilliseconds;
            sw.Restart();
            var runs = comp.RowRuns(cells, dirty);
            runsMs = sw.ElapsedMilliseconds;
            sw.Restart();
            var enc = new Andy.Tui.Backend.Terminal.AnsiEncoder();
            var bytes = enc.Encode(runs, caps);
            encMs = sw.ElapsedMilliseconds;
            bytesLen = bytes.Length;
            sw.Restart();
            // Do not inject separate EOL clears here; rely on widgets to overwrite backgrounds
            // If viewport size changed, or forced, clear the screen before drawing
            bool sizeChanged = sizeChangedPre;
            if (sizeChanged || _forceFullClear)
            {
                var clear = System.Text.Encoding.UTF8.GetBytes("\x1b[2J\x1b[H");
                var body = bytes.ToArray();
                var combined = new byte[clear.Length + body.Length];
                System.Buffer.BlockCopy(clear, 0, combined, 0, clear.Length);
                System.Buffer.BlockCopy(body, 0, combined, clear.Length, body.Length);
                await pty.WriteAsync(combined, ct);
                bytesLen = combined.Length;
            }
            else
            {
                await pty.WriteAsync(bytes, ct);
            }
            writeMs = sw.ElapsedMilliseconds;
            // compute dirty coverage
            int totalArea = Math.Max(1, viewport.W * viewport.H);
            int dirtyArea = 0;
            for (int i = 0; i < dirty.Count; i++) dirtyArea += dirty[i].Width * dirty[i].Height;
            dirtyPercent = Math.Clamp((double)dirtyArea / totalArea, 0.0, 1.0);
            _previousGrid = cells;
        }
        var elapsed = _clock.NowMs - start;
        var sleep = _targetFrameMs - (int)elapsed;
        if (sleep > 0) await Task.Delay(sleep, ct);
        var end = _clock.NowMs;
        var frameTimeMs = (end - _lastFrameEnd) > 0 ? (end - _lastFrameEnd) : (elapsed > 0 ? elapsed : _targetFrameMs);
        _lastFrameEnd = end;
        var fps = frameTimeMs > 0 ? 1000.0 / frameTimeMs : _targetFrameMs;
        _emaFps = _emaFps <= 0 ? fps : (_emaFps * 0.8 + fps * 0.2);
        _metricsSink?.Update(_emaFps, dirtyPercent, bytesLen);
        if (_metricsSink is HudOverlay hud)
        {
            hud.ViewportCols = viewport.W;
            hud.ViewportRows = viewport.H;
        }
        _timingsSink?.UpdateTimings(new Andy.Tui.Observability.FrameTimings(
            ComposeMs: 0,
            StyleMs: 0,
            LayoutMs: 0,
            DisplayListMs: 0,
            CompositeMs: compMs,
            DamageMs: damageMs,
            RowRunsMs: runsMs,
            EncodeMs: encMs,
            WriteMs: writeMs
        ));
        return (bytesLen, elapsed);
    }

    // EOL clears are handled by widgets drawing background/space; keep core minimal for now
}
