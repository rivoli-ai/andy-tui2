# Phase 4 — Virtualization & Widgets (Week 11–14)

## Outcomes
- Virtualized list/grid with tunable overscan
- Real-time log viewer with high-throughput append
- Editor MVP capable of opening large files and basic edits

## Scope
- High-value widgets built atop virtualization and rendering core

## Non-goals
- Full editor feature parity (search, LSP) — MVP only

## Deliverables
- `src/Andy.Tui.Virtualization`, `src/Andy.Tui.Widgets`
- Performance scenarios and budgets implemented in tests

## Architecture Decisions
- Virtualization API: item provider, viewport measurements, overscan strategy
- Widget contracts: separation of model, view, and rendering responsibilities

## API & Types
- Virtualization: `IVirtualizedCollection<T>`, `ViewportState`, `OverscanPolicy`
- Widgets: `VirtualizedList`, `VirtualizedGrid`, `RealTimeLogView`, `EditorView`

## Tasks & Sequencing
- [ ] Implement virtualization engine and integrate with compose/layout
- [ ] Build VirtualizedList and VirtualizedGrid
- [ ] Build RealTimeLogView and perf-tune append path
- [ ] Build Editor MVP (buffer, viewport, basic edits)

## Testing
- Perf: targets from Performance Acceptance Plan
- Integration: scroll, resize, append; editor typing latency
- Soak: long-running log appends without memory growth

## Observability
- HUD shows dirty %, queue depth; capture & replay scenarios

## Perf Gates
- Log append ≥5k lines/s ≤25% single core; 1 write/frame; dirty ≤12%
- Grid 10k×30 scroll 120 rows/s; p50 ≤ 8ms, p90 ≤ 14ms
- Editor open 10MB ≤300ms; typing p95 ≤ 10ms

## Exit Criteria
- All scenario budgets met in CI; stability across 1k deterministic runs

## Risks & Mitigations
- Overscan mis-tuning → adaptive policies and tests
- Log burst drops → coalescing and back-pressure
