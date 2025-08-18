# Phase 2 — Rendering Core (Week 6–8)

## Outcomes
- Unified, single-path rendering pipeline implemented and validated
- Display List (DL) defined; compositor and damage tracking operational
- Terminal backend encoder and PTY I/O abstraction ready
- Observability baseline: structured logs and tracing spans across stages

## Scope
- Resolve architecture issues by eliminating dual visitor/RenderElement paths
- Implement DisplayList → Compositor → Backend pipeline (TTY first)
- Standardize naming and API contracts across docs and code

## Non-goals
- Additional backends (Web/Native) beyond stable interfaces

## Deliverables
- `src/Andy.Tui.DisplayList`, `src/Andy.Tui.Compositor`, `src/Andy.Tui.Backend.Terminal`
- End-to-end tests using `VirtualScreenOracle` (Cells/Bytes)
- Logging/tracing baseline in `src/Andy.Tui.Observability`

## Architecture Decisions
- Pipeline: Compose → Style → Layout → DisplayList → Compositor → Backend
- Visitors contribute to layout and DL only; there is no direct rendering from nodes
- Clipping is modeled as explicit DL push/pop; compositor enforces z-order and clips
- TTY uses cell damage → row runs; non-TTY uses DL diff

### Cross-doc consistency
- Standardize DL op naming to noun-based IR with a verb-based builder:
  - IR ops: `Rect`, `Border`, `TextRun`, `LayerPush`, `ClipPush`, `Pop` (optional: `Line`, `Shadow` — may be deferred)
  - Builder API: `DrawRect`, `DrawBorder`, `DrawText`, `PushLayer`, `PushClip`, `Pop`
- Standardize oracle naming to `VirtualScreenOracle` with `Decode(bytes) -> CellGrid`
- Ensure `docs/06_Display_List_Compositor_and_Damage.md` reflects the same op names (including `LayerPush`/`ClipPush`/`Pop`)

## API & Types

### Display List (IR)
- Immutability per frame; creation via builder
- Emission/painter’s order defines z-order unless an explicit `zIndex` exists; if present, stable sort by `(layer, zIndex, sequence)`
- Ops (IR nouns):
  - `Rect { x, y, width, height, fill, stroke?, strokeStyle? }`
  - `Border { x, y, width, height, style, color }`
  - `TextRun { x, y, content, fg, bg?, attrs }` (attrs are flags; bg is optional/transparent)
  - `LayerPush { id?, opacity? }`
  - `ClipPush { x, y, width, height }`
  - `Pop`
  - Optional/future: `Line { x1, y1, x2, y2, style, color }`, `Shadow { x, y, width, height, offsetX, offsetY, blur, color }`
- Builder verbs:
  - `DrawRect(Rect rect)`
  - `DrawBorder(Border border)`
  - `DrawText(TextRun text)`
  - `PushLayer(LayerPush layer)`
  - `PushClip(ClipPush clip)`
  - `Pop()`

### Compositor (TTY)
- `Composite(DisplayList dl, Size viewport) -> CellGrid`
- `Damage(CellGrid previous, CellGrid next) -> DirtyRects`
- `RowRuns(CellGrid next, DirtyRects dirty) -> IReadOnlyList<RowRun>`
- Types:
  - `Cell { grapheme: string, width: byte(1|2), fg: Rgb24, bg: Rgb24, attrs: CellAttrFlags }`
  - `RowRun { row, colStart, colEnd, attrs, text }`
  - `CellAttrFlags` include: bold, faint, italic, underline (single/double), strikethrough, reverse, hyperlink, dim, blink (normally disabled)

### Terminal Backend
- `Encode(IReadOnlyList<RowRun> runs, TerminalCapabilities caps) -> ReadOnlyMemory<byte>`
- `IPtyIo { WriteAsync(ReadOnlyMemory<byte> frameBytes, CancellationToken) }`
- `TerminalCapabilities { trueColor: bool, palette256: bool, hyperlinks: bool, underlineStyles: enum, ... }`

### Oracle
- `VirtualScreenOracle.Decode(ReadOnlySpan<byte> bytes, Size viewport) -> CellGrid`
- Used to validate: `Encode(RowRuns) ≅ Composite(DL)` at the cell/bytes level

## Detailed Semantics

### Display List semantics
- Frame immutability; ops are value types suitable for cheap diff/hash
- Text rendering: `TextRun` draws glyphs only; background fill requires an explicit `Rect`
- Clipping: push/pop stack; nested clips intersect; intersection happens in DL space prior to rasterization
- Colors: internal 24-bit RGB; downsampling occurs in terminal backend

### Compositing rules (TTY)
- Overwrite model (no alpha): later ops override earlier cells; text does not paint the background unless `bg` is set
- Borders and lines resolve corners deterministically; choose precedence order (e.g., heavy > light > ascii)
- Double-width glyph policy: if `width=2` at last column, use fallback policy (truncate to a single-space placeholder or replace with `?`); the policy is fixed and shared with the oracle

### Damage model
- Sources: node dirtiness → layout boxes → DL coverage → dirty rects
- Merging: union/clip, then per-row coalescing into runs with a configurable merge threshold
- Scroll optimization: detect viewport scroll delta; shift previous `CellGrid` and restrict repaint to exposed rows
- Budgeting: optional cap on dirty % per frame with carry-over for stability under churn

### DL diffing
- Primary for non-TTY backends; define op equality and hashing
- Prefer cell damage → row runs for TTY due to terminal constraints

