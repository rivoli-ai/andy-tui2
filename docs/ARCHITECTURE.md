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

## Color & Transparency

`Rgb24` is always an **opaque** 24-bit color. Transparency is *not* a color
value — it is the **absence** of one, represented by a `null` color. Both
foreground and background can be transparent:

| Type | Field(s) | `null` means |
| --- | --- | --- |
| `DisplayList.Rect` | `Fill` | Transparent fill — the rect paints nothing, leaving whatever is underneath. |
| `DisplayList.TextRun` | `Fg`, `Bg` | `Bg` null: keep the background under the glyph. `Fg` null: use the terminal's default text color. |
| `Compositor.Cell` | `Fg`, `Bg` | The cell carries no explicit color. An untouched cell (`default(Cell)`) is fully transparent. |
| `Compositor.RowRun` | `Fg`, `Bg` | The run carries no explicit foreground / background. |

### How it reaches the terminal

When the `AnsiEncoder` meets a run whose `Bg`/`Fg` is `null`, it emits the ANSI
**default reset** — `ESC[49m` for the background, `ESC[39m` for the foreground —
instead of an explicit `ESC[48;2;r;g;bm` / `ESC[38;2;r;g;bm`. The terminal then
paints those cells with its own configured colors — which is what lets a
**translucent terminal window** or a **custom terminal theme** show through your
UI. Transparency is honored at every color depth (truecolor, 256-color,
16-color); it is never down-converted to black.

The encoder also emits a single baseline `ESC[0m` for the first run of every
frame. Encoders are created per-frame and write absolute SGR state, so this
baseline prevents attributes/colors from a previous frame leaking into the next.

### Style layer

`Style.RgbaColor` carries an alpha channel. At render time alpha is binary:
`RgbaColor.Transparent` (and any `A == 0`, plus the CSS keywords `transparent`
and `none`) is transparent; anything else is opaque. Convert a style color to a
render color with `RgbaColor.ToRgb24()`, which returns `Rgb24?` — `null` when
transparent — ready to drop into a `Rect.Fill` or `TextRun.Bg`/`Fg`.

> Widgets currently construct `Rgb24` colors directly rather than resolving
> through the style system, so transparency is reached either via raw
> `DisplayList` ops or by converting a `RgbaColor` with `ToRgb24()`. Routing all
> widget rendering through `ResolvedStyle` is a separate, larger effort.

> **Common pitfall:** painting a full-screen root rect with an explicit color
> (e.g. `new Rect(0, 0, w, h, new Rgb24(0, 0, 0))`) makes the entire app opaque,
> defeating terminal transparency. Use a `null` fill for a transparent root, and
> only paint explicit backgrounds where you actually want an opaque surface.

See `examples/.../Demos/TransparentBackgroundDemo.cs` (menu option **65**) for a
runnable comparison of transparent vs. opaque roots.

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
├── Andy.Tui/                # Umbrella meta-package
├── Andy.Tui.Core/           # Reactive system
├── Andy.Tui.Compose/        # Composition / component tree
├── Andy.Tui.Style/          # CSS and styling
├── Andy.Tui.Layout/         # Layout engine
├── Andy.Tui.Text/           # Text processing
├── Andy.Tui.Widgets/        # Widget library
├── Andy.Tui.CliWidgets/     # CLI-focused widgets
├── Andy.Tui.DisplayList/    # Rendering commands
├── Andy.Tui.Compositor/     # Layer composition
├── Andy.Tui.Backend.Terminal/ # Terminal output
├── Andy.Tui.Input/          # Input handling
├── Andy.Tui.Animations/     # Animation system
├── Andy.Tui.Virtualization/ # List/table virtualization
└── Andy.Tui.Observability/  # Logging/debugging
```

## Design Principles

1. **Reactive First**: All state changes flow through the reactive system
2. **Declarative UI**: Describe what, not how
3. **Performance**: Every abstraction must justify its cost
4. **Testability**: Deterministic rendering for reliable tests
5. **Observability**: Built-in debugging and performance monitoring

## Future Roadmap

- **Accessibility**: Screen reader support
- **Theming**: Comprehensive theme system
- **Hot Reload**: Live UI updates during development