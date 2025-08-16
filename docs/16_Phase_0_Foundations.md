# Phase 0 — Foundations (Week 1–2)

## Outcomes
- Reactive core primitives implemented and tested (signals, computed, effects)
- Bindings: two-way, converters, validators, and commands surface
- Determinism runtime and test harness established
- Minimal Compose DSL skeleton and virtual node representation

## Scope
- Core reactive engine and binding layer
- Test determinism framework and fixtures
- Initial view DSL: element creation, properties, and child composition

## Non-goals
- Full rendering pipeline, layout, and text shaping
- Backends beyond minimal test stubs

## Deliverables
- `src/Andy.Tui.Core`: reactive primitives and bindings
- `src/Andy.Tui.Compose`: virtual nodes and minimal DSL
- `tests/Andy.Tui.Core.Tests`, `tests/Andy.Tui.Compose.Tests` with deterministic tests
- Determinism toggles and utilities usable from tests

## Architecture Decisions
- Single-threaded scheduler affinity for reactive updates in deterministic mode
- Stable ordering of effects; explicit disposal semantics
- Command abstraction with CanExecute and async execute; support cancellation tokens

## API & Types
- Core
  - `Signal<T>`, `Computed<T>`, `Effect`
  - `ObservableDisposable` for lifetime (deferred)
- Bindings
  - `Binding<T>` with `IValueConverter`, `IValidator`
  - `Command`/`AsyncCommand`
- Determinism
  - `IDeterminismClock`, `ManualClock`, `DeterministicScheduler`
- Compose DSL
  - `VNode`, `VElement`, `VText`
  - `View` base and builder-style modifiers (non-rendering)

## Tasks & Sequencing
- [x] Implement `Signal<T>`, `Computed<T>`, `Effect` with disposal and dependency tracking
- [x] Implement `Binding<T>` with converters/validators and two-way plumbing
- [x] Implement `Command`/`AsyncCommand` with CanExecute tracking
- [x] Deterministic clock and scheduler (`ManualClock`, `DeterministicScheduler`)
- [x] Stubbed I/O for deterministic tests (`StubbedOutput`)
 - [x] Compose: define `VNode` hierarchy and minimal builder DSL for elements/text
 - [x] Test harness bootstrap: base test class enabling determinism

## Testing
- Unit: reactivity semantics, disposal, binding propagation, command enablement
- Property: dependency graph invalidations; no lost updates; no infinite loops with guards (FsCheck integrated)
- Golden: snapshot of virtual tree for simple view composition

## Observability
- Logging facade available: `ILogger`, `ILoggerFactory`, with `NoopLogger`, `InMemoryLogger`, and `LoggerFactory`
- Tracing stubs remain no-ops in Phase 0 (Phase 2 adds spans/export)

## Perf Gates
- Micro-benchmarks for `Signal`/`Computed` (BenchmarkDotNet) and CI budget tests
  - Target: `Signal` update < 200 ns/update (budget gate ≤ 350 ns in CI)
  - Command: `dotnet run -c Release -p benchmarks/Andy.Tui.Benchmarks/Andy.Tui.Benchmarks.csproj`

## Exit Criteria
- Deterministic tests pass across 1k re-runs (no flakes)
- Compose → recomposition tests stable and fast
- CI performance budget tests green for `Signal` and `Computed`

## Risks & Mitigations
- Feedback loops in reactive graph → detection with guard counters and test coverage
- Overly complex DSL early → keep minimal and iterate in Phase 1

## Progress (2025-08-16)
- Implemented `Signal<T>`, `Computed<T>`, `Effect` with tests (26 tests in `Andy.Tui.Core.Tests`)
- Implemented `Binding<T>`, `RelayCommand`, `AsyncRelayCommand` with tests (incl. cancellation and CanExecute changes)
- Implemented `ManualClock` + `DeterministicScheduler`; basic tests added
- Added XML docs for Core reactive and bindings APIs
- CI packs master meta-package `Andy.Tui` and individual packages
 - Compose DSL: added `VNode`, `VElement`, `VText`, `View` with initial unit tests

## Gaps / Next Steps
- Property tests: expanded to multi-level graphs (more shapes can be added over time)
- Micro-benchmarks: scheduled weekly run with artifacts (`.github/workflows/benchmarks.yml`)
- Observability facade: basic factory + loggers implemented (Phase 2 to add tracing/spans/export)

See `docs/22_Salvage_Audit_from_v1.md` for patterns to adapt (e.g., logging initializer) without importing legacy rendering paths.
