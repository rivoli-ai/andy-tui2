# Performance Acceptance Plan

## Targets (TTY, 200×60 viewport unless noted)

| Scenario | Target |
|---|---|
| Log append auto-scroll | ≥5k lines/s, ≤25% single core, ≤1 write/frame, dirty ≤12% |
| Grid 10k×30 (virt.) | Scroll 120 rows/s, p50 ≤ 8ms, p90 ≤ 14ms |
| Editor open 10 MB | ≤300ms to first paint; typing p95 ≤ 10ms |
| Markdown stream | 200 tokens/s sustained; FPS ≥ target (30–60) |
| Resize storm (2/s) | Full relayout p95 ≤ 25ms |

## Measurements
- Stage timings, bytes/frame, dirty %, FPS EMA, queue depth, drops.
- Record Chrome Trace samples for profiling.

## CI Gates
- Perf tests run nightly; fail build on regression >10%.
- Budget asserts in unit/integration tests (compose/layout/paint thresholds).

## Degradation Policy
- On budget miss: reduce overscan; skip non-essential animations; adaptive FPS step-down; coalesce log events.