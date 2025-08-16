# Phase 5 — Additional Backends (Week 15–18)

## Outcomes
- Web Canvas and Native Skia/WGPU backends implemented
- Display list parity across backends validated

## Scope
- Backend abstractions, platform-specific encoders, DL parity tests

## Non-goals
- Platform-specific widgets beyond parity validation

## Deliverables
- `src/Andy.Tui.Backend.Web`, `src/Andy.Tui.Backend.Native`
- Backend parity test suites

## Architecture Decisions
- Backend interface abstracts cell-based and pixel-based targets consistently
- DL diffing leveraged for non-TTY backends; batching strategies per platform

## API & Types
- Backend contracts: `IRenderBackend`, `IRenderSurface`, `ITextRasterizer`
- Web: Canvas implementation; Native: Skia/WGPU implementation stubs

## Tasks & Sequencing
- [ ] Define backend contracts and adapters
- [ ] Implement Web Canvas encoder; minimal hosting harness
- [ ] Implement Native Skia/WGPU pipeline; minimal hosting harness
- [ ] Parity tests vs terminal reference DL

## Testing
- Unit: backend encoders
- Integration: DL parity comparisons
- Perf: batch sizes and frame pacing tuning

## Observability
- Shared spans/logging categories; trace export supported on each backend

## Perf Gates
- DL parity within visual tolerance; maintain target FPS

## Exit Criteria
- Parity tests pass; backends integrated with pipeline

## Risks & Mitigations
- Platform differences (text, scaling) → tolerance-based diffs and adapters
