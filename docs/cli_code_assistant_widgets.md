# CLI Code Assistant Widgets Specification

This document inventories the most common **"widgets"** (text elements)
that appear in CLI-based AI code assistants (Claude Code, OpenAI Codex
CLI, Gemini CLI, aider, Charmbracelet Crush, etc.). For each widget, we
outline its purpose, triggers, and behavioral specification.

------------------------------------------------------------------------

# Core interaction

## 1) Prompt bar / input editor

**Purpose**: Where the user types instructions; supports one-liners and
multi-line edits.\
**Triggers**: App start, after each agent turn, or when user presses a
hotkey to open \$EDITOR.\
**Behavior spec**\
- **Modes**: single-line, multi-line, and "open in editor".\
- **History**: ↑/↓ cycle previous prompts; `/help` lists slash
commands.\
- **Session label**: show model alias + working dir.\
- **Errors**: on send failure, keep buffer and show retry hint.

## 2) Message stream (assistant output)

**Purpose**: Streams model text as it's generated.\
**Behavior spec**\
- **Streaming**: token-by-token with cancellation.\
- **Markdown**: render formatting; paginate long output.\
- **Copy affordances**: easy copying for code blocks.\
- **Timeouts**: footer with "/continue".

## 3) Thinking/working indicator

**Purpose**: Show the agent is reasoning or waiting on a tool.\
**Behavior spec**\
- Spinner + status, elapsed time, cost.\
- Success/failure icons.

------------------------------------------------------------------------

# Tools & safety

## 4) Tool-call block (generic)

**Purpose**: Display "agent decided to use a tool".\
**Behavior spec**\
- Header with tool and args.\
- Body: preview of execution.\
- Footer: result summary, duration.

## 5) Permission prompt (approval gate)

**Purpose**: Keep risky actions safe.\
**Behavior spec**\
- Prompt: yes/no/always.\
- Scopes: once, session, always.\
- Modes: plan-only, auto-accept-edits, bypass.\
- Audit: log rules.

## 6) Shell command transcript

**Purpose**: Run and show terminal commands with output.\
**Behavior spec**\
- Header: cwd + command.\
- Stdout/stderr: streamed, truncated with "show more".\
- Exit code & duration.

------------------------------------------------------------------------

# Files & diffs

## 7) File read viewer

-   Header: path + line range.\
-   Syntax highlight.\
-   Secret scrubbing.

## 8) Edit preview (unified diff)

-   Unified diff format, collapsible hunks.\
-   Actions: apply, edit, skip, undo.\
-   Auto-commit option.

## 9) Git ops widget

-   Warn if no repo.\
-   Auto-commit on writes.\
-   Show branch & dirty state.

------------------------------------------------------------------------

# Codebase awareness

## 10) Code search results

-   Header: query & tool.\
-   Results: path:line:snippet.\
-   Cap & refine hint.

## 11) Repo map / symbol index

-   File list + top functions/classes.\
-   Token-budget aware.

------------------------------------------------------------------------

# Memory, sessions, checkpoints

## 12) Memory editor

-   Project/user/org memory chain.\
-   Capture via `#`.\
-   `/memory` command to edit.

## 13) Conversation/session manager

-   Save/resume with tags.\
-   Local checkpoint dir.

## 14) Project checkpoint/restore

-   Snapshot before file writes.\
-   `/restore` command.

------------------------------------------------------------------------

# Integrations & configuration

## 15) Tools/MCP inspector

-   List built-ins & MCP servers.\
-   Run dry-run test.\
-   Per-tool allow/deny rules.

## 16) Output-style / mode selector

-   `/output-style` command.\
-   Examples: default, explanatory, learning.

## 17) Status line

-   One-line context bar (model, dir, branch).\
-   Refresh ≤300 ms.\
-   Accept JSON input; ANSI colors.

------------------------------------------------------------------------

# Web & knowledge

