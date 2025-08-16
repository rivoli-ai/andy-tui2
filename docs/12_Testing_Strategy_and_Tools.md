# Testing Strategy & Tools

## Determinism Mode
Virtual clock, seeded RNG, single-thread scheduler, stubbed I/O, fixed capabilities.

## Oracles
- **Virtual Screen Oracle**: decode ANSI bytes → virtual grid; must equal CellGrid.
- **Layout Invariants**: parent containment, no negative sizes, clip correctness.

## Test Pyramid
- **Unit**: pure stages (compose/style/layout/paint/diff/encode).
- **Golden**: Boxes, CellGrid, Bytes snapshots.
- **Property/Fuzz**: layout trees, reconciliation, unicode gremlins.
- **Integration**: PTY runs (resize/scroll/back-pressure); differential dirty vs full repaint.
- **Soak/Perf**: frame budgets; memory growth; dropped frames.

## Tooling
- `make test-deterministic`
- `make regen-goldens`
- `make trace` (Chrome trace)
- `make replay capture=...`

## Exit Criteria
- Flake rate ~0 over 1k runs; perf gates in CI.
