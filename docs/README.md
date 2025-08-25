# Andy.Tui v2 Documentation

## Architecture & Design

- [Overall Design](00_Overall_Design.md) - High-level architecture overview
- [Rendering Pipeline](01_Rendering_Pipeline.md) - Complete rendering flow
- [Reactive Core and Bindings](01_Reactive_Core_and_Bindings.md) - Signals, computed values, effects
- [Testing Strategy](12_Testing_Strategy_and_Tools.md) - Testing approach and tools
- [Performance Plan](14_Performance_Acceptance_Plan.md) - Performance targets and benchmarks

## Core Components

- [Compose & Widget Tree](02_Compose_Widget_Tree_and_DSL.md) - Component composition system
- [CSS & Style System](03_CSS_and_Style_System.md) - Styling with CSS subset
- [Layout Engine](04_Layout_Engine.md) - Flexbox layout implementation
- [Text Engine](05_Text_Engine.md) - Unicode text rendering
- [Display List & Compositor](06_Display_List_Compositor_and_Damage.md) - Rendering optimization

## Features

- [Input & Focus](08_Input_Focus_Accessibility.md) - Keyboard input and focus management
- [Animations](09_Animations_Timeline_and_FPS.md) - Animation system and timeline
- [Virtualization & Widgets](10_Virtualization_and_Key_Widgets.md) - Performance optimization and widget library
- [Observability](11_Observability_Logging_Tracing_Capture.md) - Logging, tracing, and debugging

## Backends

- [Backend Systems](07_Backends_Terminal_Web_Native.md) - Terminal, Web, and Native support

## Implementation Roadmap

- [Integration Roadmap](13_Integration_Roadmap.md) - Overall integration strategy
- [.NET Implementation Plan](15_DotNet_Implementation_Plan.md) - Detailed implementation guide

### Implementation Phases

1. [Phase 0: Foundations](16_Phase_0_Foundations.md) ✅ Complete
2. [Phase 1: Visual Core](17_Phase_1_Visual_Core.md) ✅ Complete
3. [Phase 2: Rendering Core](18_Phase_2_Rendering_Core.md) ✅ Complete
4. [Phase 3: Interactivity & Animations](19_Phase_3_Interactivity_Animations.md) ✅ Complete
5. [Phase 4: Virtualization & Widgets](20_Phase_4_Virtualization_Widgets.md) ✅ Complete
6. [Phase 5: Additional Backends](21_Phase_5_Additional_Backends.md) 🚧 Planned

## Migration & Legacy

- [v1 Salvage Audit](22_Salvage_Audit_from_v1.md) - What to keep from v1
- [Phase 1 Salvage Details](23_Phase_1_Salvage_Audit.md) - Specific v1 components to migrate

## Reference

- [CLI Widget Catalog](cli_code_assistant_widgets.md) - Complete widget reference (80+ widgets)