## 18) Web search / fetch result card

-   Search: show top titles/URLs.\
-   Fetch: show summary + link.

------------------------------------------------------------------------

# Diagnostics

## 19) Error & retry panel

-   Icon + short cause.\
-   Expand for details.\
-   Actions: retry, logs, plan-only.

## 20) Verbose trace / logs viewer

-   Toggle with flag or command.\
-   Show tool choices, timings, tokens.\
-   Persist logs to file; tail option.

------------------------------------------------------------------------

# Minimal API objects (examples)

``` jsonc
ToolCall {
  id, kind, input, preview, needsApproval, approved, startedAt, endedAt, exitCode?, error?
}
Approval {
  scope, pattern
}
Diff {
  files: [{path, hunks:[{oldStart, oldLines, newStart, newLines, text}]}]
}
Checkpoint {
  id, type, createdAt, note
}
Memory {
  level, file, addedVia, updatedAt
}
SearchResult {
  query, results:[{path,line,col,snippet}]
}
StatusLineInput {
  session, model, cwd, projectDir, version
}
```


---

# ASCII Mockups (with common states)

> Conventions: `⏳` working, `✔` success, `✖` error, `…` truncated, dim text is low-priority, and `▌` indicates a cursor. ANSI colors omitted.

## 1) Prompt bar / input editor

```
[sonnet] ~/repo  (↑/↓ history, Ctrl+E open in $EDITOR)
> fix the failing tests in parser ▌

# Multi-line (expanded)
[sonnet] ~/repo  [multiline: Esc to send]
1  explain how to refactor the parser into
2  smaller combinators and add tests
3  ▌

# Error on send
[network error: retry with Enter, or :wq to save draft]
> fix the failing tests in parser ▌
```

## 2) Message stream (assistant output)

```
assistant  ⏳ generating… (Ctrl-C to stop)
Refactoring plan:
1. Extract tokenizer…
2. Build unit tests…
3. …

assistant  [stream ended, 3.2s]
[ /continue ]  [ /copy-last-code ]  [ /retry ]
```

## 3) Thinking/working indicator

```
⏳ thinking…  (elapsed 00:04, est. tokens 420)
⏳ running: npm test
✔ tests passed in 7.8s
✖ linter failed (exit 1) — see details below
```

## 4) Tool-call block (generic)

```
> Tool: file.search
  args: { "query": "parse\\(", "paths": ["src"] }
  … 14 matches

✔ completed in 220ms
```

```
> Tool: web.fetch
  args: { "url": "https://example.com/spec" }

✖ failed in 1.2s
stderr: TLS handshake timeout
[ retry ]  [ open logs ]
```

## 5) Permission prompt (approval gate)

```
Permission needed: Bash("npm run test")
scope?  [y]es  [n]o  [a]lways  [s]how
> s
command:
  (~/repo) npm run test --silent

> y
✔ allowed once (rule: session/once)
```

```
Permission needed: FileWrite(3 files)
mode is plan-only — press [b]ypass to apply, or [e]dit patch
```

## 6) Shell command transcript

```
(~/repo) > npm test
⏳ running…
 PASS  parser.spec.ts
 PASS  lexer.spec.ts
✔ exit 0 in 7.82s
```

```
(~/repo) > eslint .
src/ast.ts:14:3  error  no-unused-vars  "tmp"
✖ exit 1 in 1.21s
[ show 20 more lines ]  [ re-run ]
```

## 7) File read viewer

```
📄 src/parser.ts  [L120–L172]
120 function parseExpr(tokens: Token[]): Expr {
121   if (peek(tokens)?.type === "LPAREN") { … }
122   // …
172 }
[ prev page ]  [ next page ]  [ open full ]
```

## 8) Edit preview (unified diff)

