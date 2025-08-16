# Reactive Core & Bindings

## Purpose
Provide fine-grained reactivity (SwiftUI feel) with WPF-style bindings (two-way, converters, validation, commands).

## Key Types (conceptual)
- `Signal<T>`, `Var<T>`, `Computed<T>`, `Effect`
- `Binding<T> = { get, set, mode, converter?, validators[] }`
- `Command { canExecute: Signal<bool>, execute: () => void }`

## Interfaces
- **Public:**
  - `Var<T>(initial): Var<T>`
  - `Computed(fn): Signal<T>`
  - `Effect(fn): Disposer`
  - `bind(targetProp, binding: Binding<T>)`
  - `new Command()`, `command.bindCanExecute(signal)`
- **Internal:**
  - Dependency graph with versioning; topo-ordered invalidation.

## Implementation Plan
1. **Signals & Vars**
   - Version counters; readers record dependencies during evaluation.
   - Effects schedule microtasks on the UI scheduler.
2. **Computed**
   - Memoize result; lazy re-compute; dependency set tracked per evaluation.
3. **Bindings**
   - Modes: OneTime/OneWay/TwoWay; path via key-paths/lambdas.
   - Converters & validators (sync first; async later).
4. **Commands**
   - Bind `canExecute` to disable controls; auto-toggle on state change.
5. **Schedulers**
   - Immediate (tests) + frame-coalescing (runtime).

## Test Plan
- Unit: dependency tracking, recomposition coverage, TwoWay round-trip, converter error handling.
- Fuzz: random dependency graphs — ensure stable topo ordering and no leaks.
- Perf: microbench invalidation throughput.

## Exit Criteria
- 100% deterministic with fixed scheduler.
- Bindings drive CSS variables, layout props, text inputs without feedback loops.
