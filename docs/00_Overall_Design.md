# Overall Design

## Goal
A cross-backend, high-performance, reactive UI library capable of Bloomberg/VS Code-class TUIs, real-time log viewers, and dashboard-grade widgets — with a SwiftUI-like DSL, WPF-style bindings, CSS subset, deterministic tests, and first-class observability.

## Architecture (layers)

1. **Reactive Core & Bindings** — signals/vars/computed/effects; two-way bindings, converters, validators, commands.
2. **Compose & Widget Tree (SwiftUI-like DSL)** — declarative views, modifiers, templates.
3. **CSS & Style System** — cascade, specificity, variables, media queries, state pseudo-classes.
4. **Layout Engine** — Flex (MVP), later Grid; viewport and DPI-aware.
5. **Text Engine** — grapheme segmentation, widths, wrapping; line shaping caches.
6. **Display List** — backend-agnostic drawing ops + text runs + layers/clips.
7. **Compositor, Damage, Diff** — per-cell compositor (TTY), dirty rects → row runs; full DL diff for Web/Native.
8. **Backends** — Terminal (ANSI encoder), Web (Canvas/DOM), Native (Skia/WGPU).
9. **Input/Focus/A11y** — keyboard/mouse/IME; focus model; status line for TTY; DOM semantics on Web.
10. **Animations & Frame Pacing** — transitions/keyframes, scheduler with target FPS.
11. **Virtualization & Key Widgets** — Virtualized List/Grid, Editor core, Real-time Log Viewer.
12. **Observability** — structured logging, tracing spans, HUD, frame capture & replay.
13. **Testing & Quality** — deterministic runtime, Virtual Screen oracle, PTY tests, fuzz/property tests.

## End-to-end frame lifecycle


## Non-goals (initial)
- Full CSS spec; we ship a pragmatic subset.
- Pixel-perfect RTL shaping on TTY (best-effort only).

## Cross-cutting guarantees
- Determinism mode for tests.
- One write per frame (TTY).
- O(visible area) work via virtualization.
- Crash-safe terminal restore; capture last N frames on fatal.
