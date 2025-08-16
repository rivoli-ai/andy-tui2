# Text Engine (Unicode Correctness)

## Purpose
Correct grapheme handling, widths, wrapping, truncation; caches for speed.

## Interfaces
- `shape(text, style, width) -> [runs]`
- `wrap(text, width) -> lines[]`
- `measure(text) -> {cells}`

## Implementation Plan
1. **Graphemes**
   - UAX#29 segmentation; ZWJ/emoji sequences as single graphemes.
2. **Width**
   - East Asian width; width=2 phantom guard; combining marks merged.
3. **Wrapping**
   - Word/character fallback; ellipsis handling.
4. **Caches**
   - Keyed by `(font, size, width, sliceHash, styleHash)`.
5. **BiDi (optional)**
   - Basic paragraph level reorder; skip for TTY initially.

## Test Plan
- Fixtures: emoji families, flags, combining accents, CJK wide, long words.
- Properties: sum(cell widths) equals measured line width.
- Regression: no split grapheme on wrap/resizes.

## Exit Criteria
- Zero known grapheme width errors on fixture pack; wrap speed O(added length).
