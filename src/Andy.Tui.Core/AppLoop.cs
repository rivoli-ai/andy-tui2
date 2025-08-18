using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using DL = Andy.Tui.DisplayList;

namespace Andy.Tui.Core;

public sealed class AppLoop
{
    private readonly InvalidationBus _bus;
    private readonly FrameScheduler _scheduler;
    private readonly Func<DL.DisplayList> _buildDl;
    private readonly (int W, int H) _viewport;
    private readonly TerminalCapabilities _caps;
    private readonly IPtyIo _pty;

    public AppLoop(InvalidationBus bus, FrameScheduler scheduler, Func<DL.DisplayList> buildDl, (int W, int H) viewport, TerminalCapabilities caps, IPtyIo pty)
    {
        _bus = bus; _scheduler = scheduler; _buildDl = buildDl; _viewport = viewport; _caps = caps; _pty = pty;
    }

    public async Task RunOnceAsync(CancellationToken ct)
    {
        var dl = _buildDl();
        await _scheduler.RenderOnceAsync(dl, _viewport, _caps, _pty, ct);
    }

    public async Task<int> RunForEventsAsync(int maxEvents, CancellationToken ct)
    {
        int renders = 0;
        int pending = 0;
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnRecompose()
        {
            System.Threading.Interlocked.Increment(ref pending);
            tcs.TrySetResult();
        }
        _bus.RecomposeRequested += OnRecompose;
        try
        {
            while (!ct.IsCancellationRequested && renders < maxEvents)
            {
                if (System.Threading.Volatile.Read(ref pending) == 0)
                {
                    using var reg = ct.Register(() => tcs.TrySetCanceled());
                    try { await tcs.Task.ConfigureAwait(false); }
                    catch (TaskCanceledException) { break; }
                    finally { reg.Dispose(); }
                }
                // Drain pending events
                while (pending > 0 && renders < maxEvents)
                {
                    System.Threading.Interlocked.Decrement(ref pending);
                    var dl = _buildDl();
                    await _scheduler.RenderOnceAsync(dl, _viewport, _caps, _pty, ct).ConfigureAwait(false);
                    renders++;
                }
                // prepare next await
                tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }
            return renders;
        }
        finally
        {
            _bus.RecomposeRequested -= OnRecompose;
        }
    }
}
