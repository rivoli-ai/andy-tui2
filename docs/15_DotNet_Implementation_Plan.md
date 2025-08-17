# .NET Implementation Plan

This plan refines the roadmap into concrete .NET solution layout, milestones, acceptance criteria, and guardrails. It integrates the design docs (00–14) and addresses the rendering architecture issues by unifying the rendering approach.

## Solution & Project Structure

- `Andy.Tui.sln` — root solution
- Libraries (class libraries, `net8.0`, `nullable enable`):
  - `src/Andy.Tui.Core` — reactive core (signals, computed, effects); commands; binding converters/validators
  - `src/Andy.Tui.Compose` — DSL + reconciliation; virtual tree and diff
  - `src/Andy.Tui.Style` — CSS subset: cascade, specificity, variables, pseudos, media
  - `src/Andy.Tui.Layout` — Flexbox MVP (measure/arrange), later Grid
  - `src/Andy.Tui.Text` — grapheme segmentation, widths, wrapping, caches
  - `src/Andy.Tui.DisplayList` — drawing ops, text runs, layers, clips
  - `src/Andy.Tui.Compositor` — damage tracking, cell compositor (TTY), DL diff
  - `src/Andy.Tui.Backend.Terminal` — ANSI encoder, PTY I/O abstraction
  - `src/Andy.Tui.Backend.Web` — Canvas/DOM (Phase 5)
  - `src/Andy.Tui.Backend.Native` — Skia/WGPU (Phase 5)
  - `src/Andy.Tui.Input` — input, focus, IME model, accessibility semantics
  - `src/Andy.Tui.Animations` — transitions, keyframes, frame pacing
  - `src/Andy.Tui.Virtualization` — list/grid virtualization engine
  - `src/Andy.Tui.Widgets` — base widgets (from v1 Declarative) + virtualized list/grid, editor core, real-time log viewer
  - `src/Andy.Tui.Observability` — logging facade, tracing spans, HUD, capture/replay
- Tests (xUnit):
  - `tests/Andy.Tui.Core.Tests`
  - `tests/Andy.Tui.Compose.Tests`
  - `tests/Andy.Tui.Style.Tests`
  - `tests/Andy.Tui.Layout.Tests`
  - `tests/Andy.Tui.Text.Tests`
  - `tests/Andy.Tui.Rendering.Tests` (DisplayList + Compositor + Backend; `VirtualScreenOracle`)
  - `tests/Andy.Tui.Input.Tests`
  - `tests/Andy.Tui.Animations.Tests`
  - `tests/Andy.Tui.Virtualization.Tests`
  - `tests/Andy.Tui.Widgets.Tests`
  - `tests/Andy.Tui.Observability.Tests`

## Cross-cutting Decisions

- Rendering unification (addresses current architecture issues):
  - Single pipeline: Compose → Style → Layout → DisplayList → Compositor → Backend
  - No per-node immediate rendering. Remove/avoid `RenderElement`-style entry points.
  - Node visitors are permitted only to contribute to layout and display list construction, not to perform drawing directly.
  - Clipping and z-order handled in display list and compositor, not ad-hoc per node.
- Consistent API for adding children: expose `AddChild(...)` and collection initializers consistently; remove conflicting patterns.
- Determinism mode: virtual clock, seeded RNG, single-thread scheduler, stubbed I/O, fixed terminal capabilities.
- Observability: structured logging + tracing spans via a lightweight facade; export Chrome Trace; HUD toggle.

## Technology & Packages

- Target framework: .NET 8.0
- Language features: `nullable enable`, `implicit usings` enabled in SDK-style projects
- Testing: xUnit + FluentAssertions; snapshot/golden via Verify.Xunit
- Coverage: coverlet.collector
- Logging: `Microsoft.Extensions.Logging.Abstractions` behind our facade (optionally); or pure custom facade with sinks
- Source generators (optional later): DSL optimizations, enum renderers

## Milestones, Tasks, and Acceptance Criteria

Each item includes a checkbox to track completion in docs. Do not check until tests pass and exit criteria hold.

### Phase 0 — Foundations (Week 1–2)
- [ ] Core reactive primitives (signals, computed, effects) with thread-affinity policy
- [ ] Commands, converters, validators; two-way binding plumbing
- [ ] Determinism runtime toggles and test harness bootstrapping
- [ ] Minimal DSL skeleton and virtual node representation

Acceptance:
- [ ] Unit tests cover core reactivity semantics and disposal
- [ ] Deterministic golden for simple compose → no-op re-render

### Phase 1 — Visual Core (Week 3–5)
- [ ] CSS subset: cascade, specificity, variables, pseudos
- [ ] Flex layout engine MVP (measure/arrange contract)
- [ ] Text engine: segmentation, width, wrapping; cache interfaces

