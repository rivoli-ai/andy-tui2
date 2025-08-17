# Phase 1 — Visual Core (Week 3–5)

## Outcomes
- CSS subset implemented: cascade, specificity, variables, pseudo-classes, media
- Flexbox layout MVP (measure/arrange contracts) implemented
- Text engine: grapheme segmentation, width, wrapping, caches

## Scope
- Styling, layout, and text metrics for terminal-friendly rendering

## Non-goals
- Full display list and rendering; backends beyond simple probes

## Deliverables
- `src/Andy.Tui.Style`, `src/Andy.Tui.Layout`, `src/Andy.Tui.Text`
- Golden tests for styled boxes and layout invariants
- Text wrapping and width calculation test suites
 - Salvage plan: see `docs/23_Phase_1_Salvage_Audit.md`

## Architecture Decisions
- Layout API: `Measure(Size available)` and `Arrange(Rect final)` with caching
- Flexbox subset: row/column, wrap, grow/shrink, basis, alignments
- Text shaping: width tables + grapheme cluster segmentation; cache by font metrics and style

## API & Types
- Style
  - `Style`, `StyleRule`, `Selector`, `Specificity`
  - Variables and computed values; pseudo-state flags
- Layout
  - `ILayoutNode`, `LayoutBox`
  - Flex properties: `flex-direction`, `flex-wrap`, `justify-content`, `align-items`, `align-self`, `flex-grow`, `flex-shrink`, `flex-basis`
- Text
  - `TextMeasurer`, `GraphemeEnumerator`, `WrappingStrategy`

## Tasks & Sequencing
- [x] Implement CSS cascade and specificity with variables
- [x] Implement pseudo-states and media hooks
- [x] Implement Flex layout measure/arrange (row + column, wrap, grow/shrink, alignments)
- [x] Implement text segmentation, width calc, and wrapping strategies (MVP: graphemes via StringInfo; East Asian width approx; word/char wrap)
- [ ] Compose integration: map DSL props to style/layout nodes
 - [ ] Adapt v1 layout/text test fixtures into Phase 1 tests
  - Flex:
    - [x] Gaps (`row-gap`/`column-gap`) for row/column
    - [x] Wrapping (row)
    - [x] Wrapping (column)
    - [x] Alignments (row): `justify-content`, `align-items`, `align-content`
    - [x] Align-self overrides
    - [x] Column direction: vertical `justify-content`, horizontal `align-items`, `align-content` across columns
 - Flex sizing:
   - [x] `flex-basis` used as base
   - [x] `flex-grow` distribution (row)
   - [x] `flex-shrink` distribution (row)
   - [x] Clamping and min/max constraints
 - [ ] Browser-parity golden tests (Playwright harness) for curated fixtures

## Testing
- Unit: selector specificity, variable resolution, flex computations
- Golden: styled boxes and layout trees snapshots; invariants (no negative sizes, containment)
- Property: random layout trees respecting constraints
 - Parity: compare boxes and used values to Chromium for curated scenarios (±1px tolerance)
 - Parity (TUI/Yoga subset): port curated Yoga gap/wrap/align/grow-shrink fixtures relevant to TUI; validate within ±1 cell tolerance

## Progress (2025-08-16)

- Style: Cascade/specificity and custom properties (`var()`) implemented with tests; selector matching for type/id/class; shorthand precedence for padding/margin; minimal CSS parser for our subset (type/.class/#id/:pseudo) with simple `@media` support; pseudo-classes wired (`:hover/:focus/:active/:disabled`); color parsing (hex, expanded named set, `rgb()`/`rgba()`); inheritance applied for `color` and text properties (`font-weight`, `font-style`, `text-decoration`). Percentages supported for sizing (`width/height/min/max/flex-basis`) and resolved during layout.
- Layout: Flex row wrap, gaps, all `justify-content` variants, `align-items` (row/column basics), `align-content` across lines (row), `align-self` overrides, and `flex-basis/grow/shrink` (row) implemented with tests. Column direction supported (nowrap) including vertical `justify-content` and horizontal `align-items`.
- Text: MVP implemented (grapheme segmentation via StringInfo, East Asian width approx, word/char wrapping, caches)
- Tests: Added targeted unit tests plus small golden-like snapshots; column basics covered; grow/shrink/basis distribution covered.

### Status checklist
- [x] CSS: cascade, specificity, vars, selectors (type/id/class)
- [x] Flex (row): gaps, wrapping, justify-content, align-items, align-content, align-self, basis/grow/shrink
- [x] Flex (column): basic justify/align (nowrap)
- [x] Flex (column): wrapping, align-content across columns
 - [x] Flex: min/max constraints, clamping
  - [x] Flex: baseline (typographic baseline via provider; fallback to bottom)
 - [x] Flex: overflow clipping (Hidden)
- [x] Style: parser (minimal subset), pseudo-classes, basic media queries (gating)
- [~] Style: inheritance — `color` + text props (font-weight/style/decoration) inherit; remaining to follow as properties land
- [x] Style values: px, % (resolved for sizing/flex-basis), named colors, rgb()/rgba()
- [x] Text: grapheme segmentation, width measurement, wrapping strategies (MVP)
- [~] Parity: Playwright harness scaffolded with initial fixtures; Yoga ports pending

## Gaps / Next Steps

- Flex
  - [x] Baseline alignment (true typographic baseline)
- Style
  - [~] Media invalidation/propagation: node-level invalidation implemented for media-dependent entries; track rule→node maps for more surgical updates and add benchmarks/tests
  - [ ] Inheritance table: document remaining inheritable properties (e.g., future text/typography props) and implement as they land
  - [ ] Additional units beyond px/% (e.g., em/rem) if needed
- Text
  - [x] Grapheme segmentation (MVP)
  - [x] Width measurement (MVP)
  - [x] Wrapping strategies (MVP)
- Parity
  - [ ] Port curated Yoga tests (gap/wrap/direction/align-content/grow-shrink)
  - [~] Playwright browser-parity harness (curated fixtures, ±1px tolerance)
    - [x] Project scaffold with Playwright
    - [x] 3 initial fixtures: row wrap + gaps, justify-content center, column wrap + align-content

## Observability
- Trace spans for measure/arrange timings (stub to log)
- Text: add micro-benchmark for wrapping throughput (BenchmarkDotNet)

## Perf Gates
- Layout throughput: 1000-node tree full layout p95 ≤ 8ms
- Text wrapping 10k chars p95 ≤ 6ms (tracked in TextBenchmarks)

## Exit Criteria
- Golden suites green and deterministic
- Layout invariants enforced; property tests stable
- No known grapheme width errors on fixture pack; O(added length) wrapping upheld by benchmark telemetry

## Risks & Mitigations
- Flex edge-cases → adopt W3C test-inspired fixtures
- Unicode surprises → wide test corpus and fuzzers
