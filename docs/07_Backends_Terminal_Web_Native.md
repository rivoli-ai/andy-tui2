# Backends: Terminal, Web, Native

## Terminal (ANSI/SGR)
- **Encoder:** minimal SGR changes, cursor moves; hyperlinks (OSC 8); 16/256/truecolor downsample.
- **I/O:** one buffered write per frame; back-pressure metrics.

## Web
- **Canvas** (MVP): rasterize DL; text via `fillText`; later WebGL/WebGPU for batch text/quads.
- **DOM** (optional): accessibility-friendly; slower; maps boxes to elements & CSS.

## Native
- **Skia or WGPU**: draw DL; platform text shaping (CoreText/DirectWrite/Pango).
- **Windowing**: winit/GLFW/SDL; HiDPI aware.

## Implementation Plan
1. Terminal encoder + capability detect.
2. Web Canvas renderer + ResizeObserver.
3. Native renderer (Skia/WGPU) + platform shapers.

## Test Plan
- Terminal: `VirtualScreenOracle` round-trip.
- Web/Native: pixel/regression snapshots for DL fixtures.
- Capability modes: color quantization consistency.

## Exit Criteria
- Parity across backends on DL fixtures; terminal rows×cols respected.
