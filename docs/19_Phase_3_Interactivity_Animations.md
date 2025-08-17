# Phase 3 — Interactivity & Animations (Week 9–10)

## Outcomes
- Input routing, focus model, IME hooks implemented
- Animations: transitions/keyframes with frame scheduler and FPS target
- HUD overlay with key metrics

## Scope
- Event system integration through compose; animation driving re-composition

## Non-goals
- Advanced accessibility semantics beyond initial model

## Deliverables
- `src/Andy.Tui.Input`, `src/Andy.Tui.Animations`, HUD in `src/Andy.Tui.Observability`
- Integration tests validating event → compose → render loops

## Architecture Decisions
- Focus model with traversal, active element, and focus scopes
- Animation clock integrated with deterministic clock for tests
- HUD overlay rendered through DL layer with toggles

## API & Types
- Input: `InputEvent`, `KeyEvent`, `MouseEvent`, `FocusManager`
- Animations: `Animation`, `Timeline`, `Easing`, `Scheduler`
- HUD: `HudOverlay` with metrics surface

## Dependencies and Cross-Refs
- Requires Phase 2 pipeline in place: Compose → Style → Layout → DisplayList → Compositor → Backend (see `docs/18_Phase_2_Rendering_Core.md`).
- Leverages Deterministic Clock and scheduler primitives (see `docs/16_Phase_0_Foundations.md`).
- Interpolable properties, transition/@keyframes semantics align with `docs/03_CSS_and_Style_System.md` and details in `docs/09_Animations_Timeline_and_FPS.md`.
- HUD metrics and tracing align with `docs/11_Observability_Logging_Tracing_Capture.md` and acceptance targets in `docs/14_Performance_Acceptance_Plan.md`.

## Detailed Implementation Plan

### 1) Unified Input System
- Event decoding (TTY first)
  - Keyboard: decode CSI/SS3 sequences; support modifiers; optional Kitty keyboard protocol where available.
  - Mouse: enable SGR 1006 mode; track button presses, releases, moves, wheel; include position in viewport coordinates.
  - Paste: bracketed paste enable/disable; emit `PasteEvent` with payload.
  - Resize: terminal size changes propagate `ResizeEvent` (triggers layout).
  - IME: composition start/update/end surface through `ImeEvent` with committed vs composing text.
  - Web/Native: map platform events to unified events via adapter interfaces (stub this phase, ensure API parity).

- Event model
  - Types: `KeyEvent { key, code, modifiers }`, `MouseEvent { kind, x, y, button?, modifiers }`, `PasteEvent { text }`, `ResizeEvent { cols, rows }`, `ImeEvent { kind, text }`.
  - Routing: events traverse from focused element outward with capture/bubble phases; default handler at application root.
  - Cancellation: handlers may mark handled to stop propagation.

- Focus model
  - `FocusManager` as a single owner; maintains active element id and a list of focusable nodes.
  - Traversal: `focusNext`, `focusPrevious`, programmatic `setFocus(nodeId)`, roving focus pattern for lists.
  - Scopes: modal/focus traps create subtrees with local traversal; escape to parent when scope closes.
  - Pseudo-state integration: apply `:focus`, `:hover`, `:active` flags to style engine; invalidate affected nodes.

- Integration with compose
  - On input: enqueue event → route → if handler mutates state or pseudo-state changes, mark nodes dirty → re-compose.
  - Coalescing: combine rapid move/drag or repeat key events; drop stale pointer moves under load.
  - Back-pressure: if event queue grows beyond threshold, apply degradation policy (see Perf section).

Checklist
- [ ] TTY decoders: keyboard, mouse (1006), paste, resize
- [ ] IME surface and composition lifecycle
- [ ] Focus manager with scopes and traversal; pseudo-states wired
- [ ] Event routing with capture/bubble and cancellation
- [ ] Coalescing/back-pressure policies with tests

### 2) Animation Subsystem (Transitions & Keyframes)
- Data model
  - `AnimationId`, `AnimationState { startedAt, delay, duration, easing, iterations, direction, fillMode }`.
  - `Transition { property, from?, to, duration, delay?, easing }` resolved from style `transition` declarations.
  - `Keyframes { name, frames: [{ offset: 0..1, values: Map<Property, Value> }] }` from `@keyframes` definitions.
  - Attach to nodes via computed style: `animation-name`, `animation-duration`, `animation-delay`, `animation-iteration-count`, `animation-direction`, `animation-timing-function`, `animation-fill-mode`.

- Interpolation
  - Numeric and length values: linear interpolation in computed units (px, percentages resolved at start where applicable).
  - Colors: RGB24 linear interpolation; consider gamma if needed later.
  - Discrete values: step change at boundary offsets.
  - Supported properties in Phase 3: a core subset from `docs/03_CSS_and_Style_System.md` marked interpolable (e.g., `opacity`, `tint/opacity` for text, `offset`/position for movement, `color`, `background`, sizes where safe).

