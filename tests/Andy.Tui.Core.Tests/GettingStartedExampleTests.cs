using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using Andy.Tui.Core;
using Andy.Tui.Core.Bindings;
using Andy.Tui.Core.Reactive;
using Andy.Tui.DisplayList;
using Xunit;

namespace Andy.Tui.Core.Tests;

/// <summary>
/// Compiles and executes the minimal example documented in
/// docs/GETTING_STARTED.md so the "copy, paste, run" claim stays true.
/// The example is exercised against a headless in-memory writer instead of a
/// real terminal so it is deterministic and safe to run in CI.
/// </summary>
public class GettingStartedExampleTests
{
    /// <summary>
    /// The writer from the documented example. Real programs send bytes to
    /// stdout; here we capture them so we can assert on the rendered frame.
    /// </summary>
    private sealed class BufferPty : IPtyIo
    {
        private readonly StringBuilder _sb = new();
        public string Output => _sb.ToString();

        public Task WriteAsync(ReadOnlyMemory<byte> frameBytes, CancellationToken cancellationToken)
        {
            _sb.Append(Encoding.UTF8.GetString(frameBytes.Span));
            return Task.CompletedTask;
        }
    }

    private static string StripAnsi(string s)
        => Regex.Replace(s, "\\[[0-9;?]*[A-Za-z]", string.Empty);

    [Fact]
    public async Task MinimalExample_RendersReactiveStateToAFrame()
    {
        // --- state (Signal) ---------------------------------------------------
        var count = new Signal<int>(0);

        // A binding derives display text from the signal, exactly as documented.
        var caption = new Binding<string>(() => $"Count: {count.Value}");
        Assert.Equal("Count: 0", caption.Get());

        // Simulate the input handler incrementing the signal.
        count.Value++;
        Assert.Equal("Count: 1", caption.Get());

        // --- rendering pipeline ----------------------------------------------
        var caps = CapabilityDetector.DetectFromEnvironment();
        var scheduler = new FrameScheduler();
        var pty = new BufferPty();
        var viewport = (Width: 40, Height: 10);

        // --- composition (DisplayListBuilder) --------------------------------
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, viewport.Width, viewport.Height));
        b.DrawRect(new Rect(0, 0, viewport.Width, viewport.Height, new Rgb24(0, 0, 0)));
        b.DrawBorder(new Border(2, 1, 32, 5, "single", new Rgb24(180, 180, 180)));
        b.DrawText(new TextRun(4, 2, "Andy.Tui counter", new Rgb24(200, 200, 50), null, CellAttrFlags.Bold));
        b.DrawText(new TextRun(4, 4, caption.Get(), new Rgb24(220, 220, 220), null, CellAttrFlags.None));
        b.Pop();

        await scheduler.RenderOnceAsync(b.Build(), viewport, caps, pty, CancellationToken.None);

        // The frame was encoded and written, and the reactive caption is visible.
        Assert.NotEqual(string.Empty, pty.Output);
        var plain = StripAnsi(pty.Output);
        Assert.Contains("Count: 1", plain);
        Assert.Contains("Andy.Tui counter", plain);
    }

    [Fact]
    public void Signal_Computed_And_Binding_HaveTheDocumentedApi()
    {
        // Signal<T>(initialValue), .Value get/set, ValueChanged event.
        var counter = new Signal<int>(0);
        int observed = -1;
        counter.ValueChanged += (_, value) => observed = value;

        // Computed<T>(Func<T>) recomputes on demand.
        var doubled = new Computed<int>(() => counter.Value * 2);

        counter.Value = 5;
        Assert.Equal(5, observed);

        doubled.Invalidate();
        Assert.Equal(10, doubled.Value);

        // Binding<T>(getter, setter?) reads and (optionally) writes back.
        var bound = new Binding<int>(() => counter.Value, v => counter.Value = v);
        Assert.True(bound.TrySet(7, out _));
        Assert.Equal(7, counter.Value);
        Assert.Equal(7, bound.Get());
    }
}