```
Δ 3 files changed, +42/-10
--- a/src/parser.ts
+++ b/src/parser.ts
@@ -120,7 +120,10 @@
- function parseExpr(tokens: Token[]): Expr {
+ export function parseExpr(tokens: Token[]): Expr {
   // …
 }

--- a/src/tokenizer.ts
+++ b/src/tokenizer.ts
@@ -1,3 +1,12 @@
+export type Token = { kind: string; value: string }
…
[ a ]pply  [ e ]dit hunk  [ s ]kip file  [ u ]ndo last
```

```
✔ applied patch
git commit -m "Expose parseExpr; add Token type"
[ open commit ]  [ revert ]
```

## 9) Git ops widget

```
⚠ Not a git repository.
[ init git ]  [ proceed without VCS ]
```

```
On branch feat/parser
● modified: src/parser.ts
○ untracked: tests/parser.spec.ts
[ commit ]  [ stash ]  [ discard ]
```

## 10) Code search results

```
search: "visit\\(.*Identifier\\)"
Tool: ripgrep  — scope: src/
1  src/visitor.ts:34   visit(Identifier node) { … }
2  src/visitor.ts:88   visit(Identifier node) { … }  (duplicate)
3  src/ast.ts:12      export type Identifier = { … }

[ open 1 ] [ refine query ] [ limit 50 ]
```

## 11) Repo map / symbol index

```
Repository map (top symbols)
src/parser.ts        parseExpr(), parseTerm()
src/tokenizer.ts     tokenize(), readNumber(), readIdent()
src/visitor.ts       class Visitor { visitX()… }
tests/parser.spec.ts describe("parser", …)

[ send map to model ]  [ refresh ]
```

## 12) Memory editor

```
Project Memory (CLAUDE.md)
- Code style: prefer functional composition
- Testing: vitest; snapshot tests allowed
- Shell: use pnpm

[ edit ] [ add note via # ] [ disable for session ]
```

## 13) Conversation/session manager

```
/chat list
- main          (updated 10:42)
- refactor-plan (updated yesterday)
/chat save feat-lexer
✔ saved as "feat-lexer"
```

## 14) Project checkpoint/restore

```
Snapshot created: ws-2025-08-19T10:45:03Z (pre-write)
[ /restore ws-2025-08-19T10:45:03Z ]  [ list snapshots ]
```

```
/restore ws-2025-08-19T10:45:03Z
✔ workspace restored
```

## 15) Tools/MCP inspector

```
Tools
- Bash                ask
- FileRead            allow
- FileWrite           ask
- WebFetch            deny (policy)
- Grep                allow
- MCP: jira-prod      ask (server up)
- MCP: vector-search  allow

[ test tool ] [ schema ] [ set rule ]
```

## 16) Output-style / mode selector

```
/output-style
- Default
- Explanatory  ✔
- Learning (add in-code comments)
[ select Default ]
```

## 17) Status line

```
[sonnet] ~/repo  (feat/parser)  tokens:2.3k  cost:$0.004  ⏳ npm test
```

```
[sonnet] ~/repo  (main)  ✔ patch applied (+42/-10) in 1.8s
```

## 18) Web search / fetch result card

```
web.search "typescript parse combinators"
1. "Parsing with TS combinators" — blog.dev/…
2. "PEG in TypeScript" — example.org/…
3. "Practical parser patterns" — kb.site/…
[ open 2 ] [ refine ]

web.fetch https://example.org/peg-ts
title: "PEG in TypeScript"
summary: Introduces grammar rules…
[ open ] [ cite ]
```

## 19) Error & retry panel

```
✖ FileWrite failed
reason: permission denied (readonly fs)
actions: [ retry as sudo ] [ write to tmp ] [ open logs ]
```

## 20) Verbose trace / logs viewer

```
DEBUG plan:
- decide: run tests
- tool: Bash("npm test")
TIMES:
- think: 320ms
- tool: 7.8s
TOKENS: prompt 1.2k, completion 650
[ tail -f ~/.agent/logs/session.log ]
```
