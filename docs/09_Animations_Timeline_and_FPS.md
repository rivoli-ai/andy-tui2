# Animations, Timeline & FPS Control

## Purpose
Drive `transition`/`@keyframes` and maintain target FPS under load.

## Interfaces
- `timeline.schedule(anim)`, `advance(time)`, `setTargetFps(n)`.
- HUD stats API: `getFrameStats()`.

## Implementation Plan
1. **Timeline**
   - Absolute time; easing; property interpolation.
2. **Frame Scheduler**
   - Target FPS pacing (sleep/spin); adaptive step-down; frame skip for non-essential anims.
3. **HUD**
   - FPS instant/EMA; per-stage ms; dirty %, bytes/frame; queue depth.

## Test Plan
- Deterministic clock; animations produce expected keyframes.
- Overload: FPS stays at or near target; adaptive policies engage.

## Exit Criteria
- Stable pacing at 15/30/60; HUD visible; metrics logged.
