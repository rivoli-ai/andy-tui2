# Virtualization & Key Widgets (List/Grid/Editor/Log Viewer)

## Purpose
Keep work O(visible area); ship flagship widgets that prove the core.

## Widgets
- **VirtualizedList** — variable row height; overscan; sticky headers.
- **VirtualizedGrid** — row+column virtualization; frozen panes; templates.
- **Editor** — piece-table, line index, incremental highlight; gutter/diagnostics.
- **RealTimeLogView** — firehose ingestion, staging drains, coalescing, auto-scroll.

## Implementation Plan
1. VirtualizedList (row windowing + templates).
2. Grid (row & column windows; cell recycle).
3. LogView (bounded MPSC queue; per-frame budgets; scroll shift).
4. Editor (piece-table + viewport; IME; multi-cursor later).

## Test Plan
- Scroll storms; append storms; resize storms — FPS constant.
- Virtualization correctness (indices, selection, sticky headers).
- Editor typing latency p95 threshold.

## Exit Criteria
- Each widget meets perf acceptance thresholds (see perf plan).
