# Integration Roadmap

See also: `.NET Implementation Plan` in `docs/15_DotNet_Implementation_Plan.md` for concrete solution structure, milestones, and acceptance criteria.

Detailed phase plans:
- Phase 0 — Foundations: `docs/16_Phase_0_Foundations.md`
- Phase 1 — Visual Core: `docs/17_Phase_1_Visual_Core.md`
- Phase 2 — Rendering Core: `docs/18_Phase_2_Rendering_Core.md`
- Phase 3 — Interactivity & Animations: `docs/19_Phase_3_Interactivity_Animations.md`
- Phase 4 — Virtualization & Widgets: `docs/20_Phase_4_Virtualization_Widgets.md`
- Phase 5 — Additional Backends: `docs/21_Phase_5_Additional_Backends.md`

Legacy reuse plan:
- Salvage audit: `docs/22_Salvage_Audit_from_v1.md` — adopt/adapt/avoid list mapped to v2 modules.

## Phase 0 — Foundations (Week 1–2)
- Reactive Core & Bindings (01)
- Determinism runtime + test harness (12 – Determinism)
- Minimal DSL skeleton (02)

**Integration:** compose → bindings → recomposition tests.

## Phase 1 — Visual Core (Week 3–5)
- CSS subset (03)
- Layout Flex (04)
- Text Engine (05)

**Integration:** DSL → CSS → Layout → Text measure → Boxes goldens.

## Phase 2 — Rendering Core (Week 6–8)
- Display List & Compositor & Damage (06)
- Terminal Backend encoder + I/O (07 – TTY)
- Observability baseline (11 – logs/spans)

**Integration:** DL → Cells → Diff/Encode → Virtual Screen oracle.

## Phase 3 — Interactivity & Anim (Week 9–10)
- Input/Focus (08)
- Animations & FPS/HUD (09)

**Integration:** events → compose → anim → render; HUD verified.

## Phase 4 — Virtualization & Widgets (Week 11–14)
- VirtualizedList/Grid (10)
- RealTimeLogView (10)
- Editor MVP (10)

**Integration:** widgets on terminal backend; perf targets.

## Phase 5 — Additional Backends (Week 15–18)
- Web Canvas (07 – Web)
- Native Skia/WGPU (07 – Native)

**Integration:** DL parity tests across backends.

## Ongoing — Testing & Perf (always)
- Expand fixtures, fuzzers, PTY scenarios; enforce perf gates.

### Integration Principles
- Feature flags per layer; expose seams; always keep terminal backend working.
- Differential tests whenever you introduce a fast path (dirty vs full).
- Capture & replay new failures; never “fix blind”.

