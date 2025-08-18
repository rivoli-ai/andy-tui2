# Phase 4 — Virtualization & Widgets (Week 11–14)

## Outcomes
- Virtualized list/grid with tunable overscan
- Real-time log viewer with high-throughput append
- Editor MVP capable of opening large files and basic edits

Decision: Defer declarative/SwiftUI-style DSL. Build imperative, fully working widgets first, then layer a declarative API on top in a later phase. Quality and performance take precedence over API sugar in this phase.

## Scope
- High-value widgets built atop virtualization and rendering core

Non-Goals in this phase
- Declarative/SwiftUI-style widget DSL (tracked for a later phase)
- Complex editor features (syntax tree, LSP, multi-cursor, search/replace) — focus on robust MVP

## Non-goals
- Full editor feature parity (search, LSP) — MVP only

## Deliverables
- `src/Andy.Tui.Virtualization`, `src/Andy.Tui.Widgets`
- Performance scenarios and budgets implemented in tests

## Architecture Decisions
- Virtualization API: item provider, viewport measurements, overscan strategy
- Widget contracts: separation of model, view, and rendering responsibilities

Key concepts
- **ItemProvider**: `IVirtualizedCollection<T>` exposes `Count` and `GetItem(index)`; optionally asynchronous `GetItemAsync` for streaming sources
- **ViewportState**: visible window in rows/cols/px; includes scroll offset and size
- **Measuring**: per-item measurement strategy (`Fixed`, `Estimated + refine`, `Content-driven`) with cached measurements
- **Overscan**: `OverscanPolicy { Before, After, Mode(Fixed|Adaptive) }`
- **Renderer**: `IItemRenderer<T>` that builds DisplayList for a given item into a provided `DisplayListBuilder` with a target rect
- **Recycler**: pooling/reuse of per-item render state to reduce allocations
- **Diffing**: key-based diff for item identity; cache invalidation policies

## API & Types
- Virtualization: `IVirtualizedCollection<T>`, `ViewportState`, `OverscanPolicy`
- Widgets: `VirtualizedList`, `VirtualizedGrid`, `RealTimeLogView`, `EditorView`

Proposed signatures (indicative)
- `public interface IVirtualizedCollection<T> { int Count { get; } T this[int index] { get; } string GetKey(int index); }`
- `public readonly record struct ViewportState(int FirstRow, int RowCount, int Cols, int Rows, int PixelWidth, int PixelHeight);`
- `public readonly record struct OverscanPolicy(int Before, int After, bool Adaptive);`
- `public interface IItemRenderer<T> { void Render(in T item, int index, in Andy.Tui.Layout.Rect slot, DisplayList.DisplayList baseDl, DisplayList.DisplayListBuilder builder); }`
- `public sealed class VirtualizedList<T>` and `VirtualizedGrid<T>` host virtualization engine and expose imperative APIs for scroll, data change, and selection/focus integration.

## Tasks & Sequencing

### A) Virtualization Engine (core)
- [ ] Implement `ViewportComputer` to map scroll offset → visible indices
- [ ] Implement `MeasureCache` with fixed/estimated/content-driven modes
- [ ] Implement `OverscanComputer` (fixed and adaptive based on scroll velocity)
- [ ] Implement `Recycler` and key-based diffing/invalidation
- [ ] Integrate with layout: allocate slots (`Andy.Tui.Layout.Rect`) per realized item
- [ ] Build DL for realized window only; pipe to compositor; tests for correctness and perf

### B) Widgets — VirtualizedList
- [ ] Single-column list; variable row heights supported
- [ ] Selection/focus integration (arrow keys, page up/down, home/end)
- [ ] Keyboard and mouse wheel scrolling; smooth scroll optional
- [ ] Imperative API: `ScrollToIndex`, `EnsureVisible`, `SetItems`, `Invalidate(indexes)`
- [ ] Tests: measurement accuracy, overscan correctness, scroll perf gates

### C) Widgets — VirtualizedGrid
- [ ] Multi-column grid; fixed row height and col width to start; variable later
- [ ] Keyboard navigation (arrows, page keys), mouse wheel; selection rectangle optional
- [ ] Column virtualization (if needed) for very wide grids
- [ ] Imperative API: `ScrollToCell(row,col)`, `EnsureVisible`, `SetItems`
- [ ] Tests: large grid scroll perf, dirty% within budget, correctness under resize

### D) RealTimeLogView
- [ ] Append API with high-throughput path: `AppendLine(string line)` and `AppendBatch(IEnumerable<string> lines)`
- [ ] Back-pressure & coalescing: batch recent appends into frames up to throughput gate
- [ ] Search highlight (optional), tail-follow, pause/resume
- [ ] Tests: sustained appends (≥5k lines/s), memory growth (no leaks), dirty% ≤ 12%

