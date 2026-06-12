# Engine-Editor Capabilities Manual

This document is the central manual for engine capabilities that content and editor tooling must support. It should be updated first when a capability is added, removed, renamed, or intentionally kept engine-only.

Dependent guides, catalogs, schemas, editor workflows, validation rules, and future agent APIs should be checked against this document.

## Capability support levels

| Status | Meaning |
|---|---|
| Yes | The layer supports the capability directly. |
| Partial | The layer supports part of the capability or supports it through a transitional/legacy path. |
| Legacy | Supported for old content/runtime compatibility, but not intended for new canonical authoring. |
| Intentional non-parity | The capability exists somewhere but is intentionally not exposed in another layer. |
| No | The layer does not currently support the capability. |

## Action-plan checks

| Check | Engine | Descriptor | YAML | Validation | Editor service | GUI | Notes |
|---|---:|---:|---:|---:|---:|---:|---|
| `CanMove` | Yes | Yes, canonical | Yes | Yes, reads `Facing` | Yes | Yes | Uses canonical `Facing`; no editor-authored variable name required. |
| `BlockingEntity` | Yes | Yes, canonical | Yes | Yes, reads `Facing`, writes `Target` | Yes | Yes | Produces canonical `Target` for later target-based primitives. |
| `CanPickup` | Yes | Yes, canonical target + literal coord | Yes | Yes, reads `Target` | Yes | Yes | Uses canonical `Target`; editor authors inventory coordinate only. |

## Action-plan effects

| Effect | Engine | Descriptor | YAML | Validation | Editor service | GUI | Notes |
|---|---:|---:|---:|---:|---:|---:|---|
| `Teleport` | Planned | Planned | Planned | Planned | Planned | Planned | Engine-level ur-primitive for moving any entity to any valid destination. Requires typed location model first. |
| `Move` | Yes | Yes, canonical | Yes | Yes, reads `Facing` | Yes | Yes | Movement primitive: constrained teleport of self to adjacent peer/world location derived from canonical `Facing`. |
| `Pickup` | Yes | Yes, canonical target + literal coord | Yes | Yes, reads `Target` | Yes | Yes | Movement primitive: constrained teleport of canonical `Target` into authored carried inventory coordinate. |
| `Drop` | No | No | No | No | No | No | Planned movement primitive: constrained teleport of carried entity to peer/world destination after typed location model is defined. |
| `ReverseDirection` | Yes | Yes, canonical | Yes | Yes, reads/writes `Facing` | Yes | Yes | GUI should author fixed default turn behavior. Descriptor/runtime can retain turn flags for advanced support later. |
| `Wait` | Yes | Yes | Yes | Yes | Yes | Yes | Consumes a turn through `WaitAction`. |
| `CallPlan` | Yes | Yes | Yes | Yes, includes called-plan slot requirements | Partial | Yes | GUI can select called plan; service has basic default effect creation but still needs field-level plan-reference editing. |
| `SetVariable` | Legacy | Legacy | Legacy load | Canonical authoring flags arbitrary variable fields | Legacy | Display-only | Runtime/deserialization compatibility remains, but canonical editor workflows should not create it. |

## Actor action-state defaults

| State | Engine context | Content model | YAML | Validation | Editor service | GUI | Notes |
|---|---:|---:|---:|---:|---:|---:|---|
| Initial `Facing` | Yes | Yes | Yes | Yes | Yes | Yes | Canonical YAML is `actionStateDefaults.facing`. |
| Initial `Target` | Yes | Yes | Yes | Yes | Partial | Planned | Supported by model/context. Planned for future Actor State authoring when a concrete content use case requires an initial target. |

## Movement primitive direction

Movement-like gameplay operations should be considered part of one movement-primitive family rather than unrelated special cases.

Accepted architecture direction:

- Treat a general `Teleport`/relocation capability as the engine-level “ur-primitive”: move an entity from a source location to a destination location.
- Model `Move`, `Pickup`, and `Drop` as constrained/specialized movement primitives built on the same underlying relocation semantics where practical.
- Expose both the general movement primitive and constrained friendly primitives in the editor:
  - `Move`: actor/world position + canonical `Facing` destination.
  - `Pickup`: target entity/world location to actor inventory coordinate.
  - `Drop`: carried entity/inventory location to peer/world destination.
  - `Teleport`: arbitrary entity and arbitrary destination for advanced content.
- Do not collapse the editor to only arbitrary `Teleport` until constrained primitive authoring and validation can still be expressed clearly. Limited primitives are easier for content authors and future agents, while `Teleport` provides the generalized engine substrate.

Before implementing `Drop` or arbitrary `Teleport`, define a typed location model that can represent at least world coordinates and inventory/carried-entity coordinates without falling back to arbitrary variable names.

## Intentional or unresolved parity decisions

### Movement primitive implementation

Core has direct `DropAction`, but action plans currently do not expose `Teleport`, `CanDrop`, or `Drop` primitives. `Move` and `Pickup` exist today and should be migrated toward the accepted movement primitive model without regressing current behavior.

Required before considering movement action-plan parity complete for expanded content:

- Add a typed location/relocation model.
- Add catalogued `Teleport` and `Drop` movement primitives.
- Rebase or align `Move` and `Pickup` with the shared relocation model.
- Avoid adding one-off `Drop` action-plan behavior before the typed location/relocation model is planned.

### Turn behavior fields

Some primitive effects have configurable turn behavior while others have fixed behavior. Current known configurable behavior:

- `ReverseDirection`: `ConsumesTurn`, `ContinuePlan`
- legacy `SetVariable`: `ConsumesTurn`, `ContinuePlan`

Policy:

- Default GUI authoring should use fixed turn behavior per primitive.
- Keep descriptor/runtime support where it already exists so advanced authoring can be reintroduced deliberately later.
- Do not expose turn flags in the first agent/editor API unless a concrete advanced use case requires them.

### Legacy variable authoring

Legacy arbitrary variable support remains for compatibility:

- string-keyed `ActionPlanContext` variables
- legacy descriptor fields such as `directionVariable`, `targetVariable`, and `variableName`
- legacy `defaultPlanVariables`
- legacy `SetVariable`

Canonical authoring should use:

- engine-defined slots, currently `Facing` and `Target`
- `actionStateDefaults` for persistent initial state
- primitive catalog slot read/write metadata

## Remaining priorities before agent API

Completed cleanup:

- GUI/view-model hidden legacy variable input plumbing has been removed.
- GUI authoring uses fixed default turn behavior for `ReverseDirection`; descriptor/runtime advanced fields remain for compatibility/future advanced support.
- `SetVariable` is legacy display/load/runtime compatible only and is not authored from canonical editor workflows.

Next priorities:

1. Add field-level editor-service operations for remaining authored literal fields:
   - pickup inventory coordinate
   - called plan reference
2. Plan typed movement/relocation locations before adding `Drop`/`Teleport` action-plan parity.
