# CSS & Style System (Subset)

## Scope
Selectors (type/class/ID/descendant/child), `:hover/:focus/:active/:disabled`, variables, media queries (`(min-width)`, `(terminal)`, `(prefers-reduced-motion)`), transitions, keyframes.

## Properties (MVP)
Layout (`display:flex`, `flex-*`, `gap`), Box (`padding/margin/border/radius`), Color/Text (`color/bg/font-weight/style/decoration`), Sizing (`width/height/min/max`), Overflow (`overflow`, `text-overflow`), Effects (TTY downgraded): `box-shadow`, `opacity`.

## Implementation Plan
1. **Parser**
   - Small CSS subset; store rule AST with specificity.
2. **Matcher**
   - Precompute selector functions; attach to nodes; cache results.
3. **Cascade**
   - Specificity + order; compute vars; resolve to normalized `Style`.
4. **Media Queries**
   - Recompute on viewport/capability change; invalidate affected nodes.
5. **Transitions/Keyframes**
   - Integrate with animation timeline; numeric/color/interpolable properties.

## Test Plan
- Unit: specificity conflicts, inheritance, var fallback.
- Snapshot: computed styles for fixtures under different media.
- Perf: rule matching counts per frame.

## Exit Criteria
- Style compute ≤1ms for 1k nodes; deterministic across backends.
