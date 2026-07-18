using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Andy.Tui.Core.Tests;

public class FrameSchedulerPacingTests
{
    private sealed class FakeClock : IClock
    {
        public long NowMs { get; private set; }
        public void Advance(long ms) => NowMs += ms;
    }

    /// <summary>Captures every byte buffer written so tests can assert on emitted ANSI.</summary>
    private sealed class CapturingPty : Andy.Tui.Backend.Terminal.IPtyIo
    {
        public List<byte[]> Writes { get; } = new();
        public Task WriteAsync(ReadOnlyMemory<byte> bytes, CancellationToken ct = default)
        {
            Writes.Add(bytes.ToArray());
            return Task.CompletedTask;
        }
    }

    /// <summary>Records the metrics pushed by the scheduler after each frame.</summary>
    private sealed class RecordingMetricsSink : Andy.Tui.Observability.IFrameMetricsSink
    {
        public int UpdateCount { get; private set; }
        public double LastFps { get; private set; }
        public double LastDirty { get; private set; }
        public int LastBytes { get; private set; }
        public void Update(double fps, double dirtyPercent, int bytesPerFrame)
        {
            UpdateCount++;
            LastFps = fps;
            LastDirty = dirtyPercent;
            LastBytes = bytesPerFrame;
        }
    }

    private static Andy.Tui.DisplayList.DisplayList BuildText(int x, int y, string text)
    {
        var b = new Andy.Tui.DisplayList.DisplayListBuilder();
        b.DrawText(new Andy.Tui.DisplayList.TextRun(
            x, y, text,
            new Andy.Tui.DisplayList.Rgb24(255, 255, 255), null,
            Andy.Tui.DisplayList.CellAttrFlags.None));
        return b.Build();
    }

    private static bool ContainsSubsequence(byte[] haystack, byte[] needle)
    {
        for (int i = 0; i + needle.Length <= haystack.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j]) { match = false; break; }
            }
            if (match) return true;
        }
        return false;
    }

    [Fact]
    public async Task RenderOnce_Returns_Nonzero_Bytes_And_Records_Metrics()
    {
        var clock = new FakeClock();
        var scheduler = new FrameScheduler(clock, targetFps: 30);
        var sink = new RecordingMetricsSink();
        scheduler.SetMetricsSink(sink);
        var pty = new CapturingPty();
        var caps = Andy.Tui.Backend.Terminal.CapabilityDetector.DetectFromEnvironment();
        var viewport = (W: 80, H: 24);
        var dl = BuildText(2, 2, "hello");

        var (bytes, elapsedMs) = await scheduler.RenderOnceWithMetricsAsync(
            dl, viewport, caps, pty, CancellationToken.None);

        // Behavioral: the frame actually produced output and reported it.
        Assert.True(bytes > 0, "scheduler should emit a non-empty byte buffer for a non-empty frame");
        Assert.Single(pty.Writes);
        Assert.Equal(bytes, pty.Writes[0].Length);
        Assert.True(elapsedMs >= 0);

        // Behavioral: metrics sink received exactly one update reflecting the frame.
        Assert.Equal(1, sink.UpdateCount);
        Assert.True(sink.LastFps > 0, "EMA fps should be positive after a frame");
        Assert.Equal(bytes, sink.LastBytes);
    }

    [Fact]
    public async Task First_Frame_Emits_Full_Clear_Then_Identical_Frame_Diffs_To_Fewer_Bytes()
    {
        var clock = new FakeClock();
        var scheduler = new FrameScheduler(clock, targetFps: 60);
        var pty = new CapturingPty();
        var caps = Andy.Tui.Backend.Terminal.CapabilityDetector.DetectFromEnvironment();
        var viewport = (W: 40, H: 10);
        var dl = BuildText(1, 1, "stable content");

        // First frame: size just became known -> full clear + full paint.
        var (firstBytes, _) = await scheduler.RenderOnceWithMetricsAsync(
            dl, viewport, caps, pty, CancellationToken.None);
        // Second frame with identical content -> damage set is empty -> far fewer bytes.
        var (secondBytes, _) = await scheduler.RenderOnceWithMetricsAsync(
            dl, viewport, caps, pty, CancellationToken.None);

        Assert.Equal(2, pty.Writes.Count);

        // The first frame must contain the clear-screen sequence (ESC [ 2 J).
        var clearSeq = Encoding.ASCII.GetBytes("\x1b[2J");
        Assert.True(ContainsSubsequence(pty.Writes[0], clearSeq),
            "first frame must reset the screen with ESC[2J");
        Assert.False(ContainsSubsequence(pty.Writes[1], clearSeq),
            "steady-state frame must not re-clear the whole screen");

        // Diffing must make an unchanged frame strictly cheaper than the first paint.
        Assert.True(secondBytes < firstBytes,
            $"unchanged frame ({secondBytes} bytes) should be cheaper than first paint ({firstBytes} bytes)");
    }

    [Fact]
    public async Task LowerTargetFps_Paces_Out_To_A_Longer_Idle_Sleep()
    {
        // Pacing contract: idle sleep is bounded by the per-frame budget, which shrinks
        // as target fps rises. We observe this via wall-clock delay of an empty frame.
        var caps = Andy.Tui.Backend.Terminal.CapabilityDetector.DetectFromEnvironment();
        var viewport = (W: 10, H: 3);
        var empty = new Andy.Tui.DisplayList.DisplayListBuilder().Build();

        var fast = new FrameScheduler(new FakeClock(), targetFps: 120);
        var slow = new FrameScheduler(new FakeClock(), targetFps: 15);

        var swFast = System.Diagnostics.Stopwatch.StartNew();
        await fast.RenderOnceWithMetricsAsync(empty, viewport, caps, new CapturingPty(), CancellationToken.None);
        swFast.Stop();

        var swSlow = System.Diagnostics.Stopwatch.StartNew();
        await slow.RenderOnceWithMetricsAsync(empty, viewport, caps, new CapturingPty(), CancellationToken.None);
        swSlow.Stop();

        // A 15fps budget (~66ms) must idle-sleep longer than a 120fps budget (~8ms).
        // Allow slack for scheduler jitter but still assert the ordering holds.
        Assert.True(swSlow.ElapsedMilliseconds + 5 >= swFast.ElapsedMilliseconds,
            $"lower target fps should not sleep less than higher target fps (slow={swSlow.ElapsedMilliseconds}ms fast={swFast.ElapsedMilliseconds}ms)");
        Assert.True(swSlow.ElapsedMilliseconds >= 30,
            $"a 15fps frame should pace out to a visible idle sleep (was {swSlow.ElapsedMilliseconds}ms)");
    }
}
