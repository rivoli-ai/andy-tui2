# Layout Engine (Flex First)

## Purpose
Compute boxes (x,y,w,h,clip) for nodes from styles + content + viewport.

## Interfaces
- `layout(styledTree, viewport, textMetrics) -> Boxes`
- Boxes: map of `nodeId -> {x,y,w,h, scroll, clip}`

## Implementation Plan
1. **Measurement**
   - Text measurement via Text Engine; widgets implement `measure()`.
2. **Flex Layout**
   - Direction, wrap, grow/shrink, basis, gap; min/max constraints.
3. **Placement & Clipping**
   - Absolute/relative positioning; z-index order.
4. **Grid (Phase 2)**
   - Track placement, fractions, minmax, auto-flow.

## Test Plan
- Invariants: children inside parents; no negative sizes; gaps sum correctly.
- Goldens: common flex patterns; responsive relayout on width change.
- Fuzz: random trees with sane ranges.

## Exit Criteria
- P50 ≤ 1.5ms for 1k nodes @ 200×60; all invariants enforced.
