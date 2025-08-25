# Andy.Tui

[![NuGet](https://img.shields.io/nuget/vpre/Andy.Tui)](https://www.nuget.org/packages/Andy.Tui/)

A modern, reactive TUI (Terminal User Interface) framework for .NET 8+ with a declarative API inspired by SwiftUI and React.

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

- **Reactive Core**: Signals, computed values, and effects for state management
- **Declarative DSL**: SwiftUI-like syntax for composing views
- **CSS Styling**: Subset of CSS with cascade, specificity, variables, and pseudo-classes
- **Flex Layout**: Modern flexbox-based layout engine
- **Rich Widgets**: 80+ pre-built widgets including tables, charts, forms, and more
- **High Performance**: Optimized rendering pipeline with virtualization support
- **Cross-Platform**: Works on Windows, macOS, and Linux terminals

## Quick Start

```csharp
using Andy.Tui;

var app = new TerminalApp();
app.Run(() => 
    VStack()
        .Add(Text("Hello, TUI!").ForegroundColor(Color.Green))
        .Add(Button("Click me").OnClick(() => Console.Beep()))
);
```

## Installation

```bash
dotnet add package Andy.Tui --prerelease
```

## Documentation

For full documentation, examples, and API reference, visit:
https://github.com/rivoli-ai/andy-tui2

## License

Apache-2.0 License

## Support

- Issues: https://github.com/rivoli-ai/andy-tui2/issues
- Discussions: https://github.com/rivoli-ai/andy-tui2/discussions