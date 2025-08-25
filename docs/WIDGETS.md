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

## Widget Properties

Most widgets share common properties:

- **Style** - CSS-like styling
- **Visible** - Show/hide widget
- **Enabled** - Enable/disable interaction
- **Width/Height** - Size constraints
- **Margin/Padding** - Spacing
- **Id** - Unique identifier
- **Class** - CSS class names

## Custom Widgets

Create custom widgets by inheriting from `Widget`:

```csharp
public class CustomWidget : Widget
{
    protected override void OnRender(IRenderContext context)
    {
        // Custom rendering logic
        context.DrawText(0, 0, "Custom Widget");
    }
    
    protected override Size OnMeasure(Size available)
    {
        // Return desired size
        return new Size(20, 1);
    }
}
```