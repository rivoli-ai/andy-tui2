# Widget Catalog

This catalog lists the widgets that are actually implemented and public in the
source tree. Every entry below corresponds to a public type in
`src/Andy.Tui.Widgets/` (namespace `Andy.Tui.Widgets`) or
`src/Andy.Tui.CliWidgets/` (namespace `Andy.Tui.CliWidgets`).

> **Status:** ALPHA. Widget APIs are not stable and may change between
> releases. Types that were named in older drafts of this document but do not
> exist in source are listed under [Planned / not implemented](#planned--not-implemented).

## How widgets work

Widgets in `Andy.Tui.Widgets` are plain classes. They do **not** inherit from a
common `Widget` base class and there is no `IRenderContext`. Instead, a widget
exposes a `Render` method with the signature declared by
[`IRenderable`](../src/Andy.Tui.Widgets/IRenderable.cs):

```csharp
void Render(in Andy.Tui.Layout.Rect rect,
            Andy.Tui.DisplayList.DisplayList baseDl,
            Andy.Tui.DisplayList.DisplayListBuilder builder);
```

The widget appends draw operations for its `rect` to the supplied
`DisplayListBuilder`; the compositor and terminal backend turn the resulting
display list into terminal output. Widget state is set through constructors and
explicit `Set*` methods rather than public property setters.

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

## Display and text

- **Label** — single styled line of text (`Label.cs`)
- **RichText** — multi-run styled text (`RichText.cs`)
- **Link** — hyperlink-styled text (`Link.cs`)
- **LargeText** — oversized banner text, styles via `LargeTextStyle` (`LargeText.cs`)
- **FigletViewer** — FIGlet-style ASCII banner text (`FigletViewer.cs`)
- **Badge** / **TitleBadge** — small status labels (`Badge.cs`, `TitleBadge.cs`)
- **KeyValueList** — aligned key/value pairs (`KeyValueList.cs`)
- **CodeViewer** — monospace source display (`CodeViewer.cs`)
- **MarkdownRenderer** — renders Markdown to a display list (`MarkdownRenderer.cs`)

## Input

- **Button** — clickable button, `new Button(text)` (`Button.cs`)
- **TextInput** — single-line text entry (`TextInput.cs`)
- **Checkbox** — boolean toggle, `new Checkbox(label, checked)` (`Checkbox.cs`)
- **Toggle** — on/off switch, `new Toggle(on, label)` (`Toggle.cs`)
- **RadioGroup** — single selection from a group (`RadioGroup.cs`)
- **Slider** — numeric value selector (`Slider.cs`)
- **Select** — dropdown selection (`Select.cs`)
- **ColorChooser** — color selection (`ColorChooser.cs`)

## Containers and layout

- **Panel** — bordered content area (`Panel.cs`)
- **Card** — styled content card (`Card.cs`)
- **GroupBox** — titled bordered group (`GroupBox.cs`)
- **ScrollView** — scrollable viewport (`ScrollView.cs`)
- **Accordion** — collapsible sections (`Accordion.cs`)
- **Tabs** — tabbed interface (`Tabs.cs`)
- **Carousel** — paged content (`Carousel.cs`)
- **Splitter** — resizable split, `SplitterOrientation` (`Splitter.cs`)
- **DockLayout** — docking regions via `DockRegion` (`Dock.cs`)
- **Align** — alignment container, `HorizontalAlign` / `VerticalAlign` (`Align.cs`)
- **StackLayers** — z-ordered overlays (`StackLayers.cs`)
- **ResizeHandle** — drag-to-resize handle (`ResizeHandle.cs`)

## Data display

- **Table** — data grid with columns (`Table.cs`)
- **TreeTable** — hierarchical table (`TreeTable.cs`)
- **DataGrid** — column grid (`DataGrid.cs`)
- **ListBox** — selectable item list (`ListBox.cs`)
- **ListView** — item list view (`ListView.cs`)
- **TreeView** — hierarchical tree, `ITreeNode` / `Node` (`TreeView.cs`)
- **VirtualizedList** — windowed list for large collections (`VirtualizedList.cs`)
- **VirtualizedGrid** — windowed grid for large collections (`VirtualizedGrid.cs`)
- **Pager** — page navigation (`Pager.cs`)

## Charts and visualization

- **BarChart** (`BarChart.cs`), **LineChart** (`LineChart.cs`),
  **PieChart** (`PieChart.cs`), **ScatterPlot** (`ScatterPlot.cs`)
- **Sparkline** (`Sparkline.cs`), **Histogram** (`Histogram.cs`),
  **Heatmap** (`Heatmap.cs`), **BoxPlot** (`BoxPlot.cs`)
- **BulletChart** (`BulletChart.cs`), **Candlestick** (`Candlestick.cs`),
  **AsciiGraph** (`AsciiGraph.cs`)
- **Gauge** (`Gauge.cs`), **ProgressBar** (`ProgressBar.cs`)
- **GanttChart** (`GanttChart.cs`), **Timeline** (`Timeline.cs`),
  **MapView** (`MapView.cs`)

## Navigation and menus

- **MenuBar** — application menu bar, `Menu` / `MenuItem` (`MenuBar.cs`)
- **MenuPopup** — popup menu, `MenuBehaviorOptions` (`MenuPopup.cs`)
- **ContextMenu** — context menu (`ContextMenu.cs`)
- **CommandPalette** — searchable command list (`CommandPalette.cs`)
- **Breadcrumbs** — navigation trail (`Breadcrumbs.cs`)
- **HintPanel** — key-hint panel (`HintPanel.cs`)
- **FocusRing** — focus-order helper (`FocusRing.cs`)
- **Router** — view routing (`Router.cs`)

## Dialogs and overlays

- **ModalDialog** — modal dialog, `ModalResult` (`ModalDialog.cs`)
- **AboutDialog** — about box (`AboutDialog.cs`)
- **FileDialog** — file browser (read-only directory enumeration), `FileDialogMode` (`FileDialog.cs`)
- **Toast** — transient notification (`Toast.cs`)
- **Tooltip** — hover tooltip (`Tooltip.cs`)
- **PreferencesPanel** — settings panel (`PreferencesPanel.cs`)
- **FindReplacePanel** — find/replace UI (`FindReplacePanel.cs`)

## Feedback and status

- **StatusBar** — application status bar (`StatusBar.cs`)
- **Spinner** — loading indicator (`Spinner.cs`)
- **Bell** — terminal bell trigger (`Bell.cs`)

## Editors and views

- **EditorView** — text editor view (`EditorView.cs`)
- **DiffViewer** — side-by-side / unified diff (`DiffViewer.cs`)
- **ChatView** — chat transcript view (`ChatView.cs`)
- **RealTimeLogView** — streaming log view (`RealTimeLogView.cs`)

## CLI widgets (`Andy.Tui.CliWidgets`)

Widgets tailored to command-line / chat-style interfaces:

- **PromptLine** — input prompt line (`PromptLine.cs`)
- **CommandOutput** / **CommandOutputView** — command output rendering (`CommandOutput.cs`, `CommandOutputView.cs`)
- **FeedView** — scrolling feed of `IFeedItem` (user bubbles, code blocks, markdown, separators) (`FeedView.cs`)
- **KeyHintsBar** — key-hint bar (`KeyHintsBar.cs`)
- **MarkdownDisplay** — markdown output block (`MarkdownDisplay.cs`)
- **StatusMessage** — status text (`StatusMessage.cs`)
- **StatusLine** — single-line status text (`ToastStatus.cs`)
- **Toast** — transient CLI status toast (`ToastStatus.cs`)
- **TokenCounter** — token usage indicator (`TokenCounter.cs`)
- **ResponseSeparatorItem** — response divider (`ResponseSeparator.cs`)

## Usage example

The following pattern is exercised by the runnable samples in
[`examples/Andy.Tui.Examples/Program.cs`](../examples/Andy.Tui.Examples/Program.cs).
Widgets draw into a `DisplayListBuilder`; they do not manage their own layout or
event loop.

```csharp
using Andy.Tui.Widgets;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

// Construct widgets via their constructors and Set* methods.
var checkbox = new Checkbox("Receive updates", initial: true);
var button = new Button("Submit");
button.SetFocused(true);

// Build a base display list, then let each widget append to a builder.
var baseDl = new DL.DisplayListBuilder().Build();
var builder = new DL.DisplayListBuilder();

checkbox.Render(new L.Rect(2, 3, 40, 1), baseDl, builder);
button.Render(new L.Rect(2, 5, 12, 1), baseDl, builder);

// The compositor/backend turns builder.Build() into terminal output.
var displayList = builder.Build();
```

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

## Planned / not implemented

The following types appeared in earlier drafts of this catalog but are **not**
present in the source tree. They are aspirational and tracked under the
documentation-accuracy epic
([#18](https://github.com/rivoli-ai/andy-tui2/issues/18)). Do not reference them
in code:

- `TextField`, `TextArea` — use **TextInput** for single-line entry;
  multi-line editing is provided in a limited form by **EditorView**.
- `Container`, `FlexBox`, `Grid`, `Stack`, `Spacer`, `Divider`, `Gap` — layout
  is handled by the `Andy.Tui.Layout` engine and the container widgets above,
  not by dedicated widget types of these names.
- `Form`, `FormField`, `Validator`, `RequiredValidator`, `EmailValidator`,
  `ErrorMessage` — there is no form/validation widget layer. Input-validation
  primitives live in `Andy.Tui.Core` as `IValidator<T>` on data bindings, not as
  widgets.
- `DatePicker`, `TimePicker`, `ColorPicker`, `FilePicker`, `Calendar` — not
  implemented (**ColorChooser** and **FileDialog** are the closest existing
  widgets).
- `Terminal` — there is no embedded-terminal widget; `TerminalCapabilities` in
  `Andy.Tui.Backend.Terminal` is a capability-detection type, not a widget.
- `Widget` base class and `IRenderContext` — do not exist; see
  [How widgets work](#how-widgets-work).
- `Editor` (syntax-highlighting editor), `Kanban`, `NetworkIndicator`,
  `BatteryIndicator`, `ActivityIndicator`, `QRCode`, `Avatar`, `Image`,
  `Skeleton`, `Alert`, `ConfirmDialog`, `MessageBox`, `Pagination`, `Stepper`,
  `ToolBar`, `HUD`, `Console`, `Inspector`, `JsonViewer`, `Heading`,
  `Paragraph`, `Text`, `Code`, `Icon`, `Switch`, `RadioButton` — not
  implemented. Where a similar widget exists it is listed in the sections above
  (for example **RadioGroup** instead of `RadioButton`, **Toggle** instead of
  `Switch`).
