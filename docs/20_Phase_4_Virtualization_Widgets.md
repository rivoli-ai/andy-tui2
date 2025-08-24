# Phase 4 — Virtualization & Widgets (Clean, de-duplicated)

## Outcomes
- Virtualized list/grid with tunable overscan
- Real-time log viewer with high-throughput append
- Editor MVP capable of opening large files and basic edits
- Practical set of UI primitives with examples and tests

## Scope & Decisions
- Build imperative, fully working widgets first; add declarative/DSL later
- Focus on performance (damage tracking, fixed FPS), correctness, and comprehensive tests
- Unify rendering via DisplayList → Compositor → Backend pipeline

## Core Architecture
- Virtualization: `IVirtualizedCollection<T>`, `ViewportState`, `OverscanPolicy`
- Rendering: Widgets build `DisplayList` (rects, text, borders, clips)
- Compositor: Computes CellGrid + damage; encoder emits ANSI
- FrameScheduler: pacing, metrics, terminal I/O

## Implementation Tracker (W00–W32)
Legend: [x] done, [~] in progress, [ ] not started

- [ ] W00 Root/Screen (orchestrator)
- [x] W01 Panel (Window/Frame)
- [x] W02 VStack/HStack (Box layouts)
- [x] W03 ScrollView (non-virtualized)
- [x] W04 Button
- [x] W05 Checkbox
- [x] W06 RadioGroup
- [x] W07 Toggle / Switch
- [x] W08 TextInput (single line)
- [x] W09 ListBox (single-select)
- [x] W10 ProgressBar
- [x] W11 Slider
- [x] W12 VirtualizedList
- [~] W13 VirtualizedGrid (MVP + nav/highlight; column virt TBD)
- [x] W14 RealTimeLogView
- [x] W15 EditorView (MVP)
- [x] W16 Menu bar
- [x] W17 Menu/Submenu
- [x] W18 Context menu
- [x] W19 Command palette
- [x] W20 Select/Dropdown/ComboBox
- [x] W21 TreeView/Explorer
- [x] W22 Table
- [x] W23 LargeText / BigDigits
- [x] W24 Data grid (virtualized)
- [x] W25 Modal dialog (Confirm/Prompt)
- [x] W26 Toast/Notification
- [x] W27 Spinner/Busy indicator
- [x] W28 Sparklines (ASCII)
- [x] W29 Bar charts (ASCII)
- [x] W30 Label/Text (minimal)
- [x] W31 Link (OSC 8 hyperlinks when supported)
- [x] W32 Stack/Layers (z-index overlays)
- [x] W33 Splitter/Resizer (drag split panes)
- [x] W34 Tabs/TabView
- [x] W35 Collapsible/Accordion
- [x] W36 GroupBox/Fieldset
- [x] W37 Spacer/Expander/Align helpers
- [x] W38 Dock/Sidebar/Drawer (edge containers)
- [x] W39 Overlay layer (modal/lightbox surface)
- [x] W40 Status/Title bar (tmux-like)
- [x] W41 Breadcrumbs
- [x] W42 Router/Navigator (view switching + history)
- [x] W43 ListView (multi-select + virtualization as needed)
- [x] W44 Carousel/Stepper
- [x] W45 Focus ring/manager (visual/logic)
- [x] W46 Rich text/markup (bold/italic/color/links)
- [x] W47 Code viewer (syntax/line nums)
- [x] W48 Markdown renderer
- [x] W49 Diff viewer (side-by-side/inline)
- [x] W50 Tooltip/Popover
- [x] W51 Badge/Pill
- [x] W52 Hint/Help panel (keybindings)
- [x] W53 Tree table (hierarchical rows + columns)
- [x] W54 Key–Value/Description list
- [x] W55 Cards (header/body/footer)
- [x] W56 Timeline
- [x] W57 Line/Area charts
- [x] W58 Scatter plot
- [x] W59 Histogram
- [x] W60 Box plot
- [x] W61 Heatmap
- [x] W62 Pie/Donut
- [x] W63 Bullet chart
- [x] W64 Gauge/Meter
- [x] W65 Candlestick/OHLC
- [x] W66 Gantt
- [x] W67 Network/Graph (ASCII)
- [x] W68 Map/tile view
- [x] W69 File open/save dialog
- [x] W70 Find/Replace panel
- [x] W71 Preferences/Settings
- [x] W72 Color chooser
- [x] W73 About dialog
- [ ] ~~W74 Mouse handler (full routing of clicks/drags/wheel)~~
- [ ] ~~W75 Clipboard bridge (OSC 52)~~
- [x] W76 Hyperlink support (OSC 8 integration across widgets)
- [x] W77 Title/badge control
- [x] W78 Notifications/Bell
- [x] W79 Resize handle (live reflow)
- [x] W80 ANSI art/FIGlet viewer
- [ ] ~~W81 QR/barcode generator~~
- [ ] W82 Image viewer (SIXEL/kitty/iterm fallback)
- [ ] W83 Canvas/Drawing (braille/quarter-block)
- [ ] W84 REPL console
- [ ] W85 Terminal emulator pane
- [ ] W86 Process list monitor
- [ ] W87 Task runner panel
- [ ] W88 Debugger panes
- [ ] W89 Profiler/Perf view
- [ ] W90 Keymap viewer
- [ ] W91 Macro recorder
- [ ] W92 Search-in-view
- [ ] W93 Filter bar
- [ ] W94 Sorter (per-column)
- [ ] W95 Selection model (single/multi/range)
- [ ] W96 Reselect/Undo selection
- [ ] W97 Scrollbars (track/thumb + mouse)
- [ ] W98 Pagination controls
- [ ] W99 Inline validation
- [ ] W100 Placeholders/Skeletons

