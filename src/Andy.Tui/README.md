# Andy.Tui

[![NuGet](https://img.shields.io/nuget/vpre/Andy.Tui)](https://www.nuget.org/packages/Andy.Tui/)

A modern, reactive TUI (Terminal User Interface) framework for .NET 8+ with declarative component composition and reactive state management.

> ⚠️ **ALPHA RELEASE WARNING** ⚠️
>
> This software is in ALPHA stage. Public APIs are unstable and may change
> without notice. Do not depend on it in production.
>
> **Terminal-safety notes:** this library renders to the terminal and does
> **not** delete, move, or overwrite files. It writes ANSI/VT escape sequences to
> stdout and may switch the terminal into raw mode and the alternate screen
> buffer, so an app that exits abnormally can leave the terminal modified
> (no echo, hidden cursor, alternate screen). Pair terminal setup with cleanup,
> restore the terminal with `reset` or `stty sane` if needed, and sanitize
> untrusted text before displaying it.

## Features

- **Reactive Core**: Signals, computed values, and effects for state management
- **Component System**: Declarative component composition with modifiers
- **CSS Styling**: Subset of CSS with cascade, specificity, variables, and pseudo-classes
- **Flex Layout**: Modern flexbox-based layout engine
- **Rich Widgets**: 70+ rendering widgets including tables, charts, dialogs, and editors (see the [Widget Catalog](https://github.com/rivoli-ai/andy-tui2/blob/main/docs/WIDGETS.md))
- **Virtualization**: List and grid virtualization that renders only the visible window, independent of total item count
- **Cross-Platform**: Targets .NET 8 on Windows, macOS, and Linux terminals with ANSI/VT support (primary development and CI on macOS/Linux)

## Quick Start

Andy.Tui is immediate-mode: each frame you build a display list and hand it to
the frame scheduler, which composites, diffs, encodes, and writes it.

```csharp
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;   // CapabilityDetector, IPtyIo
using Andy.Tui.Core;               // FrameScheduler
using Andy.Tui.DisplayList;        // DisplayListBuilder and ops

var caps = CapabilityDetector.DetectFromEnvironment();
var scheduler = new FrameScheduler();
var viewport = (Console.WindowWidth, Console.WindowHeight);

var b = new DisplayListBuilder();
b.DrawText(new TextRun(2, 1, "Hello, TUI!", new Rgb24(0, 200, 0), null, CellAttrFlags.Bold));

await scheduler.RenderOnceAsync(b.Build(), viewport, caps, new StdoutPty(), CancellationToken.None);

// A tiny writer that sends encoded frame bytes to stdout.
// Top-level statements must precede type declarations, so this comes last.
sealed class StdoutPty : IPtyIo
{
    public Task WriteAsync(ReadOnlyMemory<byte> frameBytes, CancellationToken ct)
    {
        Console.Out.Write(Encoding.UTF8.GetString(frameBytes.Span));
        return Task.CompletedTask;
    }
}
```

See the [Getting Started guide](https://github.com/rivoli-ai/andy-tui2/blob/main/docs/GETTING_STARTED.md)
for terminal setup, input handling, reactive state, and failure-safe shutdown.

## Installation

```bash
dotnet add package Andy.Tui --prerelease
```

This single package contains every Andy.Tui assembly, including the CLI-focused
widgets. Component assemblies are not published as separate packages.

## Documentation

For full documentation, examples, and API reference, visit:
https://github.com/rivoli-ai/andy-tui2

## License

Apache-2.0 License

## Support

- Issues: https://github.com/rivoli-ai/andy-tui2/issues
- Discussions: https://github.com/rivoli-ai/andy-tui2/discussions
