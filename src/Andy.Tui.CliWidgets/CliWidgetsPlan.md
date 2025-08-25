# Andy.Tui.CliWidgets – Plan

Scope: a focused CLI assistant widgets pack, separate from core widgets, with its own library, tests and examples so it can be carved out later.

Initial widget set (MVP):
- CommandOutput: stream, paginate and colorize CLI output
- KeyHints: always-on footer with keybindings
- PromptLine: input line with history and inline suggestions (ghost text)
- Toast/Status: transient messages and long-running status with spinner
- TaskList: tasks with progress and states (queued/running/success/fail)
- FilePicker (CLI-flavored): minimal directory/file selector (non-mouse)
- LogTail: follow file/stdout with filters and levels
- PanelLayout: top header, bottom hints, left nav, right content helpers

Phases:
1) Infrastructure: csproj, tests csproj, examples csproj, references, CI hooks
2) Core rendering primitives bridge (DL+Layout) and base helpers
3) Implement KeyHints + Toast/Status + Spinner; tests, example
4) Implement PromptLine (history, suggestions, basic editing); tests, example
5) Implement CommandOutput (append, pagination, ANSI color passthrough); tests, example
6) Implement TaskList (API to update tasks, renders progress); tests, example
7) Implement PanelLayout helpers; refactor examples to use
8) Implement LogTail (follow file), filters; tests, example
9) Implement FilePicker (CLI); tests, example

Out-of-scope: mouse interactions, complex virtualized grids (reuse existing if needed), networking.

Notes:
- Examples may temporarily depend on local tools in `~/devel/rivoli-ai/` when needed; we keep a thin adapter.
- Keep TFM at net8.0 to align with main solution.