### Terminal encoding & capability negotiation
- Detect capabilities (16/256/truecolor, hyperlinks, underline variants)
- Emit minimal SGR diffs between sequential runs; avoid global resets except when strictly cheaper
- Guarantee single buffered write per frame; chunk only if required by I/O constraints without interleaving frames

## Tasks & Sequencing
- [x] Unify DL op naming across docs and code; implement builder with verbs
- [x] Define DL ops and builders; integrate with layout boxes (builder in place; layout-box traversal integration pending)
- [x] Implement compositor with damage heuristics and scroll detection (row-runs + scroll detection implemented)
- [~] Implement terminal ANSI encoder and PTY abstraction with capability detection (encoder + capabilities + IPtyIo in place; detection heuristics TBD)
- [~] Observability: structured logging categories and spans per stage (baseline initializer available; categories/spans wiring pending)
- [x] End-to-end DL → Cells → Bytes round-trips with golden tests (text placement + colors/attrs parity via oracle implemented)

## Testing
- Unit
  - DL building (nested clips, z-order, overlapping rect+text)
  - Compositor blending (text over background, borders meeting at corners)
  - Double-width and combining glyph edge policies
  - Damage merging and thresholds; scroll detection behavior
  - Encoder SGR minimalization; truecolor→256 downsample parity; hyperlink enable/disable
- Golden
  - `CellGrid` snapshots for curated fixtures
  - Bytes snapshots round-tripped via `VirtualScreenOracle`
- Integration
  - Dirty vs full repaint produce identical `CellGrid` and bytes

## Observability
- Initialize comprehensive logging in tests via `ComprehensiveLoggingInitializer.Initialize(isTestMode: true)`
- Define logging categories: Compose, Style, Layout, DisplayList, Compositor, Damage, Encoder, Backend
- Trace spans per stage with parent/child nesting; support Chrome Trace export

## Progress (2025-08-17)

- DisplayList: IR noun ops (`Rect`, `Border`, `TextRun`, `LayerPush`, `ClipPush`, `Pop`) and verb-based `DisplayListBuilder` implemented. Invariants validator added (clip intersection, push/pop balance, monotonic ordering guard). Cross-doc op naming aligned.
- Compositor (TTY): MVP implemented producing `CellGrid`; overwrite model; clipping respected; borders rendered (box-drawing); row-run extraction groups by attrs and colors; damage model computes per-row rects and includes vertical scroll detection.
- Terminal Backend: ANSI encoder implemented with minimal-diff across runs for attrs and fg/bg; truecolor/256/16 color mapping using salvage algorithms; `TerminalCapabilities` + `IPtyIo` defined.
- Oracle: `VirtualScreenOracle` added for cursor-position/text placement parity (SGR colors/attrs parsing not yet implemented).
- Observability: Minimal `ComprehensiveLoggingInitializer` added for test mode; full categories/spans pending.
- Tests (green):
  - DisplayList: build/balance, invariants (empty-intersection, stray pop, unbalanced push).
  - Compositor: rect+text layering, text background policy (with/without bg), clipping, borders, row-run grouping, damage (multi-rect, no-diff), z-order overwrite.
  - Encoder: truecolor/256/16 color emissions; minimal redundant SGR sequences; e2e multi-run cursor moves; DL→Cells→Runs→Encode→Decode text placement parity.

### Additional progress (2025-08-18)

- Scroll dirty budget test (≤ 12%) and bytes-per-frame budget tests added.
- Curated parity fixtures validate colors/attrs round-trips on multi-run frames.
- Wide-glyph edge policy implemented (placeholder at last column); test scaffold in place.
- One-write-per-frame validated via `FrameWriter`.
- Examples: added simple terminal examples demonstrating DL→Compositor→Encoder pipeline.

## Perf Gates
- One write per frame (TTY) — enforced via `FrameWriter` test
- Dirty ≤ 12% for scrolling log scenarios — covered by `ScrollDirtyBudgetTests`
- Full repaint 200×60 p95 ≤ 25ms — smoke test added; convert to budgeted perf test in CI
- Budget asserts: bytes/frame on curated scenarios — `BytesPerFrameBudgetTests` added

## Exit Criteria
- Oracle parity: `Encode(RowRuns)` equals `Composite(DL)` for curated fixtures — basic and multicolor fixtures added (attrs/colors parsed)
- Differential tests: equality between dirty and full repaint paths — basic parity test added; extend to borders/clipping
- Observability: categories exposed; spans/timings wiring pending; test ensures logging low overhead in test mode

## v1 salvage for Phase 2
- See `docs/22_Salvage_Audit_from_v1.md` for details. For this phase:
  - Adopt: ANSI color mapping helpers and test vectors (truecolor→256→16 conversions)
  - Adopt/Adapt: Display list invariants (clip stack balance; layer monotonic) into `Andy.Tui.DisplayList`
  - Adapt: Terminal double-buffer/dirty concepts into `Andy.Tui.Compositor` row-run model
  - Adapt: `ComprehensiveLoggingInitializer` pattern into `Andy.Tui.Observability` with test mode
  - Use as naming reference only: terminal capabilities interface; re-express as `TerminalCapabilities` + `IPtyIo`

## External inspirations (not pipeline copies)
- Textual: region damage minimization; capability-based styling fallbacks
- Ink: ANSI diffing strategies; snapshot testing for terminal output
- Bubbletea: single buffered write discipline and simple render loop

## Risks & Mitigations
- Double-render regressions → unified pipeline implemented; tests ensure DL→Compositor→Backend path; no direct node rendering
- Damage thrash → added scroll detection; dirty % budget tests; consider coalescing thresholds if needed
- Grapheme width surprises → wide-glyph edge policy implemented; add more fixtures; align compositor/oracle width logic next
