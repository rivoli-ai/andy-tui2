# Getting Started with Andy.Tui

## Installation

```bash
dotnet add package Andy.Tui --prerelease
```

## Basic Concepts

### 1. Reactive State Management

Andy.Tui uses signals and computed values for state management:

```csharp
using Andy.Tui.Core;

// Create a signal (observable value)
var counter = new Signal<int>(0);

// Create computed values that auto-update
var doubled = new Computed<int>(() => counter.Value * 2);

// React to changes
counter.ValueChanged += (_, value) => Console.WriteLine($"Counter: {value}");

// Update the signal
counter.Value = 5; // Triggers update, doubled becomes 10
```

### 2. Components and Widgets

The framework provides 80+ built-in widgets:

```csharp
using Andy.Tui.Widgets;

// Create widgets
var label = new Label { Text = "Hello World" };
var button = new Button { Label = "Click Me" };
var table = new Table();

// Style widgets
label.Style.ForegroundColor = Color.Green;
button.Style.Border = BorderStyle.Rounded;
```

### 3. Layout System

Andy.Tui uses a flexbox-based layout system:

```csharp
using Andy.Tui.Layout;

// Flex layout properties
var container = new Container();
container.Style.FlexDirection = FlexDirection.Row;
container.Style.JustifyContent = JustifyContent.SpaceBetween;
container.Style.AlignItems = AlignItems.Center;
```

### 4. Terminal Rendering

The framework handles all terminal rendering automatically:

```csharp
using Andy.Tui.Backend.Terminal;

// Initialize terminal backend
var terminal = new TerminalBackend();
terminal.Initialize();

// Render your UI
// (Framework handles the rendering pipeline automatically)
```

## Simple Example

Here's a minimal working example:

```csharp
using Andy.Tui.Core;
using Andy.Tui.Widgets;
using Andy.Tui.Backend.Terminal;

// Create a simple counter app
var count = new Signal<int>(0);
var label = new Label();

// Bind label to signal
var binding = new Binding<int>(
    count,
    value => label.Text = $"Count: {value}"
);

// Create button to increment
var button = new Button { Label = "Increment" };
button.Clicked += () => count.Value++;

// Layout components
var container = new Container();
container.AddChild(label);
container.AddChild(button);

// Run the application
// (Note: Full application loop implementation varies)
```

## Key Features

- **Reactive Bindings**: Automatically update UI when data changes
- **Rich Widget Library**: Tables, forms, charts, progress bars, and more
- **CSS Styling**: Familiar styling with a subset of CSS
- **Unicode Support**: Full Unicode text rendering with grapheme clusters
- **Performance**: Optimized rendering with damage tracking and virtualization
- **Cross-Platform**: Works on Windows, macOS, and Linux terminals

## Next Steps

1. Explore the [Widget Catalog](WIDGETS.md) for available components
2. Learn about [Architecture](ARCHITECTURE.md) for deeper understanding
3. Check [API Reference](API_REFERENCE.md) for detailed documentation

## Important Notes

> ⚠️ This is ALPHA software. APIs may change. Use at your own risk.

The framework is actively being developed. Some features mentioned in examples may not be fully implemented yet. Always refer to the actual source code for the current API.