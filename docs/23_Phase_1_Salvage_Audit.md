# Phase 1 Salvage Audit (from v1: `~/devel/rivoli-ai/andy-tui`)

Goal: Reuse proven tests/algorithms from v1 for Style/Layout/Text without importing its problematic rendering paths.

## High-Confidence Salvage Targets

- Layout (APIs and invariants)
  - Tests to mirror/adapt:
    - `tests/Andy.TUI.Layout.Tests/LayoutConstraintsTests.cs`
    - `tests/Andy.TUI.Layout.Tests/LayoutBoxTests.cs`
    - `tests/Andy.TUI.Declarative.Tests/Layout/StackLayoutTests.cs`
    - `tests/Andy.TUI.Declarative.Tests/Layout/GridLayoutTests.cs`
    - `tests/Andy.TUI.Declarative.Tests/Layout/ConstraintPropagationTests.cs`
  - Source references for ideas (not direct reuse):
    - `src/Andy.TUI.Layout/LayoutBox.cs` (constraints, rect math)
    - `src/Andy.TUI.Declarative/Layout/*` (V/H stacks behavior)

- Text (wrapping and fixtures)
  - Tests to mirror/adapt:
    - `tests/Andy.TUI.Declarative.Tests/TextWrapExampleTests.cs`
    - `tests/Andy.TUI.Declarative.Tests/TextLayoutTests.cs`
  - Plan: port test cases into `Andy.Tui.Text.Tests` and implement `GraphemeEnumerator`, width tables, and wrapping strategies.

- Style (naming and theming expectations)
  - Tests/Docs for reference:
    - `tests/Andy.TUI.Theming.Tests/*` (theming invariants)
    - Theming sources under `src/Andy.TUI.Theming/*`
  - Plan: use naming and category conventions; implement CSS subset fresh (cascade, specificity, variables, pseudos), while borrowing test vocabulary where helpful.

## Out-of-Scope for Salvage

- Any rendering engine or VirtualDomRenderer-dependent code
- Mixed visitor/immediate rendering paths

## Concrete Actions

- Layout
  - Define `Size`, `Rect`, `Thickness` structs with XML docs (tty-friendly units)
  - Interfaces: `ILayoutNode` with `Measure(Size)`, `Arrange(Rect)` and caching policy
  - Flex subset: row/column; wrap; grow/shrink/basis; alignment properties
  - Tests: adapt constraints, box layout, and propagation tests from v1

- Text
  - Implement `GraphemeEnumerator` with Unicode test corpus; width tables (East Asian width), mixed-width cases
  - Wrapping strategies: greedy by width, hard/soft break handling
  - Tests: adapt text wrap examples; add property tests for long runs and mixed graphemes

- Style
  - `Style`, `StyleRule`, `Selector`, `Specificity`; variables and pseudo-states
  - Tests: selector specificity, cascade order, variables resolution

- Observability
  - Add measure/arrange spans around layout passes (log stubs in Phase 1)

## Risks & Mitigations

- Unicode width edge-cases → expand corpus and add property tests
- Flex edge-cases → follow W3C-inspired fixtures; align naming with common implementations
- API drift → define stable interfaces early; lock with tests
