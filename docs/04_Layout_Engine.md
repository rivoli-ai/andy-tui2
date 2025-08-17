# Layout Engine (Flex First)

## Purpose
Compute boxes (x,y,w,h,clip) for nodes from styles + content + viewport.

## Data Types & Interfaces

- Size/Rect/Thickness
  - `Size { double Width, double Height }`
  - `Rect { double X, double Y, double Width, double Height }`
  - `Thickness { double Left, Top, Right, Bottom }`

- Layout Node Contract
  - `Measure(Size available) -> Size desired`
  - `Arrange(Rect final) -> void`
  - Caching policy: last-available/desired pair with versioning; invalidate on style/content change

- Tree API
  - `layout(styledTree, viewport, textMetrics) -> Boxes`
  - Boxes: map of `nodeId -> {x,y,w,h, scroll, clip}`

## Implementation Plan
1. **Measurement**
   - Text measurement via Text Engine; widgets implement `measure()`.
2. **Flex Layout**
    - Direction (row/column), wrap, align (items/self/content)
    - Order; Grow/Shrink/Basis; min/max constraints; gap (row/column)
   - Intrinsics: min-content, max-content handling via measure passes
3. **Placement & Clipping**
   - Absolute/relative positioning; z-index order
   - Scrollable containers: content size, scroll offsets, clip rects
4. **Grid (Phase 2)**
   - Track placement, fractions, minmax, auto-flow.

## Test Plan

- Invariants: children inside parents; no negative sizes; gaps sum correctly; no NaN
- Goldens: common flex patterns; responsive relayout on width change; wrapping scenarios; `order`-based reordering
- Fuzz: random trees with sane ranges and style combinations (property-based)
- Performance: synthetic 1k node tree budget assertions

## Salvage Mapping (from v1)

- Adapt invariants and box constraint tests from:
  - `tests/Andy.TUI.Layout.Tests/LayoutConstraintsTests.cs`, `LayoutBoxTests.cs`
  - Declarative layout tests for stacks and grids

## API Sketch (Phase 1)

```csharp
public interface ILayoutNode
{
    Size Measure(in Size available);
    void Arrange(in Rect finalRect);
}

public sealed record Size(double Width, double Height);
public sealed record Rect(double X, double Y, double Width, double Height);
public sealed record Thickness(double Left, double Top, double Right, double Bottom);
```

## Exit Criteria
- P50 ≤ 1.5ms for 1k nodes @ 200×60; all invariants enforced.
- Browser parity: boxes match Chromium within ±1px for curated flex fixtures; identical line breaks and wrapping decisions.

## Next Steps
- Implement gaps (`row-gap`/`column-gap`) and tests
- Implement wrapping and line generation per flex spec; tests for min/max widths and `flex-basis`
- Implement `justify-content` and `align-items/align-content`; tests for each keyword
- Implement grow/shrink distribution and clamping; property and golden tests
