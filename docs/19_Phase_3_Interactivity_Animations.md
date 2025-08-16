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

## Tasks & Sequencing
- [ ] Implement input event routing and focus management
- [ ] Implement animations and frame scheduler
- [ ] Integrate HUD overlay and metrics sources
- [ ] Compose hooks for animation-driven updates

## Testing
- Unit: focus transitions, key/mouse routing, animation curves
- Integration: input → compose → render sequences; HUD metrics presence
- Soak: long-running animations without drift under deterministic mode

## Observability
- FPS and stage timings visible in HUD; spans around animation ticks

## Perf Gates
- Target FPS 30–60 stable under normal interactions

## Exit Criteria
- Event/animation scenarios golden tests pass; HUD verified

## Risks & Mitigations
- Event storms → coalescing and back-pressure strategy
- Animation drift → test against deterministic clock
