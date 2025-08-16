# Andy.Tui v2 (.NET 8)

A modern, reactive TUI framework for .NET 8 with a SwiftUI-like DSL, WPF-style bindings, a pragmatic CSS subset, a unified rendering pipeline, and first-class observability. Built for high-performance dashboards, editors, and real-time log viewers with deterministic tests and perf gates.

## Features (high level)
- Reactive core (signals, computed, effects), bindings, converters, validators, commands
- Declarative DSL for composing views and modifiers
- CSS subset: cascade, specificity, variables, pseudo-classes, media
- Layout engine (Flex MVP), Text engine (graphemes, widths, wrapping)
- Unified pipeline: Compose → Style → Layout → DisplayList → Compositor → Backend
- Backends: Terminal (ANSI encoder). Web/Native planned
- Observability: structured logging, trace spans, HUD, capture/replay
- Deterministic mode for stable tests; Virtual Screen oracle

## Repository structure
- `src/` — library projects (packable)
  - `Andy.Tui.Core`
  - `Andy.Tui.Compose`
  - `Andy.Tui.Style`
  - `Andy.Tui.Layout`
  - `Andy.Tui.Text`
  - `Andy.Tui.DisplayList`
  - `Andy.Tui.Compositor`
  - `Andy.Tui.Backend.Terminal`
  - `Andy.Tui.Backend.Web`
  - `Andy.Tui.Backend.Native`
  - `Andy.Tui.Input`
  - `Andy.Tui.Animations`
  - `Andy.Tui.Virtualization`
  - `Andy.Tui.Widgets`
  - `Andy.Tui.Observability`
- `tests/` — xUnit test projects (non-packable)
- `docs/` — design, roadmap, phases, testing, perf plans
- `assets/` — icons and images used for documentation and NuGet packaging

## Getting started
Prerequisites: .NET SDK 8.0+

- Restore and build:
  - `dotnet restore`
  - `dotnet build -c Release`
- Run tests:
  - `dotnet test -c Release`
- Code coverage (optional):
  - `dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults`
  - `reportgenerator -reports:"./TestResults/*/coverage.cobertura.xml" -targetdir:"./TestResults/CoverageReport" -reporttypes:Html`

## Packing and publishing
All libraries under `src/` are packable. Packaging metadata is centralized via `Directory.Build.props` (SourceLink, icon, README).

- Pack all libraries:
  - `dotnet pack -c Release -o ./nupkg`
- Publish a package to NuGet (example for Core):
  - `dotnet nuget push ./nupkg/Andy.Tui.Core.0.1.0.nupkg -k <NUGET_API_KEY> -s https://api.nuget.org/v3/index.json`

Tip: Consider publishing only the surface packages initially (e.g., `Andy.Tui.Core`, `Andy.Tui.Compose`, `Andy.Tui.DisplayList`, `Andy.Tui.Compositor`, `Andy.Tui.Backend.Terminal`) and mark pre-release versions until APIs stabilize.

## Development workflow
- Format: `dotnet format`
- Install pre-commit hooks: `./scripts/setup-git-hooks.sh` (macOS/Linux)
- Tests must accompany code changes to `src/`
- Run tests before committing significant changes: `dotnet test`
- Generate coverage for meaningful refactors/features

## Roadmap & phase plans
- Integration roadmap: `docs/13_Integration_Roadmap.md`
- .NET implementation plan: `docs/15_DotNet_Implementation_Plan.md`
- Detailed phases:
  - `docs/16_Phase_0_Foundations.md`
  - `docs/17_Phase_1_Visual_Core.md`
  - `docs/18_Phase_2_Rendering_Core.md`
  - `docs/19_Phase_3_Interactivity_Animations.md`
  - `docs/20_Phase_4_Virtualization_Widgets.md`
  - `docs/21_Phase_5_Additional_Backends.md`
- Legacy salvage audit (what to adopt/adapt/avoid): `docs/22_Salvage_Audit_from_v1.md`

## Status
The project is scaffolded with solution and projects, tests run green on templates, and NuGet packages pack successfully. Implementation proceeds by phases per the docs.

## License
MIT (see license file in repository when added).
