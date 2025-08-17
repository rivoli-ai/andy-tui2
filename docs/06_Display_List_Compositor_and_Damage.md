# Display List, Compositor & Damage

## Purpose
Backend-agnostic drawing IR + terminal cell compositor + precise damage model.

## Interfaces
- Display ops: `Rect`, `Border`, `TextRun`, `Line`, `LayerPush`, `ClipPush`, `Pop`, `Shadow`.
- `buildDisplayList(boxes, styles, content) -> DL`
- TTY: `composite(DL) -> CellGrid`, `damage(old, new) -> DirtyRects`, `rowRuns(cells, dirty) -> Runs`

### IR and Builder naming
- IR uses noun ops suitable for diff/hash: `Rect`, `Border`, `TextRun`, `LayerPush`, `ClipPush`, `Pop` (optional: `Line`, `Shadow`).
- Builder exposes verb methods to construct the IR: `DrawRect`, `DrawBorder`, `DrawText`, `PushLayer`, `PushClip`, `Pop`.

## Implementation Plan
1. **Display List Builder**
   - Walk boxes; emit ops in z-order; clip per node.
2. **TTY Compositor**
   - Offscreen `Cell[w×h] = {grapheme, fg, bg, attrs, w}`; blend layers.
3. **Damage**
   - Node dirtiness → rects; union/clip; scroll shifting optimization.
4. **Row-run extraction**
   - For each dirty row, group contiguous cells with same attrs → run.

## Test Plan
- Goldens: borders/padding/overlays/popups; clipping.
- Dirty vs full repaint differential equality.
- Scroll shifting reduces runs; bytes drop.

## Exit Criteria
- Consistent cell grid from DL; dirty % tracks changed area; bytes/frame minimized.
