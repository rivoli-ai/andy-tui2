# Andy.Tui Architecture

## Overview

Andy.Tui follows a unidirectional data flow with a clear separation of concerns across its rendering pipeline.

## Core Architecture

### Rendering Pipeline

```
User Input → State Update → Compose → Style → Layout → Display List → Compositor → Terminal Output
```

1. **Compose**: Build the widget tree from components
2. **Style**: Apply CSS rules and compute final styles  
3. **Layout**: Calculate positions and sizes using flexbox
4. **Display List**: Generate optimized drawing commands
5. **Compositor**: Merge layers and handle damage tracking
6. **Backend**: Render to terminal using ANSI escape sequences

### Key Components

#### Reactive Core (`Andy.Tui.Core`)
- **Signals**: Observable values that trigger updates
- **Computed**: Derived values that auto-recalculate
- **Effects**: Side effects that run on changes
- **Bindings**: Connect data to UI properties

#### Style System (`Andy.Tui.Style`)
- CSS parser and resolver
- Cascade and specificity rules
- CSS variables and pseudo-classes
- Theme support

#### Layout Engine (`Andy.Tui.Layout`)
- Flexbox implementation
- Constraint-based sizing
- Text measurement and wrapping
- RTL/Bidi support (planned)

#### Widget System (`Andy.Tui.Widgets`)
- 80+ pre-built components
- Composite widget patterns
- State management helpers
- Form validation

#### Terminal Backend (`Andy.Tui.Backend.Terminal`)
- ANSI/VT100 escape sequences
- 24-bit color support
- Mouse input handling
- Terminal capability detection

## Data Flow

```csharp
// 1. State Change
signal.Value = newValue;

// 2. Triggers Binding
binding.Update();

// 3. Updates Widget
widget.Property = transformedValue;

// 4. Marks Dirty
widget.InvalidateLayout();

// 5. Render Cycle
scheduler.RequestFrame();
```

## Performance Optimizations

### Damage Tracking
Only redraws changed regions of the screen, minimizing terminal output.

### Virtualization
Large lists and tables only render visible items, supporting millions of rows.

### Command Batching
Groups terminal commands to reduce syscall overhead.

### Layout Caching
Reuses layout calculations when possible.

## Threading Model

- **Main Thread**: UI updates and event handling
- **Render Thread**: Terminal output (when supported)
- **Worker Threads**: Background computations

## Extensibility Points

### Custom Widgets
```csharp
public class MyWidget : Widget
{
    protected override void OnRender(IRenderContext context)
    {
        // Custom rendering logic
    }
}
```

### Custom Styles
```csharp
StyleSheet.Global.AddRule(".my-class", new Style
{
    ForegroundColor = Color.Blue,
    Border = BorderStyle.Double
});
```

### Custom Backends
Implement `IBackend` to support new output targets (e.g., HTML, GUI frameworks).

## Directory Structure

```
src/
├── Andy.Tui.Core/           # Reactive system
├── Andy.Tui.Style/          # CSS and styling
├── Andy.Tui.Layout/         # Layout engine
├── Andy.Tui.Text/           # Text processing
├── Andy.Tui.Widgets/        # Widget library
├── Andy.Tui.DisplayList/    # Rendering commands
├── Andy.Tui.Compositor/     # Layer composition
├── Andy.Tui.Backend.Terminal/ # Terminal output
├── Andy.Tui.Input/          # Input handling
├── Andy.Tui.Animations/     # Animation system
└── Andy.Tui.Observability/  # Logging/debugging
```

## Design Principles

1. **Reactive First**: All state changes flow through the reactive system
2. **Declarative UI**: Describe what, not how
3. **Performance**: Every abstraction must justify its cost
4. **Testability**: Deterministic rendering for reliable tests
5. **Observability**: Built-in debugging and performance monitoring

## Future Roadmap

- **Web Backend**: Render to HTML/Canvas
- **Native Backend**: Direct GUI framework integration
- **Accessibility**: Screen reader support
- **Theming**: Comprehensive theme system
- **Hot Reload**: Live UI updates during development