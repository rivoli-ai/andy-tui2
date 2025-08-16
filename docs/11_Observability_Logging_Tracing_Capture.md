# Observability: Logging, Tracing, HUD & Capture

## Purpose
See everything: logs per stage, trace spans for Chrome/Perfetto, on-screen HUD, frame capture & replay.

## Features
- **Structured logging** (trace/debug/info/warn/error) with categories.
- **Tracing spans** with nesting; export to Chrome Trace.
- **HUD** with FPS, stage times, bytes, dirty %, queue depth.
- **Frame capture**: inputs, state diffs, Boxes, DL, CellGrid, bytes, logs — replayable.

## Implementation Plan
1. Logging facade with category filters; ring buffer + file sink.
2. Span API; automatic bracketing of pipeline stages.
3. HUD overlay; toggles; heatmaps.
4. Capture writer/reader; replay harness.

## Test Plan
- Verify minimal overhead at `info`; sampling at `trace`.
- Replay reproduces frames deterministically.

## Exit Criteria
- Devs can diagnose “why slow/what changed” in one capture.
