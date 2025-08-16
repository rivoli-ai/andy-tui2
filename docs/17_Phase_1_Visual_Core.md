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
- [ ] Implement CSS cascade and specificity with variables
- [ ] Implement pseudo-states and media hooks
- [ ] Implement Flex layout measure/arrange
- [ ] Implement text segmentation, width calc, and wrapping strategies
- [ ] Compose integration: map DSL props to style/layout nodes

## Testing
- Unit: selector specificity, variable resolution, flex computations
- Golden: styled boxes and layout trees snapshots; invariants (no negative sizes, containment)
- Property: random layout trees respecting constraints

## Observability
- Trace spans for measure/arrange timings (stub to log)

## Perf Gates
- Layout throughput: 1000-node tree full layout p95 ≤ 8ms
- Text wrapping 10k chars p95 ≤ 6ms

## Exit Criteria
- Golden suites green and deterministic
- Layout invariants enforced; property tests stable

## Risks & Mitigations
- Flex edge-cases → adopt W3C test-inspired fixtures
- Unicode surprises → wide test corpus and fuzzers