Acceptance:
- [ ] Golden tests of styled boxes; layout invariants hold
- [ ] Text wrapping snapshot suite passes for mixed-width graphemes

### Phase 2 — Rendering Core (Week 6–8)
- [ ] Display list ops: rect, border, text, layer, clip, z-index
- [ ] Compositor: cell grid, damage tracking, DL-diff → cells
- [ ] Terminal backend: ANSI encoder, PTY I/O abstraction
- [ ] Observability baseline: logging categories, spans

Acceptance:
- [ ] `VirtualScreenOracle` parity: DL → cells → bytes round-trip equals expected
- [ ] Dirty vs full repaint differential tests prove equivalence

### Phase 3 — Interactivity & Animations (Week 9–10)
- [ ] Input routing, focus model, IME hooks
- [ ] Animations: transitions/keyframes, frame scheduler with target FPS
- [ ] HUD overlay toggles and metrics plumbed

Acceptance:
- [ ] Event → compose → anim → render golden scenarios pass
- [ ] HUD timings consistent; no excessive overhead at info level

### Phase 4 — Virtualization & Widgets (Week 11–14)
- [ ] Base Widgets (from v1 Declarative)
  - Containers
    - [ ] Stack (VStack/HStack) — Flex wrappers with sensible defaults
    - [ ] Grid (simple cells prior to full Grid engine)
    - [ ] ScrollView (single child, overflow handling)
    - [ ] Border/Box (padding/border/radius/background)
    - [ ] Spacer & Divider
  - Content
    - [ ] Text / Label (styleable, alignment, wrapping hooks to Text engine)
    - [ ] Icon (glyph-based)
  - Inputs / Controls
    - [ ] Button (press/hover/focus states)
    - [ ] Toggle / Checkbox
    - [ ] RadioButton (grouping)
    - [ ] TextInput / TextArea (cursor, selection basics)
    - [ ] Select / Dropdown (simple list popup)
    - [ ] Slider
    - [ ] ProgressBar
  - Collections & Navigation
    - [ ] ListView (non-virtualized baseline)
    - [ ] Table (simple header + rows)
    - [ ] TreeView (expand/collapse)
    - [ ] Tabs
  - Overlays
    - [ ] Dialog / Modal
    - [ ] Tooltip

- [ ] Virtualized list/grid with overscan tuning
- [ ] Real-time log view (sustained high-throughput append)
- [ ] Editor MVP (buffer, viewport, basic edits)

Acceptance:
- [ ] Base widgets expose consistent APIs and CSS style mappings; focus and input semantics verified
- [ ] Perf targets met per Performance Acceptance Plan for scenarios
- [ ] Widget suites stable across 1k deterministic runs

#### Notes (Base Widgets)
- v1 Declarative provided these primitives; in v2, containers are implemented atop Flex (Phase 1) and integrate with the CSS subset.
- Non-virtualized lists/tables provide parity and simpler mental models; virtualized variants land in the same module with shared interfaces.
- Input controls wire to `Andy.Tui.Input` focus and event routing; pseudo-classes map to visual states.

### Phase 5 — Additional Backends (Week 15–18)
- [ ] Web Canvas backend
- [ ] Native Skia/WGPU backend

Acceptance:
- [ ] DL parity tests across backends; visual diffs within tolerance

## Testing Strategy Integration

- Oracles: `VirtualScreenOracle`; Layout invariants; Bytes snapshots
- Test pyramid: unit → golden → property/fuzz → integration (PTY) → soak/perf
- Tooling commands:
  - `dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults`
  - Use ReportGenerator to produce HTML/TextSummary

## Observability Plan

- Facade with category filters; ring buffer + file sink
- Span API; automatic stage bracketing; Chrome Trace exporter
- HUD overlay with FPS, stage times, bytes, dirty %, queue depth
- Capture writer/reader; replay harness integrated with deterministic mode

## Performance Gates

- Nightly perf runs with budgets from `14_Performance_Acceptance_Plan.md`
- Budget asserts in unit/integration tests (compose/layout/paint thresholds)
- Degradation policy: overscan reduction, animation skipping, adaptive FPS

## Build, Quality, and Developer Workflow

- `dotnet format` before commits; pre-commit hooks via `./scripts/setup-git-hooks.sh`
- Ensure tests pass locally; generate coverage for significant changes
- Keep roadmap checklists up to date; add dated summaries for milestones

## Risks & Mitigations

- Text rendering edge cases → property tests with unicode corpora
- Performance regressions → capture & replay + CI perf gates
- API drift between layers → internal interfaces + integration tests

## Definition of Done (per feature)

- Tests: unit + golden where applicable; no flakes in 1k deterministic runs
- Observability: spans and logs present; HUD shows relevant metrics
- Performance: within budget for its scenario(s)
- Docs: updated checklists and brief summary in roadmap
