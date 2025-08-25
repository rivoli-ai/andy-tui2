# Andy.Tui Documentation

## Quick Links

- 📖 [Getting Started](GETTING_STARTED.md) - Installation and basic usage
- 🏗️ [Architecture](ARCHITECTURE.md) - System design and components
- 🎨 [Widget Catalog](WIDGETS.md) - Complete list of UI components

## Overview

Andy.Tui is a modern terminal UI framework for .NET 8+ that brings reactive programming and declarative UI patterns to console applications.

## Key Features

- **Reactive State Management** - Signal-based reactivity system
- **Rich Widget Library** - 80+ pre-built components
- **CSS Styling** - Familiar styling with CSS subset
- **Flexbox Layout** - Modern layout system
- **Unicode Support** - Full Unicode text rendering
- **Cross-Platform** - Windows, macOS, Linux support

## Documentation Structure

### For New Users
Start with [Getting Started](GETTING_STARTED.md) to learn the basics and build your first TUI application.

### For Developers  
Read [Architecture](ARCHITECTURE.md) to understand the system design, rendering pipeline, and extension points.

### For Reference
Browse [Widget Catalog](WIDGETS.md) for the complete list of available UI components and their usage.

## Project Status

✅ **Completed Phases:**
- Phase 0: Foundations
- Phase 1: Visual Core  
- Phase 2: Rendering Core
- Phase 3: Interactivity & Animations
- Phase 4: Virtualization & Widgets

🚧 **In Progress:**
- Phase 5: Additional Backends (Web, Native)

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

## Contributing

See [CONTRIBUTING.md](../CONTRIBUTING.md) for guidelines.

## License

Apache-2.0 License. See [LICENSE](../LICENSE) for details.

---

> ⚠️ **ALPHA SOFTWARE** - APIs may change. Use at your own risk.