# Phase 2 — Rendering Core (Week 6–8)

## Outcomes
- Unified rendering pipeline implemented and validated
- Display list ops defined; compositor and damage tracking operational
- Terminal backend encoder and PTY I/O abstraction ready
- Observability baseline: structured logs and tracing spans across stages

## Scope
- Unify rendering per architecture issues: eliminate dual visitor/RenderElement paths
- Build DisplayList → Compositor → Backend pipeline

## Non-goals
- Additional backends (Web/Native) beyond interface contracts

## Deliverables
- `src/Andy.Tui.DisplayList`, `src/Andy.Tui.Compositor`, `src/Andy.Tui.Backend.Terminal`
- End-to-end tests using Virtual Screen Oracle (Cells/Bytes)
- Logging/tracing baseline in `src/Andy.Tui.Observability`

## Architecture Decisions
- Pipeline: Compose → Style → Layout → DisplayList → Compositor → Backend
- Node visitors contribute to layout & display list only; no direct rendering
- Clipping implemented as DL nodes with push/pop semantics; compositor respects z-order and clips
- Damage model: dirty rects → row runs (TTY); diff DL for non-TTY backends

## API & Types
- DisplayList: `DrawRect`, `DrawBorder`, `DrawText`, `PushLayer`, `PushClip`, `Pop`
- Compositor: `CellGrid`, `DamageTracker`, `DisplayListDiffer`
- Backend.Terminal: `AnsiEncoder`, `IPtyIo`
- Oracle: `VirtualScreenDecoder` (bytes → cell grid)

## Tasks & Sequencing
- [ ] Define DL ops and builders; integrate with layout boxes
- [ ] Implement compositor with damage heuristics and DL diff
- [ ] Implement terminal ANSI encoder and PTY abstraction
- [ ] Observability: structured logging categories and spans per stage
- [ ] End-to-end DL → Cells → Bytes round-trips with golden tests

## Testing
- Unit: DL building, diffing; damage merging; ANSI encoding cases
- Golden: cell grid snapshots and bytes snapshots
- Integration: DL parity across dirty vs full repaint

## Observability
- Initialize comprehensive logging in tests via `ComprehensiveLoggingInitializer.Initialize(isTestMode: true)`
- Chrome Trace export for key pipelines

## Perf Gates
- One write per frame (TTY)
- Dirty ≤ 12% for scrolling log scenarios
- Full repaint 200×60 p95 ≤ 25ms

## Exit Criteria
- Virtual Screen Oracle parity; differential tests prove equivalence
- Logs/spans show stage timings; overhead minimal at info

## Risks & Mitigations
- Double-render regressions → strict removal of legacy paths; tests cover it
- Damage thrash → smoothing and row-run merging
