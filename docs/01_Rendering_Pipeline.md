# Andy.Tui — Rendering Pipeline Overview

This document explains, at a high level, how Andy.Tui renders to a terminal, why it stays low‑CPU, and the key components involved. It is intended as a practical guide for contributors and users integrating new widgets or scenarios.

## Big picture

- Single‑threaded, frame‑oriented loop
- Immutable Display List (DL) built per frame by widgets/examples
- Compositor computes row damage and assembles row runs
- Terminal backend encodes ANSI compatible with detected capabilities (truecolor/256/16)
- One write per frame to the PTY/stdout
- Aggressive damage tracking + pacing keeps CPU low

Core assemblies:
- `Andy.Tui.DisplayList`: immutable IR for drawing (rects, text runs, borders, clips)
- `Andy.Tui.Compositor`: clipping, z‑order, row damage and run assembly
- `Andy.Tui.Backend.Terminal`: ANSI encoding and capability detection
- `Andy.Tui.Core`: `FrameScheduler` orchestrates frame timing, metrics, and write
- `Andy.Tui.Observability`: HUD overlay and timing/CPU instrumentation
- `Andy.Tui.Widgets`: imperative widgets that render into a DL builder

## Frame lifecycle

1) Build base list
- App or example builds a base `DisplayList` (`baseDl`) with background, layout scaffolding, etc.

2) Render widgets into a new builder
- Each widget implements `Render(Rect, baseDl, builder)` and records drawing ops (text/rect/border). The output is a second `DisplayList`.

3) Combine
- The base and widget lists are concatenated (or layered) to form a single frame DL.

4) Compositor pass
- Clipping and z‑order are enforced.
- Row damage is computed vs. prior frame. Only changed rows produce bytes.
- Row runs are assembled to minimize SGR/position churn.

5) Encode + write
- Terminal capabilities are auto‑detected (truecolor/256/16, cursor controls, alternate buffer).
- A single byte array is produced and written once per frame (no chunked writes).

6) Metrics / pacing
- `FrameScheduler` records timings per stage (DL build, composite, damage, encode, write).
- FPS, dirty %, and bytes/frame are tracked. Optional HUD can overlay these.
- The caller typically sleeps to a target cadence (e.g., ~30 FPS) when idle.

## Why low CPU?

- Damage based: only changed rows are encoded and written.
- One write per frame: avoids syscall overhead and terminal thrash.
- Single‑threaded: no contention, no background busy loops.
- Pacing: caller controls cadence; idle frames are cheap.
- Encoding tuned for minimal SGR churn and cursor movement.

In practice, simple animated scenarios sit well below a single core. HUD CPU shows process % normalized by core count; `top` shows per‑core %, so numbers differ by N cores.

## Threading model

- The render loop is single‑threaded and deterministic.
- Asynchrony is used only for I/O (e.g., `WriteAsync`).
- If your app produces background data (network/LLM), prefer queue + coalescing and apply updates on the main loop to keep rendering predictable.

## Terminal capability detection

- Truecolor/24‑bit preferred when available; falls back to 256 or 16 colors.
- Cursor visibility, alternate screen, and keypad/mouse modes are toggled when supported.
- Resize handling clears the screen once to prevent artifacts when the viewport grows.

## Observability

- `HudOverlay` can render: FPS, dirty %, bytes/frame, timings per stage, and process CPU.
- `IFrameMetricsSink` and `IFrameTimingsSink` can be implemented to capture metrics without the HUD.
- Comprehensive logging is available for deep diagnostics during testing.

## Widgets and layout

- Widgets are imperative with a simple `Render(rect, baseDl, builder)` contract.
- Layout helpers (`Panel`, `VStack`/`HStack`) and widgets render into a shared DL.
- Virtualization (`VirtualizedList/Grid`) realizes only visible items and integrates with the same pipeline.

## Typical loop (example)

```csharp
var scheduler = new FrameScheduler();
var hud = new HudOverlay { Enabled = true };
scheduler.SetMetricsSink(hud);
var pty = new StdoutPty();

while (running)
{
    long start = Environment.TickCount64;

    // 1) Build base
    var baseB = new DisplayListBuilder();
    baseB.PushClip(new ClipPush(0, 0, viewport.W, viewport.H));
    baseB.DrawRect(new Rect(0, 0, viewport.W, viewport.H, new Rgb24(0,0,0)));
    var baseDl = baseB.Build();

    // 2) Render widgets
    var widgets = new DisplayListBuilder();
    widget.Render(new Layout.Rect(2, 2, viewport.W - 4, viewport.H - 4), baseDl, widgets);

    // 3) Combine + optional HUD
    var combined = Combine(baseDl, widgets.Build());
    var overlay = new DisplayListBuilder();
    hud.ViewportCols = viewport.W; hud.ViewportRows = viewport.H;
    hud.Contribute(combined, overlay);

    // 4–5) Composite, encode, and write once
    await scheduler.RenderOnceAsync(Combine(combined, overlay.Build()), viewport, caps, pty, CancellationToken.None);

    // 6) Pacing (~30 FPS)
    int sleep = (int)Math.Max(0, 33 - (Environment.TickCount64 - start));
    await Task.Delay(sleep);
}
```

## Performance tips

- Avoid rebuilding large DLs when nothing changes; gate expensive work on input/state changes.
- Prefer text color changes (green/red) over structural reflow.
- Keep `Dirty %` low by limiting the number of rows updated per frame.
- For high‑throughput appenders (logs), batch appends within a frame and enable follow‑tail.

## Common questions

- "Why does HUD CPU say 0.6% but `top` shows 19%?"
  - HUD normalizes by total cores; top shows per‑core. On 8 cores, 19% top ≈ 2.4% total.
- "Is rendering multi‑threaded?"
  - No. Rendering is single‑threaded by design. Use background tasks for I/O and coalesce UI updates on the main loop.

## Where to look in code

- `src/Andy.Tui.Core/FrameScheduler.cs` — frame timings, resize handling, single write
- `src/Andy.Tui.DisplayList/` — DL ops and invariants
- `src/Andy.Tui.Compositor/` — clipping, damage, row runs, encoding preparation
- `src/Andy.Tui.Backend.Terminal/` — ANSI encoder & capabilities
- `src/Andy.Tui.Observability/HudOverlay.cs` — HUD and process CPU calculation
- `src/Andy.Tui.Widgets/` — widget implementations using the DL