## Current Status Highlights

### W13 VirtualizedGrid
- Behavior: Realizes cells within viewport; fixed cell size MVP
- Input: Arrow/Left/Right move active cell; PgUp/PgDn, Home/End; mouse wheel scroll
- Rendering: Active cell highlight with background
- APIs: `SetDimensions`, `SetColumnWidths`, `SetCellTextProvider`, `SetActiveCell`, `MoveActiveCell`, `EnsureVisible`, `AdjustScroll`, `EnsureVisibleCols`
- Demo: VirtualizedGrid demo (menu 15) shows nav + wheel scrolling
- Next: Column virtualization for very wide grids; additional tests (windowing, horizontal reveal)

### W24 Data grid (virtualized)
- Behavior: Virtualized rows with column headers and active-cell highlight
- Input: Arrow keys move active cell; PgUp/PgDn/Home/End; mouse wheel scroll
- APIs: `SetColumns(headers,widths)`, `SetRowCount`, `SetCellTextProvider`, `SetActiveCell`, `MoveActiveCell`, `EnsureVisible`, `AdjustScroll`
- Demo: Data Grid demo (menu 28)
- Tests: Basic header + active-cell render test (expand with perf/windowing)
- Next: Column virtualization; editing hooks (cell editors), selection model integration

### W25 Modal dialog (Confirm/Prompt)
- Plan: Overlay layer with focus trap; title, body text, buttons (OK/Cancel), optional input prompt
- Input: Enter = confirm, Esc = cancel; Tab cycles focus within dialog
- Rendering: Centered panel with border; dimmed backdrop; z-overlays via `StackLayers`
- APIs: `ShowConfirm(title,message)`, `ShowPrompt(title,message,default)` returning result
- Tests: Focus trap, dismissal by Esc/Enter, layering order, viewport clamping
- Status: Not implemented (next)

## Demos (menu entries)
- 1–22: Core widgets and examples (Hello, Buttons, Toggle, Checkbox, Radio, TextInput, ScrollView, Progress/Slider, Log, Chat, ListBox, MenuBar/Menus, TreeView, Table, VirtualizedGrid, International text, EditorView, Context Menu, Select, Command Palette, LargeText Clock, ASCII Art)
- 23: Pager/Toast/Spinner (basics)
- 24: Sparklines (ASCII)
- 25: Bar Chart (ASCII)
- 26: Link (underlined; OSC 8)
- 27: Layers (stack/overlay)
- 28: Data Grid (virtualized)

## Testing & Perf
- Unit tests exist for: Tables, Menus/Popups, Select, Command Palette, LargeText, VirtualizedGrid basics, Pager/Toast/Spinner, DataGrid basics, FrameScheduler pacing
- Perf gates (next):
  - Log: ≥5k lines/s within dirty% budget
  - Grid: 10k×30 scroll targets (p50 ≤ 8ms, p90 ≤ 14ms)
  - Editor: open 10MB ≤ 300ms; typing p95 ≤ 10ms

## Observability
- HUD overlay: FPS, dirty %, bytes/frame, CPU samples
- Comprehensive logs available in tests and diagnostics

## Notes
- All widgets render through DisplayList and respect clipping
- Demos use a shared `TerminalHelpers.PollResize` and consistent FrameScheduler pacing (30 FPS where applicable)
- Link emits OSC 8 hyperlinks on capable terminals (Ctrl+Click / Enter)