- Timeline and scheduler
  - Global `Timeline` driven by a `FrameScheduler` tick.
  - Each tick advances active animations by `deltaTime`; computes new property values; triggers node invalidation when properties change.
  - Deterministic mode for tests via `ManualClock` advancing discrete steps; verify produced values at checkpoints.

- Compose integration
  - Animation-driven updates enqueue a compose pass without user events.
  - Dirty tracking: only nodes with changed animated properties are marked; propagate to layout if properties affect geometry.

Checklist
- [ ] Parse/resolve `transition` and `@keyframes` subset into runtime structures
- [ ] Implement `Easing` functions (in/out/linear; extendable to cubic variants)
- [ ] Interpolators for numbers, lengths, colors; discrete fallbacks
- [ ] Per-tick evaluation and node invalidation
- [ ] Deterministic verification harness (advance N ms → expect values)

### 3) Frame Scheduler & FPS Control
- Targets
  - Default target FPS 60; allow 30/15 via configuration or auto step-down.
  - One buffered write per frame (TTY); no interleaving frames.

- Scheduler loop
  - Compute target frame time; measure previous frame stages (compose, style, layout, DL, composite, encode, write).
  - Sleep/spin to align with target; if over budget, consider skipping non-essential animations or reducing frequency.
  - Coalesce multiple invalidations within a frame window.

- Adaptive policies (see `docs/09_Animations_Timeline_and_FPS.md` and `docs/14_Performance_Acceptance_Plan.md`)
  - Step-down FPS under sustained overload; step-up when idle.
  - Frame-skip: deprioritize low-importance animations first.
  - Cap dirty % per frame; carry over remainder to next frame.

Checklist
- [ ] Implement `FrameScheduler` with target FPS and deterministic clock support
- [ ] Stage timing measurement and aggregation (instant + EMA)
- [ ] Adaptive step-down/up and animation deprioritization hooks
- [ ] Ensure single buffered write per frame in backend

### 4) HUD Overlay (Observability)
- Rendering
  - Implement `HudOverlay` as a DL-contributed layer rendered last; not part of layout tree.
  - Toggle via key (e.g., `F12`) and programmatic API; configurable verbosity.

- Metrics displayed (align with `docs/11_Observability_Logging_Tracing_Capture.md`)
  - FPS instant and EMA
  - Per-stage timings (compose/style/layout/DL/compositor/encoder/write)
  - Dirty %, bytes per frame, event queue depth, dropped/coalesced counts
  - Clock mode indicator (deterministic vs real-time)

Checklist
- [ ] Metrics source plumbing and aggregation window
- [ ] Overlay drawing via DL primitives; ensure minimal overhead
- [ ] Toggle and level controls; tests for presence/format

### 5) Event → Compose → Render Integration
- Ensure any of: input events, animation ticks, resize, and pseudo-state changes trigger the same single pipeline.
- Verify no legacy direct-render paths exist (Phase 2 invariant).
- Add trace spans around animation tick and event routing for Chrome Trace export.

Checklist
- [ ] Unified invalidation entrypoints
- [ ] Trace spans for event routing and animation ticks
- [ ] Integration tests covering all trigger types

## Tasks & Sequencing
- [ ] Implement input event routing and focus management
  - [ ] Keyboard/mouse/paste/resize decoders (TTY)
  - [ ] IME lifecycle events
  - [ ] Focus manager + scopes + pseudo-states
  - [ ] Routing with capture/bubble and cancellation
- [ ] Implement animations and frame scheduler
  - [ ] `transition`/`@keyframes` parse/resolve
  - [ ] Interpolators and easings
  - [ ] Timeline evaluation and invalidation
  - [ ] Scheduler with target FPS and adaptivity
- [ ] Integrate HUD overlay and metrics sources
  - [ ] Collect stage timings, bytes, dirty %, queue depth
  - [ ] Overlay DL layer and toggles
- [ ] Compose hooks for animation-driven updates
  - [ ] Deterministic clock harness and tests

## Testing
- Unit: focus transitions, key/mouse routing, animation curves
- Integration: input → compose → render sequences; HUD metrics presence
- Soak: long-running animations without drift under deterministic mode

Additional tests
- Golden bytes → events decoding (partial sequences, modifiers, alt codes)
- Animation keyframe/transition checkpoints at fixed times (deterministic clock)
- FPS control under load: verify adaptive policies engage; ensure stability at 15/30/60 targets
- Event storms (scroll/drag/append): coalescing and budget adherence

## Observability
- FPS and stage timings visible in HUD; spans around animation ticks
 - Structured logs per stage; counters for coalesced/dropped events
 - Chrome Trace export includes animation and scheduler slices

