# Salvage Audit from v1 (`~/devel/rivoli-ai/andy-tui`)

Purpose: Identify high-quality, self-contained pieces from v1 that can be reused in v2 without compromising the new architecture.

Repo scanned: `/Users/samibengrine/devel/rivoli-ai/andy-tui` (multiple projects, tests, and docs)

## Summary
- **Adopt (near as-is)**: Display list invariant checks; ANSI color mapping tables/algorithms
- **Adapt (re-implement with same ideas)**: Terminal double buffer/dirty tracking; logging initializer patterns; terminal API surface naming
- **Avoid (architecturally conflicting)**: `VirtualDomRenderer` and any node-immediate rendering paths; legacy diff engine coupling; mixed visitor/RenderElement flows

## Candidates

### ANSI Encoder (Terminal Backend)
- Files: `src/Andy.TUI.Terminal/AnsiRenderer.cs`, `src/Andy.TUI.Terminal/Color.cs`, `src/Andy.TUI.Terminal/Style.cs`
- Strengths:
  - Solid ANSI sequences coverage: truecolor/256/16-color fallbacks
  - Color conversion helpers: `RgbTo256Color`, `RgbTo16Color`
  - Cursor positioning and basic clip logic
- Plan:
  - Adopt color mapping algorithms and test vectors
  - Integrate into v2 `Andy.Tui.Backend.Terminal` with single-write-per-frame strategy
  - Replace per-char writes with row-run batching from compositor

### Terminal Buffer & Dirty Regions
- Files: `src/Andy.TUI.Terminal/TerminalBuffer.cs`, `Buffer.cs`, `Cell.cs`
- Strengths:
  - Double buffering with dirty set; sentinel invalidation trick
  - Swap returns changed cells for efficient updates
- Plan:
  - Re-implement concept in v2 `Andy.Tui.Compositor` with more efficient dirty tracking (bitsets/row-runs)
  - Keep API shape: `SwapBuffers()` → enumerable of changes; ensures testability

### Display List Types & Invariants
- Files: `src/Andy.TUI.Terminal/Rendering/DisplayList.cs`
- Strengths:
  - Explicit DL items (PushClip/PopClip/DrawRect/DrawText/DrawBox) and layer monotonic invariants
  - Invariant validator with clear exceptions
- Plan:
  - Adopt invariant rules and test style in v2 `Andy.Tui.DisplayList`
  - Align item names with v2 ops (`LayerPush`, `ClipPush`, `Pop`, `Rect`, `Border`, `TextRun`)
  - Keep push/pop clip semantics and layer monotonic checks

### Diagnostics / Observability Init
- Files: `src/Andy.TUI.Diagnostics/ComprehensiveLoggingInitializer.cs`, `LogManager` et al.
- Strengths:
  - Test-mode logging bootstrap, correlation IDs, failure export
- Plan:
  - Adapt pattern behind v2 `Andy.Tui.Observability` facade
  - Preserve tests that validate low overhead in info mode; integrate with Chrome Trace exporter
  - Standardize initializer name and usage with `ComprehensiveLoggingInitializer.Initialize(isTestMode: true)`

### Terminal Abstractions
- Files: `src/Andy.TUI.Terminal/ITerminal.cs`
- Strengths:
  - Clear terminal capability surface
- Plan:
  - Use as naming inspiration; in v2 express as `TerminalCapabilities` + `IPtyIo`
  - Keep capability detection in the backend encoder, not the compositor

## Not Recommended to Reuse
- `VirtualDomRenderer`, `DeclarativeRenderer` and associated mixed flows — conflicts with v2 unified pipeline
- Legacy diff engine tightly coupled to rendering
- Ad-hoc clipping/positioning scattered in visitors

## Tests to Port/Adapt
- ANSI encoder tests: bytes sequences and color fallbacks (from `Andy.TUI.Terminal.Tests`)
- Display list invariant tests: ensure clip balance and layer monotonicity
- Dirty vs full repaint differential tests: port concept to v2 compositor/backend

## Proposed Integration Points in v2
- `src/Andy.Tui.DisplayList`: Item types; invariant validator and tests (LayerPush/ClipPush/Pop naming)
- `src/Andy.Tui.Compositor`: Dirty tracking with row-run deltas; swap API
- `src/Andy.Tui.Backend.Terminal`: ANSI encoder with color mapping algorithms
- `src/Andy.Tui.Observability`: Logging initializer pattern; test hooks

## Risk Assessment
- **Low**: Color mapping helpers; invariant validator
- **Medium**: Dirty tracking (ensure no O(N) hot paths on big grids)
- **High**: Any renderer that draws directly per-node (will be avoided)

## Next Steps
- [ ] Extract color mapping helpers into v2 backend with unit tests
- [ ] Implement DL invariant validator and port/adapt tests
- [ ] Implement compositor dirty tracking using row-run model (inspired by v1 buffer)
- [ ] Implement observability initializer aligned with v2 facade; add deterministic test hooks
- [ ] Align `Virtual Screen oracle` mentions to `VirtualScreenOracle` across docs and tests

## Notes
- No solid Unicode width/grapheme utilities were found in v1; v2 will implement this anew in `Andy.Tui.Text`
- Spatial index components exist in v1 but v2 compositor will prefer DL + damage approach; revisit later if needed