### E) EditorView (MVP)
- [ ] Rope/segment buffer to support large files
- [ ] Viewport with virtual lines; basic cursor movement; insert/delete; simple undo stack
- [ ] Lazy line decoding (UTF-8), optional basic syntax coloring (token heuristic)
- [ ] Imperative API: `Open(path)`, `Insert`, `Delete`, `Save`, `Goto(line,col)`
- [ ] Tests: open 10MB ≤ 300ms, typing latency p95 ≤ 10ms, correctness for edits/scroll/resize

## Testing
- Perf: targets from Performance Acceptance Plan
- Integration: scroll, resize, append; editor typing latency
- Soak: long-running log appends without memory growth

Additional
- Unit: viewport computation, overscan policies, measure cache invalidation, recycler reuse
- Integration: virtualization + compositor dirty% and bytes/frame budgets
- Golden: deterministic fixtures for list/grid arrangements under variable sizes

## Observability
- HUD shows dirty %, queue depth; capture & replay scenarios

Details
- Add counters: realized items, overscan before/after, recycler hits/misses
- Queue depth for log appends and editor input events; bytes/frame and FPS already present
- Optional Chrome Trace slices for virtualization steps (measure, realize, recycle)

## Perf Gates
- Log append ≥5k lines/s ≤25% single core; 1 write/frame; dirty ≤12%
- Grid 10k×30 scroll 120 rows/s; p50 ≤ 8ms, p90 ≤ 14ms
- Editor open 10MB ≤300ms; typing p95 ≤ 10ms

List perf
- 100k items list, variable heights (mean 1.0, σ 0.3): scroll 120 rows/s; p50 ≤ 8ms, p90 ≤ 14ms; dirty ≤ 12%

## Exit Criteria
- All scenario budgets met in CI; stability across 1k deterministic runs

## Risks & Mitigations
- Overscan mis-tuning → adaptive policies and tests
- Log burst drops → coalescing and back-pressure

## Widget Specs (Detailed)

### VirtualizedList
- **Behavior**: Only items within viewport ± overscan are realized. Variable heights supported via measurement cache.
- **Input**: Arrow keys move selection; PgUp/PgDn scrolls by viewport; Home/End jump.
- **Rendering**: Each realized item renders into a slot rect. Background and selection highlight applied.
- **Selection/Focus**: Single-select MVP; multi-select later. Integrates with `FocusManager`.
- **APIs**: `SetItems`, `SetItemRenderer`, `ScrollToIndex`, `EnsureVisible`, events (`SelectionChanged`).
- **Tests**: Realization correctness under fast scroll; overscan reduces thrash; recycler reuse rate.

### VirtualizedGrid
- **Behavior**: Realizes cells within viewport window; fixed cell size MVP; optional column virtualization.
- **Input**: Arrow keys move active cell; PgUp/PgDn, Home/End; wheel scroll.
- **Rendering**: Grid lines optional; cell content via renderer.
- **APIs**: `SetGridProvider(rows, cols)`, `SetCellRenderer`, `ScrollToCell`, `EnsureVisible`.
- **Tests**: Large matrix perf; windowing and overscan; resize stability.

### RealTimeLogView
- **Behavior**: Appends accumulate; view can follow tail or pause. Coalesce appends within a frame.
- **Input**: Wheel/keys to scroll; space to toggle follow; `/` to search (later).
- **Rendering**: Lines rendered as text runs; optional color parse for ANSI within lines.
- **APIs**: `AppendLine`, `AppendBatch`, `Pause`, `Resume`, `FollowTail(bool)`.
- **Tests**: Throughput, memory, tail-follow correctness, scroll behavior under heavy append.

### EditorView (MVP)
- **Behavior**: Opens large files; supports basic edits and scrolling.
- **Input**: Arrow keys, page keys, insert/delete, enter; save; basic selection later.
- **Rendering**: Line numbers optional; current line highlight; simplistic token coloring optional.
- **APIs**: `Open`, `Save`, `Insert`, `Delete`, `Goto`, `GetViewportText()`.
- **Tests**: Open latency, typing latency, correctness of edits, scroll under large files.

## Salvage Plan from legacy `~/devel/rivoli-ai/andy-tui`
- Virtualization patterns and list/grid skeletons from `Andy.TUI.Declarative` (adapt contracts to imperative widgets)
- Log viewer append/coalescing logic from `Andy.TUI.Terminal` testbeds
- Rope/segment buffer utilities (if present) for editor; otherwise implement a minimal rope
- Input navigation helpers for lists/grids
- Color parsing for ANSI within log lines

Notes
- Do not port the declarative renderer yet. Reuse only algorithms and data structures; wire them to Phase 2/3 pipeline (DL→Compositor→Backend).
- Ensure namespaces updated to `Andy.Tui.*` and remove any direct rendering paths that bypass DL/Compositor.

## Plan & Milestones (suggested)
- Week 11: Virtualization core (viewport, overscan, measure, recycler) + tests
- Week 12: VirtualizedList (variable heights) + perf gates; start VirtualizedGrid
- Week 13: Grid perf + RealTimeLogView throughput/coalescing; soak tests
- Week 14: Editor MVP (open/scroll/edits), polish, docs; revisit remaining budgets
