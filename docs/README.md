# Andy.Tui Documentation

## Quick Links

- 📖 [Getting Started](GETTING_STARTED.md) - Installation and basic usage
- 🏗️ [Architecture](ARCHITECTURE.md) - System design and components
- 🎨 [Widget Catalog](WIDGETS.md) - Implemented widgets, mapped to their source files
- 📦 [NuGet Package Cleanup](NUGET_PACKAGE_CLEANUP.md) - One-time component-package retirement runbook

## Overview

Andy.Tui is a modern terminal UI framework for .NET 8+ that brings reactive programming and declarative UI patterns to console applications.

## Key Features

- **Reactive State Management** - Signal-based reactivity system
- **Rich Widget Library** - 70+ rendering widgets ([catalog](WIDGETS.md))
- **CSS Styling** - Familiar styling with a CSS subset
- **Flexbox Layout** - Modern layout system
- **Unicode Support** - Grapheme-aware text rendering
- **Cross-Platform** - Targets .NET 8 on Windows, macOS, and Linux terminals

## Documentation Structure

### For New Users
Start with [Getting Started](GETTING_STARTED.md) to learn the basics and build your first TUI application.

### For Developers  
Read [Architecture](ARCHITECTURE.md) to understand the system design, rendering pipeline, and extension points.

### For Reference
Browse [Widget Catalog](WIDGETS.md) for the complete list of available UI components and their usage.

## Project Status

🟢 **Implemented (covered by the test suite):**
- Phase 0: Foundations
- Phase 1: Visual Core
- Phase 2: Rendering Core
- Phase 3: Interactivity & Animations
- Phase 4: Virtualization & Widgets

🚧 **In progress / planned:**
- Phase 5: Additional Backends (only the terminal backend ships today)
- Documentation, test-quality, and API-accuracy hardening — epic [#18](https://github.com/rivoli-ai/andy-tui2/issues/18)

## Example

```csharp
using Andy.Tui.Core;
using Andy.Tui.Widgets;

// Create reactive state
var counter = new Signal<int>(0);

// Create UI bound to state
var label = new Label();
var binding = new Binding<int>(counter, v => label.Text = $"Count: {v}");

// Handle interactions
var button = new Button { Label = "Increment" };
button.Clicked += () => counter.Value++;
```

## License

Apache-2.0 License. See [LICENSE](../LICENSE) for details.

---

> ⚠️ **ALPHA SOFTWARE** - APIs may change. Use at your own risk.
