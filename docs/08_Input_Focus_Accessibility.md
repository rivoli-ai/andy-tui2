# Input, Focus & Accessibility

## Purpose
Unified input events, focus management, keyboard navigation; basic accessibility.

## Interfaces
- Events: `Key{code, mods}`, `Mouse{type, x,y, button, mods}`, `Paste`, `Resize`, `IME`.
- `focusManager`: roving focus; tab order; `focusNext/Prev`, `setFocus(nodeId)`.

## Implementation Plan
1. **Decoders**
   - TTY: CSI/SS3, 1006 mouse, bracketed paste, Kitty keyboard (opt).
   - Web/Native: map platform events to unified events.
2. **Focus Model**
   - Single owner; aria-like roles; default tab order; focus ring.
3. **A11y**
   - TTY: status line updates; high-contrast themes.
   - Web/DOM renderer: roles, labels from view props.

## Test Plan
- Golden bytes → events; modifiers; partial sequences.
- Focus traversal across complex trees.
- IME composition text flow.

## Exit Criteria
- Reliable decoding; predictable focus; keyboard-only operation.
