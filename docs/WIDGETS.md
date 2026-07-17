# Widget Catalog

## Basic Widgets

### Display
- **Label** - Static text display
- **Text** - Styled text with formatting
- **Heading** - H1-H6 heading levels
- **Paragraph** - Multi-line text block
- **Code** - Monospace code display
- **Icon** - Unicode/emoji icons

### Input
- **Button** - Clickable button with label
- **TextField** - Single-line text input
- **TextArea** - Multi-line text input
- **Checkbox** - Boolean toggle
- **RadioButton** - Single selection from group
- **Slider** - Numeric value selector
- **Switch** - On/off toggle

### Containers
- **Container** - Basic layout container
- **ScrollView** - Scrollable viewport
- **Panel** - Bordered content area
- **Card** - Styled content card
- **Accordion** - Collapsible sections
- **TabView** - Tabbed interface

## Data Display

### Lists & Tables
- **List** - Vertical item list
- **Table** - Data grid with columns
- **TreeView** - Hierarchical tree display
- **DataGrid** - Advanced table with sorting/filtering
- **ListView** - Virtualized list for large datasets

### Visualization
- **ProgressBar** - Progress indicator
- **Gauge** - Circular progress
- **BarChart** - Vertical/horizontal bars
- **LineChart** - Line graph
- **SparkLine** - Inline mini-chart
- **HeatMap** - Density visualization

## Layout Widgets

### Spacing
- **Spacer** - Flexible space
- **Divider** - Horizontal/vertical separator
- **Gap** - Fixed space

### Arrangement
- **Stack** - Vertical/horizontal stacking
- **Grid** - Grid-based layout
- **FlexBox** - Flexible box layout
- **Dock** - Docking panel layout

## Forms

### Controls
- **Form** - Form container with validation
- **FormField** - Labeled form input
- **Select** - Dropdown selection
- **DatePicker** - Date selection
- **TimePicker** - Time selection
- **ColorPicker** - Color selection
- **FilePicker** - File path selection

### Validation
- **Validator** - Input validation rules
- **ErrorMessage** - Validation error display

## Feedback

### Notifications
- **Alert** - Alert message box
- **Toast** - Temporary notification
- **Badge** - Status indicator
- **Spinner** - Loading indicator
- **Skeleton** - Loading placeholder

### Dialogs
- **Dialog** - Modal dialog
- **ConfirmDialog** - Confirmation prompt
- **MessageBox** - Message display

## Navigation

- **Menu** - Dropdown/context menu
- **MenuBar** - Application menu bar
- **Breadcrumb** - Navigation trail
- **Pagination** - Page navigation
- **Stepper** - Step-by-step wizard

## Advanced

### Debugging
- **HUD** - Performance overlay
- **Console** - Debug console
- **Inspector** - Widget inspector

### Specialized
- **Terminal** - Embedded terminal
- **Editor** - Text editor with syntax highlighting
- **Calendar** - Calendar view
- **Kanban** - Kanban board
- **Gantt** - Gantt chart
- **Markdown** - Markdown renderer
- **JsonViewer** - JSON tree viewer

## Status Indicators

- **StatusBar** - Application status bar
- **ToolBar** - Tool button bar
- **ActivityIndicator** - Activity status
- **NetworkIndicator** - Network status
- **BatteryIndicator** - Battery level

## Media

- **Image** - Image display (ASCII/Unicode art)
- **Avatar** - User avatar
- **QRCode** - QR code generator

## Usage Example

```csharp
// Create a simple form
var form = new Form();

var nameField = new FormField
{
    Label = "Name:",
    Input = new TextField { Placeholder = "Enter name" }
};

var emailField = new FormField  
{
    Label = "Email:",
    Input = new TextField { Placeholder = "user@example.com" }
};

var submitButton = new Button { Label = "Submit" };

form.AddChild(nameField);
form.AddChild(emailField);
form.AddChild(submitButton);

// Add validation
nameField.Validator = new RequiredValidator();
emailField.Validator = new EmailValidator();

// Handle submission
submitButton.Clicked += () =>
{
    if (form.Validate())
    {
        // Process form data
    }
};
```

## Widget Runtime Contract

All built-in widgets implement the single composable contract `IWidget`
(`src/Andy.Tui.Widgets/IWidget.cs`), which extends the minimal `IRenderable`
render contract (`src/Andy.Tui.Widgets/IRenderable.cs`). Because they share this
contract, widgets nest directly inside the stack and container widgets
(`VStack`, `HStack` in `src/Andy.Tui.Widgets/Layout/StackPanel.cs`) without an
adapter.

The contract unifies:

- **Measurement** - `Size Measure(Size available)`
- **Rendering** - `void Render(in Rect rect, DisplayList baseDl, DisplayListBuilder builder)`
- **Identity** - `string? Key` (assign with `WithKey(...)`)
- **Focusability** - `bool Focusable`, `bool IsFocused`, `SetFocused(bool)`
- **Input handling** - `bool HandleInput(IInputEvent ev)`
- **Disabled state** - `bool IsEnabled`, `SetEnabled(bool)`
- **Visible state** - `bool IsVisible`, `SetVisible(bool)` (hidden widgets emit nothing and are skipped by stacks)
- **Style hooks** - `WidgetStyle? Style`, `SetStyle(WidgetStyle?)` to override theme colours per widget
- **Invalidation** - `event Action Invalidated`, `Invalidate()`

## Custom Widgets

Create custom widgets by inheriting from `WidgetBase`
(`src/Andy.Tui.Widgets/WidgetBase.cs`), which supplies the shared runtime
behaviour. Implement `RenderCore` and, when the intrinsic size matters,
`MeasureCore`:

```csharp
using Andy.Tui.Widgets;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

public sealed class CustomWidget : WidgetBase
{
    protected override void RenderCore(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        // Called only when visible with a positive-area rectangle.
        builder.DrawText(new DL.TextRun(
            (int)rect.X, (int)rect.Y, "Custom Widget",
            ResolveForeground(new DL.Rgb24(200, 200, 200)), null, DL.CellAttrFlags.None));
    }

    protected override L.Size MeasureCore(L.Size available) => new(13, 1);

    // Optionally override Focusable and HandleInputCore for interactive widgets.
    public override bool Focusable => true;
}
```

The base class already implements visibility, enabled/focus state, style
resolution (`ResolveForeground` / `ResolveBackground`), and invalidation, so a
custom widget only describes how it paints and measures.

### Adapting external rendering

For rendering owned outside the widget runtime, wrap it with `WidgetAdapter`
(`src/Andy.Tui.Widgets/WidgetAdapter.cs`) so it can still be nested in stacks:

```csharp
IWidget widget = WidgetAdapter.FromRender(
    (in L.Rect r, DL.DisplayList _, DL.DisplayListBuilder b) =>
        b.DrawText(new DL.TextRun((int)r.X, (int)r.Y, "external", fg, null, DL.CellAttrFlags.None)),
    desired: new L.Size(8, 1));
```