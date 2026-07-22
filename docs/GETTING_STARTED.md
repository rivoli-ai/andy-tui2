# Getting Started with Andy.Tui

> ⚠️ **ALPHA SOFTWARE.** Public APIs are still changing between releases. Pin an
> exact version and expect breaking changes. See
> [Alpha limitations](#alpha-limitations-and-supported-platforms) below.

Andy.Tui is an immediate-mode terminal UI library for .NET 8. Every frame you
describe what the screen should look like as a **display list**, and a
**frame scheduler** composites it, diffs it against the previous frame, encodes
the difference to ANSI, and writes it to the terminal.

The pipeline is: **state → display list → scheduler → PTY**.

All type and member names in this guide are taken from the shipped public API.
The minimal example is compiled and executed in CI by
[`tests/Andy.Tui.Core.Tests/GettingStartedExampleTests.cs`](../tests/Andy.Tui.Core.Tests/GettingStartedExampleTests.cs).

## Installation

```bash
dotnet new console -o MyTuiApp
cd MyTuiApp
dotnet add package Andy.Tui --prerelease
```

The `Andy.Tui` package bundles all framework assemblies used below, including
`Andy.Tui.Core` (`FrameScheduler`, `Signal<T>`), `Andy.Tui.DisplayList`
(`DisplayListBuilder` and its ops), `Andy.Tui.Backend.Terminal`
(`CapabilityDetector`, `IPtyIo`), and `Andy.Tui.CliWidgets`.

## Minimal example

Replace the generated `Program.cs` with the following. It renders a counter,
increments it on <kbd>Space</kbd>/<kbd>Enter</kbd>, and quits on
<kbd>Esc</kbd>/<kbd>Q</kbd>. It also restores the terminal on exit — even if an
exception is thrown.

```csharp
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;   // CapabilityDetector, IPtyIo
using Andy.Tui.Core;               // FrameScheduler
using Andy.Tui.Core.Reactive;      // Signal<T>
using Andy.Tui.DisplayList;        // DisplayListBuilder and ops

// A tiny writer that sends encoded frame bytes to stdout.
// IPtyIo is the only thing FrameScheduler needs to talk to a terminal.
sealed class StdoutPty : IPtyIo
{
    public Task WriteAsync(ReadOnlyMemory<byte> frameBytes, CancellationToken cancellationToken)
    {
        Console.Out.Write(Encoding.UTF8.GetString(frameBytes.Span));
        Console.Out.Flush();
        return Task.CompletedTask;
    }
}

class Program
{
    static async Task Main()
    {
        // Detect true-color / 256-color support from TERM and COLORTERM.
        var caps = CapabilityDetector.DetectFromEnvironment();
        var scheduler = new FrameScheduler();
        var pty = new StdoutPty();

        // Reactive state: a signal is an observable value.
        var count = new Signal<int>(0);

        // Terminal setup: alternate screen buffer, hide cursor, disable line wrap.
        Console.Write("\u001b[?1049h\u001b[?25l\u001b[?7l");
        try
        {
            var running = true;
            var previousSize = (Width: -1, Height: -1);

            while (running)
            {
                // Viewport handling: read the current terminal size each frame.
                var viewport = (Width: Console.WindowWidth, Height: Console.WindowHeight);

                // On resize, force a full repaint so stale cells are cleared.
                if (viewport != previousSize)
                {
                    scheduler.SetForceFullClear(true);
                    previousSize = viewport;
                }
                else
                {
                    scheduler.SetForceFullClear(false);
                }

                // Input: drain the key buffer without blocking the render loop.
                while (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    if (key.Key is ConsoleKey.Escape or ConsoleKey.Q) { running = false; break; }
                    if (key.Key is ConsoleKey.Spacebar or ConsoleKey.Enter) count.Value++;
                }

                // Composition: describe the whole frame as a display list.
                var b = new DisplayListBuilder();
                b.PushClip(new ClipPush(0, 0, viewport.Width, viewport.Height));
                b.DrawRect(new Rect(0, 0, viewport.Width, viewport.Height, new Rgb24(0, 0, 0)));
                b.DrawBorder(new Border(2, 1, 32, 5, "single", new Rgb24(180, 180, 180)));
                b.DrawText(new TextRun(4, 2, "Andy.Tui counter", new Rgb24(200, 200, 50), null, CellAttrFlags.Bold));
                b.DrawText(new TextRun(4, 4, $"Count: {count.Value}", new Rgb24(220, 220, 220), null, CellAttrFlags.None));
                b.DrawText(new TextRun(2, viewport.Height - 1, "Space/Enter: +1   Esc/Q: quit", new Rgb24(160, 160, 160), null, CellAttrFlags.None));
                b.Pop();

                // Rendering: composite -> diff -> encode -> write one frame.
                await scheduler.RenderOnceAsync(b.Build(), viewport, caps, pty, CancellationToken.None);

                await Task.Delay(16); // ~60 FPS
            }
        }
        finally
        {
            // Shutdown: always restore line wrap, cursor, and main screen buffer.
            Console.Write("\u001b[?7h\u001b[?25h\u001b[?1049l");
        }
    }
}
```

Run it:

```bash
dotnet run
```

## How it fits together

### State: signals, computed values, and bindings

`Signal<T>` (namespace `Andy.Tui.Core.Reactive`) is an observable value.
`Computed<T>` derives a value from other signals. `Binding<T>` (namespace
`Andy.Tui.Core.Bindings`) wraps a getter and an optional setter.

```csharp
using Andy.Tui.Core.Reactive;
using Andy.Tui.Core.Bindings;

var counter = new Signal<int>(0);
counter.ValueChanged += (_, value) => { /* react to the new value */ };

var doubled = new Computed<int>(() => counter.Value * 2);

counter.Value = 5;      // raises ValueChanged
doubled.Invalidate();   // recompute on next read
_ = doubled.Value;      // 10

// A two-way binding over the same signal.
var bound = new Binding<int>(() => counter.Value, v => counter.Value = v);
bound.TrySet(7, out _); // counter.Value is now 7
```

### Composition: the display list

`DisplayListBuilder` (namespace `Andy.Tui.DisplayList`) records drawing ops.
The available ops are `Rect`, `Border`, `TextRun`, `ClipPush`, `LayerPush`, and
`Pop`. Colors are `Rgb24(byte R, byte G, byte B)`; text attributes come from the
`[Flags]` enum `CellAttrFlags` (`None`, `Bold`, `Faint`, `Italic`, `Underline`,
`Strikethrough`, `Reverse`, `Dim`, `Blink`, …).

```csharp
var b = new DisplayListBuilder();
b.PushClip(new ClipPush(0, 0, width, height));          // clip to a rectangle
b.DrawRect(new Rect(0, 0, width, height, new Rgb24(0, 0, 0)));   // fill
b.DrawBorder(new Border(2, 1, 30, 5, "single", new Rgb24(180, 180, 180)));
b.DrawText(new TextRun(4, 3, "Hello", new Rgb24(220, 220, 50), null, CellAttrFlags.Bold));
b.Pop();                                                // pop the clip
DisplayList frame = b.Build();
```

### Layout and widgets

Widgets in `Andy.Tui.Widgets` are immediate-mode: you construct one, set its
state with `SetX` methods, then call `Render(rect, baseDl, builder)` where
`rect` is an `Andy.Tui.Layout.Rect(double X, double Y, double Width, double Height)`,
`baseDl` is the frame built so far, and `builder` receives the widget's ops.

```csharp
using L = Andy.Tui.Layout;
using Andy.Tui.Widgets;

var baseDl = b.Build();          // everything drawn so far
var widgets = new DisplayListBuilder();

var panel = new Panel();
panel.SetTitle("Controls");
panel.Render(new L.Rect(2, 2, 40, 12), baseDl, widgets);

var button = new Button("Run");
button.SetFocused(true);
button.Render(new L.Rect(4, 4, 10, 1), baseDl, widgets);

var input = new TextInput();
input.SetText("hello");
input.SetFocused(true);
input.Render(new L.Rect(4, 6, 24, 1), baseDl, widgets);
```

You then concatenate the ops of `baseDl` and `widgets.Build()` into one display
list before rendering. See
[`examples/Andy.Tui.Examples/Program.cs`](../examples/Andy.Tui.Examples/Program.cs)
for a full multi-widget dashboard, and the `Demos/` folder for one file per
widget.

### Rendering and the scheduler

`FrameScheduler` (namespace `Andy.Tui.Core`) drives one frame:

```csharp
await scheduler.RenderOnceAsync(displayList, (width, height), caps, pty, CancellationToken.None);
```

- `caps` is a `TerminalCapabilities` from `CapabilityDetector.DetectFromEnvironment()`.
- `pty` is any `IPtyIo` — the one-method interface
  `Task WriteAsync(ReadOnlyMemory<byte> frameBytes, CancellationToken cancellationToken)`.
- `scheduler.SetForceFullClear(true)` forces a full repaint (use it after a
  resize); the scheduler otherwise writes only the cells that changed.
- `scheduler.SetMetricsSink(...)` accepts an `IFrameMetricsSink` (for example
  `Andy.Tui.Observability.HudOverlay`) for per-frame timing metrics.

### Terminal setup, viewport, and failure-safe shutdown

Andy.Tui does not take over the terminal for you — you own the escape sequences.
The example uses the common pattern:

- **Setup:** `\u001b[?1049h` (alternate screen) `\u001b[?25l` (hide cursor)
  `\u001b[?7l` (disable auto-wrap).
- **Viewport:** read `Console.WindowWidth` / `Console.WindowHeight` each frame;
  call `SetForceFullClear(true)` when the size changes.
- **Input:** poll `Console.KeyAvailable` and `Console.ReadKey(intercept: true)`.
- **Shutdown (failure-safe):** restore in a `finally` block —
  `\u001b[?7h` `\u001b[?25h` `\u001b[?1049l`. Because it is in `finally`, the
  terminal is restored even if the render loop throws.

## Alpha limitations and supported platforms

- **Alpha.** APIs, namespaces, and widget behavior can change between
  pre-release versions. Pin an exact version in production experiments.
- **Immediate mode only.** There is no retained component tree or automatic
  re-render on signal change: you rebuild the display list every frame and call
  `RenderOnceAsync` yourself. Signals notify via `ValueChanged`; wiring them to
  a redraw is your loop's responsibility.
- **You own the terminal lifecycle.** Setup, resize handling, and restoration
  are explicit (see above). Always restore in a `finally`.
- **Platforms.** Targets .NET 8 and runs on Windows, macOS, and Linux terminals
  with ANSI support. Color fidelity depends on the terminal — true color is used
  when `COLORTERM` indicates `truecolor`/`24bit`, otherwise a 256-color or basic
  palette is used, as determined by `CapabilityDetector.DetectFromEnvironment()`.

## Next steps

1. Browse runnable demos in
   [`examples/Andy.Tui.Examples`](../examples/Andy.Tui.Examples) —
   `dotnet run` there opens an interactive menu of every widget.
2. Read the [Architecture](ARCHITECTURE.md) guide for the full rendering
   pipeline.
3. See the [Widget Catalog](WIDGETS.md) for available components.
