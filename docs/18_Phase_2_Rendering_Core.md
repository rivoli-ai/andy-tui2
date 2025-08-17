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
- [ ] Unify DL op naming across docs and code; implement builder with verbs
- [ ] Define DL ops and builders; integrate with layout boxes
- [ ] Implement compositor with damage heuristics and scroll detection
- [ ] Implement terminal ANSI encoder and PTY abstraction with capability detection
- [ ] Observability: structured logging categories and spans per stage
- [ ] End-to-end DL → Cells → Bytes round-trips with golden tests

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

## Perf Gates
- One write per frame (TTY)
- Dirty ≤ 12% for scrolling log scenarios
- Full repaint 200×60 p95 ≤ 25ms
- Add budget asserts to unit/integration tests (bytes/frame, dirty %, stage timings)

## Exit Criteria
- Oracle parity: `Encode(RowRuns)` equals `Composite(DL)` for curated fixtures
- Differential tests prove equality between dirty and full repaint paths
- Logs/spans show stage timings; overhead minimal at info

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
- Double-render regressions → remove legacy paths; add tests that assert no direct rendering modules are present in Phase 2
- Damage thrash → coalescing thresholds and scroll detection; stabilize with dirty % cap
- Grapheme width surprises → centralized width computation shared by compositor and oracle; wide test corpus
