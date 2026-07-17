# Andy.Tui v2 (.NET 8)

[![NuGet](https://img.shields.io/nuget/vpre/Andy.Tui)](https://www.nuget.org/packages/Andy.Tui/)
[![CI](https://github.com/rivoli-ai/andy-tui2/actions/workflows/ci.yml/badge.svg)](https://github.com/rivoli-ai/andy-tui2/actions/workflows/ci.yml)
[![Build and Release](https://github.com/rivoli-ai/andy-tui2/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/rivoli-ai/andy-tui2/actions/workflows/build-and-release.yml)

A modern, reactive TUI framework for .NET 8 with declarative components, reactive bindings, a pragmatic CSS subset, a unified rendering pipeline, and first-class observability. Built for high-performance terminal applications, dashboards, and real-time log viewers.

> ⚠️ **ALPHA RELEASE WARNING** ⚠️
> 
> This software is in ALPHA stage. **NO GUARANTEES** are made about its functionality, stability, or safety.
> 
> **CRITICAL WARNINGS:**
> - This library performs **DESTRUCTIVE OPERATIONS** on files and directories
> - Permission management is **NOT FULLY TESTED** and may have security vulnerabilities
> - **DO NOT USE** in production environments
> - **DO NOT USE** on systems with critical or irreplaceable data
> - **DO NOT USE** on systems without complete, verified backups
> - The authors assume **NO RESPONSIBILITY** for data loss, system damage, or security breaches
> 
> **USE AT YOUR OWN RISK**

## Features
- **Reactive Core**: Signals, computed values, effects, and data bindings
- **Component System**: Declarative component composition with modifiers
- **CSS Styling**: Subset of CSS with cascade, specificity, variables, pseudo-classes
- **Flex Layout**: Modern flexbox-based layout engine
- **Rich Text**: Unicode-aware text rendering with grapheme support
- **Widget Library**: 80+ pre-built widgets (tables, forms, charts, etc.)
- **Unified Pipeline**: Compose → Style → Layout → DisplayList → Compositor → Backend
- **Terminal Backend**: Full ANSI/escape sequence support
- **Observability**: Built-in logging, tracing, performance monitoring
- **Testing**: Deterministic mode for reliable unit tests

## Repository structure
- `src/` — library projects (packable)
  - `Andy.Tui` (umbrella meta-package)
  - `Andy.Tui.Core`
  - `Andy.Tui.Compose`
  - `Andy.Tui.Style`
  - `Andy.Tui.Layout`
  - `Andy.Tui.Text`
  - `Andy.Tui.DisplayList`
  - `Andy.Tui.Compositor`
  - `Andy.Tui.Backend.Terminal`
  - `Andy.Tui.Input`
  - `Andy.Tui.Animations`
  - `Andy.Tui.Virtualization`
  - `Andy.Tui.Widgets`
  - `Andy.Tui.CliWidgets`
  - `Andy.Tui.Observability`
- `tests/` — xUnit test projects (non-packable)
- `docs/` — design, roadmap, phases, testing, perf plans
- `assets/` — icons and images used for documentation and NuGet packaging

## Installation

### Via NuGet Package Manager
```bash
dotnet add package Andy.Tui --prerelease
```

### Via PackageReference
```xml
<PackageReference Include="Andy.Tui" Version="*-rc.*" />
```

> **Note**: Pre-release packages are published automatically for every commit to main branch.

### Package model
- `Andy.Tui` is a **dependency meta-package**: it ships no assemblies of its own and instead declares NuGet dependencies on every library, so a single reference pulls in the whole framework. Each library is also published as its own package and can be referenced individually.
- `Andy.Tui.CliWidgets` is an **opt-in add-on** package that depends on the `Andy.Tui` meta-package. It is not pulled in by default (that would create a dependency cycle); add it explicitly when you want the CLI-focused widgets:
  ```bash
  dotnet add package Andy.Tui.CliWidgets --prerelease
  ```

## Getting started

### Prerequisites
- .NET SDK 8.0 or later
- Terminal with ANSI color support (most modern terminals)

### Building from source
  - `dotnet restore`
  - `dotnet build -c Release`
- Run tests:
  - `dotnet test -c Release`
- Reproduce CI exactly (restore, build the complete graph, then run every test
  project after confirming its binary was produced by the build):
  - `scripts/ci-graph-test.sh --configuration Debug`
  - `scripts/ci-graph-test.sh --configuration Release`
  - Both the `ci` and `Build and Release` workflows run this same script, so a
    local green run matches CI. Add `--require-clean` to enforce a clean
    checkout, and set `RUN_PARITY=true` to include the Playwright parity suite.
- Code coverage (optional):
  - `dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults`
  - `reportgenerator -reports:"./TestResults/*/coverage.cobertura.xml" -targetdir:"./TestResults/CoverageReport" -reporttypes:Html`

## Package Publishing

### Automated Publishing
The CI/CD pipeline automatically publishes NuGet packages:
- **Pre-release versions** (e.g., `2025.8.25-rc.30`): Published on every push to `main` branch
- **Release versions** (e.g., `1.0.0`): Published when creating a tag matching `v*`

### Manual Publishing
To manually pack and publish:
```bash
# Pack all libraries
dotnet pack -c Release -o ./nupkg

# Publish to NuGet (requires API key)
dotnet nuget push ./nupkg/Andy.Tui.*.nupkg -k <NUGET_API_KEY> -s https://api.nuget.org/v3/index.json
```

## Development workflow
- Format: `dotnet format`
- Tests must accompany code changes to `src/`
- Run tests before committing significant changes: `dotnet test`
- Generate coverage for meaningful refactors/features

## Documentation

- 📖 [Getting Started](docs/GETTING_STARTED.md) - Installation, basic concepts, and examples
- 🏗️ [Architecture](docs/ARCHITECTURE.md) - System design and rendering pipeline
- 🎨 [Widget Catalog](docs/WIDGETS.md) - Complete list of 80+ UI components
- 📚 [Documentation Index](docs/README.md) - Full documentation overview

## Current Status

### Phase Progress
- ✅ **Phase 0**: Foundations - Complete
- ✅ **Phase 1**: Visual Core - Complete  
- ✅ **Phase 2**: Rendering Core - Complete
- ✅ **Phase 3**: Interactivity & Animations - Complete
- ✅ **Phase 4**: Virtualization & Widgets - Complete (80+ widgets implemented)
- 🚧 **Phase 5**: Additional Backends - Planned

### CI/CD Status
- ✅ Automated builds on push/PR
- ✅ Test suite runs (with performance tests skipped in CI)
- ✅ NuGet package publishing to nuget.org
- ⚠️ Some Playwright tests disabled pending CI browser setup

### Known Issues
- Performance tests may fail in CI due to environment variability
- Playwright browser tests require manual browser installation
- Some widget rendering edge cases in complex layouts

## Contributing

Contributions are welcome! Please:
1. Run tests before submitting PRs: `dotnet test`
2. Follow existing code style (use `dotnet format`)
3. Add tests for new functionality
4. Update documentation as needed

## License

Apache-2.0 License. See [LICENSE](LICENSE) file for details.

## Support

- **Issues**: [GitHub Issues](https://github.com/rivoli-ai/andy-tui2/issues)
- **Discussions**: [GitHub Discussions](https://github.com/rivoli-ai/andy-tui2/discussions)

---

**Remember**: This is ALPHA software. Use at your own risk and always maintain backups.
