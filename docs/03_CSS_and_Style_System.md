# CSS & Style System (Subset)

## Scope
Selectors (type/class/ID; minimal compound without descendant/child in parser for now), `:hover/:focus/:active/:disabled`, variables, media queries (`(min-width)`, `(max-width)`, `(terminal)`, `(prefers-reduced-motion)`), transitions, keyframes.

## Properties (MVP)
Layout (`display:flex`, `flex-*`, `gap`), Box (`padding/margin/border/radius`), Color/Text (`color/bg/font-weight/style/decoration`), Sizing (`width/height/min/max`), Overflow (`overflow`, `text-overflow`), Effects (TTY downgraded): `box-shadow`, `opacity`.

## Implementation Plan
1. **Parser**
   - Small CSS subset implemented (type/.class/#id/:pseudo simple sequences; simple `@media(...)` gating); store rules with specificity.
2. **Matcher**
   - Precompute selector functions; attach to nodes; cache results.
3. **Cascade**
   - Specificity + order; compute vars; resolve to normalized `Style`.
4. **Media Queries**
   - Recompute on viewport/capability change; invalidate affected nodes.
5. **Transitions/Keyframes**
   - Integrate with animation timeline; numeric/color/interpolable properties.

## Browser Parity Goals

- Match browser cascade and specificity for the supported subset.
- Match flex layout behavior for common patterns within ±1px (rounding tolerance) and identical wrapping decisions.
- Match computed values for properties in the registry below for curated fixtures.
- Resolve variables and inheritance per spec (specified → computed → used → actual values).

## Cascade & Specificity

- Origins and importance: user-agent < author < author `!important` (no user styles in MVP). Inline styles treated as author with highest order.
- Specificity tuple: `(a, b, c)` where `a` = ID selectors, `b` = class/attr/pseudo-class, `c` = type/pseudo-element. Higher tuple wins; ties broken by source order.
- Shorthands expand to longhands before cascade. Conflicts resolved per specificity and order.

## Variables and Inheritance

- CSS custom properties: `--name: value;` and usage `var(--name, fallback)`.
- Resolution occurs during computed-value time; cycles invalidate the property and use fallback/initial.
- Keywords: `initial`, `inherit`, `unset`, `revert` respected for supported properties.
- Inheritance table maintained per-property; e.g., `color` inherits, `margin` does not.

## Value Processing Pipeline

- Specified → Computed → Used → Actual
  - Percentages resolved at computed or used stage depending on property (e.g., `width: 50%` resolves during layout when container size known).
  - `auto` values defer to layout engine to determine used values.
  - Color normalization to RGBA; lengths normalized to device-independent units (px for TUI grid cells).

## Units and Value Types (MVP)

- Length: `px`, `%` (relative to containing block for sizing, to gap for certain alignments). `em/rem` deferred.
- Number: unitless for flex-groups (grow/shrink).
- Color: hex, rgb(a), named limited set.
- Time: `ms` for transitions/animations. Easing: predefined keywords.

## Property Registry (MVP)

- Layout container:
  - `display`: `none | flex` (default: `flex` for container widgets, `none` otherwise)
  - `flex-direction`: `row | column` (initial: `row`, inherited: no)
  - `flex-wrap`: `nowrap | wrap` (initial: `nowrap`, inherited: no)
  - `justify-content`: `flex-start | center | flex-end | space-between | space-around | space-evenly` (initial: `flex-start`, inherited: no)
  - `align-items`: `stretch | flex-start | center | flex-end | baseline` (initial: `stretch`, inherited: no)
  - `align-content`: `stretch | flex-start | center | flex-end | space-between | space-around | space-evenly` (initial: `stretch`, inherited: no)
  - `gap`, `row-gap`, `column-gap`: `<length>` (initial: `0px`, inherited: no)

- Flex item:
  - `order`: `<integer>` (initial: `0`, inherited: no)
  - `flex-grow`: `<number>` (initial: `0`, inherited: no)
  - `flex-shrink`: `<number>` (initial: `1`, inherited: no)
  - `flex-basis`: `auto | <length> | % | content` (initial: `auto`, inherited: no)
  - `align-self`: same keywords as `align-items` + `auto` (initial: `auto`, inherited: no)

- Box model:
  - `margin`, `padding`: shorthands to edges; longhands `-top/-right/-bottom/-left`
  - `border-width`, `border-style`, `border-color`; `border-radius`
  - Sizing: `width`, `height`, `min-width`, `min-height`, `max-width`, `max-height`
  - `box-sizing`: `content-box | border-box` (initial: `content-box`)

- Text/Color:
  - `color` (inherits)
  - `background-color`
  - `font-weight`, `font-style`, `text-decoration` (subset for TTY)

- Overflow:
  - `overflow`: `visible | hidden | scroll` (subset; maps to clipping/scroll containers)
  - `text-overflow`: `clip | ellipsis` (TTY ellipse as `…`)

## Selector Matching

- Supported selectors: type, `.class`, `#id`, pseudo-classes `:hover/:focus/:active/:disabled` (mapped to state machine). Parser supports simple sequences (e.g., `button:hover`, `.btn.primary`). Descendant/child and attribute selectors deferred.
- Precompile to matcher functions with specificity precomputed; evaluate top-down; cache per node with invalidation tokens.

## Media Queries

- `(min-width)`, `(max-width)` based on columns; `(terminal)` feature flag; `(prefers-reduced-motion)` from environment.
- Rules can be gated at compute time; full invalidation model to be added.

## Transitions & Keyframes (Interop)

- Interpolable properties: lengths, colors, numbers. Keyframes schedule values against global animation timeline (see `Animations` doc).
- Transition composition: last-writer wins per property; discrete properties snap at 50% unless special-cased.

## Interop with Layout Engine

- Computed `display:flex` designates flex container; flex item properties forwarded to layout node.
- Map `gap` to row/column spacing in flex algorithm.
- Respect `order` for item ordering prior to line generation.
- `min/max` constraints, `flex-basis` resolution, and `auto` sizing delegated to measure/arrange passes per spec.
- `overflow` maps to scrollable container and clipping in placement.

## Test Plan
- Unit: specificity conflicts, inheritance, var fallback, shorthand expansion, `order` sorting.
- Snapshot: computed styles and used values for fixtures under different media.
- Browser parity: reproduce a curated subset of WPT flexbox and cascade tests as C# fixtures; cross-check expected boxes against Chromium headless measurements (Playwright harness) with ±1px tolerance.
- Property-based: generate random rule sets and trees; assert invariants (no negative sizes, consistent cascade ordering).
- Perf: rule matching and style compute ≤ 1ms for 1k nodes (cold/warm).

## Exit Criteria
- Style compute ≤1ms for 1k nodes; deterministic across backends.
- ≥95% pass on curated browser-parity suite; 100% on internal invariants.

## API Sketch

```csharp
public readonly record struct Specificity(int A, int B, int C) : IComparable<Specificity>;

public abstract record Selector(Specificity Specificity)
{
    public abstract bool Matches(Node node);
}

public sealed record Rule(Selector Selector, IReadOnlyDictionary<string, CssValue> Declarations, int SourceOrder);

public sealed class Stylesheet
{
    public IReadOnlyList<Rule> Rules { get; }
}

public sealed class StyleResolver
{
    public ResolvedStyle Compute(Node node, IEnumerable<Stylesheet> stylesheets, EnvironmentContext env);
}

public readonly record struct ResolvedStyle(
    Display Display,
    FlexDirection FlexDirection,
    FlexWrap FlexWrap,
    JustifyContent JustifyContent,
    AlignItems AlignItems,
    AlignContent AlignContent,
    int Order,
    double FlexGrow,
    double FlexShrink,
    LengthOrAuto FlexBasis,
    LengthOrAuto Width,
    LengthOrAuto Height,
    Thickness Padding,
    Thickness Margin,
    Length RowGap,
    Length ColumnGap,
    Overflow Overflow,
    RgbaColor Color,
    RgbaColor BackgroundColor
);
```

## Salvage Mapping (from v1)

- Reuse selector/tokenizer ideas if present; otherwise adopt well-known small CSS parsers for .NET as references (no runtime dependency).
- Adapt any existing style-to-layout integration tests; port expected results to new flex engine invariants.

## Next Steps

- Variables/inheritance: implement `var()` resolution, fallback, and inheritance table; add tests (including cycles).
- Pseudo-classes: wire view state to `:hover/:focus/:active/:disabled` and test matching.
- Value parsing: accept string inputs for enums/colors/lengths; expand unit coverage beyond px.
- Shorthands: add `margin`, `border-*`, `gap` expansion with precedence tests.
- Media queries: environment change invalidation tests and perf assertions.

## Status (2025-08-16)

- Cascade & specificity: implemented for supported selectors; source-order tiebreaks covered by tests.
- Custom properties: `var()` with fallback implemented; cycle prevention in resolver; used by typed getters.
- Selectors: type, id, class, pseudo-classes implemented; matching tests added (case sensitivity where applicable).
- Shorthands precedence: longhand vs shorthand resolution implemented for padding/margin; tests added.
- Property registry alignment: `align-content` now includes `space-evenly` to match current implementation.
- Parsing: minimal parser implemented; tests added for parser, pseudo-classes, and media gating.
- Value parsing: basic colors (hex and common names) supported; percentages still pending.
- Inheritance: color inherits from parent; broader table pending.
