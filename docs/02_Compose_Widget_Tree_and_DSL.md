# Compose & Widget Tree (SwiftUI-like DSL)

## Purpose
Declarative views with modifiers, templates, and identity keys; produces a virtual tree for reconciliation.

## Interfaces
- `protocol View { body: View }`
- `@State`, `@Binding`, `@ObservedObject`, `@Environment`
- Containers: `VStack`, `HStack`, `Grid`, `List`, `ForEach(data, id:)`
- Modifiers: `.padding() .border() .background() .class() .id()`

## Implementation Plan
1. **VNode Model**
   - `{type, key, props, children, stateRef}`; stable keys for ForEach.
2. **Reconciler**
   - Old vs new keyed diff; preserves state; marks dirty on prop/style/state changes.
3. **Environment**
   - Inherited map; includes theme, viewport, capabilities.
4. **Templates**
   - DataTemplate for `List`/`Grid` rows; lazy creation (virtualized).
5. **Error Boundaries**
   - Contain faults to a subtree; keep terminal clean.

## Test Plan
- Unit: keyed moves vs inserts, environment propagation, modifier merge.
- Property: randomized tree updates — immutability, no duplicate IDs.
- Snapshot: VNode trees for canonical views.

## Exit Criteria
- Reconciliation stable across reorder/move; precise dirty marking.