## Perf Gates
- Target FPS 30–60 stable under normal interactions
 - One write per frame (TTY); dirty ≤ thresholds from `docs/14_Performance_Acceptance_Plan.md`
 - Animation evaluation overhead ≤ 15% frame budget at 60 FPS on curated scenes

## Exit Criteria
- Event/animation scenarios golden tests pass; HUD verified
 - Deterministic tests green; no drift across long runs
 - Unified pipeline only; no direct rendering entry points

## Risks & Mitigations
- Event storms → coalescing and back-pressure strategy
- Animation drift → test against deterministic clock
 - Overdraw from HUD → draw as lightweight DL and allow quick toggle
 - IME complexity → ship MVP hooks; iterate with platform adapters in later phase

## Inspirations (not 1:1 copies)
- Bubble Tea: single buffered write discipline, framerate-based renderers, focus and mouse support; spring animations inspiration (`harmonica`).
- Textual: high-level animation API with easing functions and completion callbacks; event system and async-friendly architecture.
- Ink: focus management hooks and input handling patterns; snapshot-friendly testing for terminal output.

## Salvage from `andy-tui` (legacy)

Targets to port/adapt into `andy-tui2` for Phase 3. Paths are under `users/samibengrine/devel/rivoli-ai/andy-tui`.

- Frame scheduler and FPS
  - `src/Andy.TUI.Terminal/RenderScheduler.cs`
    - Port scheduling concepts: `TargetFps`, OnDemand vs Fixed modes, per-frame timing, before/after frame events, single buffered write discipline.
    - Replace direct buffer/renderer coupling with our Phase 2 pipeline hooks: integrate timings from Compose → Backend, and emit spans/metrics for HUD.
    - Keep batching semantics (queue + max wait) but drive invalidations via our unified pipeline.

- Unified input model and decoders
  - `src/Andy.TUI.Terminal/InputEvent.cs` (types: `InputEvent`, `KeyInfo`, `MouseInfo`, `ResizeInfo`)
    - Port type shapes nearly 1:1 into `src/Andy.Tui.Input`, adjusting namespaces and naming (`Andy.Tui.*`).
    - Retain modifier helpers and string representations for diagnostics.
  - `src/Andy.TUI.Terminal/{CrossPlatformInputManager,EnhancedConsoleInputHandler,ConsoleInputHandler,PollingInputHandler}.cs`
    - Reuse TTY decoding strategies as a baseline; wire outputs to new unified event bus and focus router.

- Focus management
  - `src/Andy.TUI.Declarative/Focus/FocusManager.cs`
    - Port invariant-checked registry, `SetFocus`, `MoveFocus`, stable next-focus on unregister.
    - Map to `FocusManager` API in this doc, integrate with pseudo-states and style invalidations.
  - `src/Andy.TUI.Declarative/Hooks/UseFocusHook.cs`
    - Salvage ergonomics for per-component focus state; adapt to new compose/bindings layer.

- Event routing
  - `src/Andy.TUI.Declarative/Events/EventRouter.cs`
    - Port Tab/Shift+Tab behavior and focused-component routing; expand to capture/bubble phases and mouse hit-testing via layout boxes.

- Hooks and input handling
  - `src/Andy.TUI.Declarative/Hooks/UseInputHook.cs`
    - Salvage handler subscription mechanics; adapt to new input event shapes and cancellation model.

- Tests to adapt
  - `tests/Andy.TUI.Terminal.Tests/InputSystem/*` (Key/Mouse/Resize)
  - `tests/Andy.TUI.Terminal.Tests/Rendering/RenderSchedulerTests.cs`
  - `tests/Andy.TUI.Declarative.Tests/Focus/*`, `Integration/TabNavigation*`
    - Use as seeds for: decoder golden tests, focus traversal invariants, scheduler timing assertions (converted to deterministic clock).

Porting notes
- Namespace/casing: convert `Andy.TUI.*` → `Andy.Tui.*`.
- Replace legacy direct rendering/terminal buffer usage with Phase 2 DL/compositor/backend contracts.
- Avoid bringing over virtual DOM or renderer shortcuts that bypass the unified pipeline.

Salvage checklist
- [ ] Port `InputEvent`/`KeyInfo`/`MouseInfo`/`ResizeInfo` to `src/Andy.Tui.Input`
- [ ] Implement TTY input manager using legacy decoders; add unit tests
- [ ] Port `FocusManager` with invariants; integrate pseudo-states and style invalidation
- [ ] Implement `EventRouter` with capture/bubble; map Tab/Shift+Tab; mouse focus via hit-test
- [ ] Adapt `RenderScheduler` ideas into `FrameScheduler` with stage timings and HUD metrics
- [ ] Migrate relevant tests to new projects with deterministic clock
