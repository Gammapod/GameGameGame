# Engine-Editor Capabilities Manual

This is the central manual for GameGameGame engine/editor capability parity. It is intended for:

- engine/editor maintainers deciding how new Core features should be authored, validated, and exercised through editor tooling;
- content creators and content-editing agents that need to know what the editor can safely author today;
- agent API work, which should use the same canonical authoring model as the GUI and editor service.

Update this document whenever an engine capability is added, removed, renamed, promoted to editor support, intentionally kept engine-only, or moved into legacy compatibility.

## Evolution policy

The engine and editor should evolve together without forcing every new engine capability to become a polished GUI workflow immediately.

Use staged support:

1. **Engine/runtime support**: Core can execute or represent the capability.
2. **Descriptor/YAML support**: content can serialize the capability without hand-written runtime code.
3. **Validation support**: malformed content receives actionable diagnostics where possible.
4. **Editor service support**: tooling and future agent APIs can author the capability through typed operations.
5. **GUI support**: human-facing workflows can create/edit the capability.

New capabilities may pass through these stages over time. Prefer typed descriptors and canonical engine concepts over ad-hoc editor-only fields. Do not add editor-only concepts that Core cannot consume.

## Authoring support tiers

### Stable authoring support

Stable support is appropriate for GUI workflows and future agent API commands. These capabilities have canonical descriptors, validation, and editor-service support, and are intended for normal content authoring.

Current stable authoring areas:

- entity templates and presentations;
- inventory dimensions, weight, carrying capacity, and carried entity layout;
- action plans and action-plan steps;
- actor initial `Facing` through `actionStateDefaults.facing`;
- checks: `CanMove`, `BlockingEntity`, `CanPickup`;
- effects: `Wait`, `Move`, `Pickup`, `ReverseDirection`, `CallPlan`;
- movement effects: `Teleport`, `Drop` are functional and supported, but their GUI is intentionally advanced/generic rather than polished/specialized.

### Advanced but supported

Advanced support is usable and validated but may evolve as the engine grows. Content creators and agents may use these capabilities, but should expect workflow polish and command shapes to improve.

Current advanced support:

- `Teleport`, the general relocation/ur-primitive;
- `Drop`, constrained relocation from actor-carried inventory to peer/world destination;
- typed movement target/destination descriptors;
- descriptor/runtime turn flags retained below the GUI for future advanced authoring.

Guidance:

- Prefer constrained primitives (`Move`, `Pickup`, `Drop`) for ordinary content.
- Use `Teleport` for advanced relocation cases.
- Do not model common simple behavior as arbitrary teleport if a constrained primitive expresses it more clearly.

### Legacy compatibility support

Legacy support is retained for old content/runtime compatibility. It should load, display, and execute where applicable, but should not be used by new canonical editor or agent API authoring.

Current legacy support:

- string-keyed `ActionPlanContext` variables;
- legacy descriptor fields: `directionVariable`, `targetVariable`, `variableName`;
- `defaultPlanVariables`;
- `SetVariable`;
- configurable turn flags on legacy `SetVariable` and descriptor-level `ReverseDirection`.

Guidance:

- Do not expose arbitrary variable-name authoring in new GUI or agent API workflows.
- Do not rush to remove Core/runtime compatibility until there is a migration plan and confidence that content no longer depends on it.
- Canonical authoring should use engine-defined slots and typed descriptors instead.

## Capability support statuses

| Status | Meaning |
|---|---|
| Yes | The layer supports the capability directly. |
| Partial | The layer supports part of the capability, or supports it through a transitional/advanced path. |
| Legacy | Supported for old content/runtime compatibility, not intended for new canonical authoring. |
| Planned | The capability is intentionally planned but not currently exposed in that layer. |
| Intentional non-parity | The capability exists somewhere but is intentionally not exposed in another layer. |
| No | The layer does not currently support the capability. |

## Current editor capability summary

The editor can currently:

- create/open/save/reload content documents;
- edit entity templates, presentations, inventory dimensions, weights, capacities, and carried entities;
- assign/clear default action plans;
- edit actor initial `Facing`;
- create/edit/delete/reorder action plans and steps;
- author `CanMove`, `BlockingEntity`, and `CanPickup` checks;
- author `Wait`, `Move`, `Pickup`, `ReverseDirection`, `CallPlan`, `Teleport`, and `Drop` effects;
- edit pickup inventory coordinates and call-plan references;
- edit movement target/destination fields for `Teleport` and `Drop`;
- validate content and surface diagnostics for missing references, missing canonical slots, malformed movement descriptors, inventory layout issues, and legacy/arbitrary variable fields;
- author content through the first in-process `AgentContentEditorApi` facade over editor/content services;
- load/display legacy variable-based content and legacy `SetVariable` effects without exposing them for new canonical GUI authoring.

The editor intentionally does not currently:

- author arbitrary action-plan variables through GUI;
- author `SetVariable` through GUI;
- author `directionVariable`, `targetVariable`, or `variableName` fields through GUI;
- author initial `Target` actor state through GUI;
- expose `CanDrop`, which is deferred until a concrete branching use case appears;
- provide an external agent transport/protocol layer yet.

## Action-plan checks

| Check | Tier | Engine | Descriptor | YAML | Validation | Editor service | GUI | Notes |
|---|---|---:|---:|---:|---:|---:|---:|---|
| `CanMove` | Stable | Yes | Yes, canonical | Yes | Yes, reads `Facing` | Yes | Yes | Uses canonical `Facing`; no editor-authored variable name required. |
| `BlockingEntity` | Stable | Yes | Yes, canonical | Yes | Yes, reads `Facing`, writes `Target` | Yes | Yes | Produces canonical `Target` for later target-based primitives. |
| `CanPickup` | Stable | Yes | Yes, canonical target + literal coord | Yes | Yes, reads `Target` | Yes | Yes | Uses canonical `Target`; editor authors inventory coordinate only. |
| `CanDrop` | Planned | No | No | No | No | No | No | Deferred. Add only if action plans need explicit branching before `Drop`. |

## Action-plan effects

| Effect | Tier | Engine | Descriptor | YAML | Validation | Editor service | GUI | Notes |
|---|---|---:|---:|---:|---:|---:|---:|---|
| `Wait` | Stable | Yes | Yes | Yes | Yes | Yes | Yes | Consumes a turn through `WaitAction`. |
| `Move` | Stable | Yes | Yes, canonical | Yes | Yes, reads `Facing` | Yes | Yes | Constrained movement primitive: self to adjacent peer/world location derived from canonical `Facing`. Uses relocation internally. |
| `Pickup` | Stable | Yes | Yes, canonical target + literal coord | Yes | Yes, reads `Target` | Yes | Yes | Constrained movement primitive: canonical `Target` to authored carried inventory coordinate. Keeps pickup-specific validation/capacity rules. |
| `ReverseDirection` | Stable | Yes | Yes, canonical | Yes | Yes, reads/writes `Facing` | Yes | Yes | GUI authors fixed default turn behavior. Descriptor/runtime retain advanced turn flags. |
| `CallPlan` | Stable | Yes | Yes | Yes | Yes, includes called-plan slot requirements | Yes | Yes | GUI can select called plan. |
| `Teleport` | Advanced | Yes | Yes | Yes | Yes | Yes | Yes | General relocation primitive for arbitrary entity/destination movement. GUI exposes generic movement fields. |
| `Drop` | Advanced | Yes | Yes | Yes | Yes | Yes | Yes | Constrained movement primitive: carried entity to peer/world destination. `CanDrop` intentionally deferred. |
| `SetVariable` | Legacy | Legacy | Legacy | Legacy load | Canonical authoring flags arbitrary variable fields | Legacy/display-only | Display-only | Runtime/deserialization compatibility remains, but canonical workflows should not create it. |

## Actor action-state defaults

| State | Tier | Engine context | Content model | YAML | Validation | Editor service | GUI | Notes |
|---|---|---:|---:|---:|---:|---:|---:|---|
| Initial `Facing` | Stable | Yes | Yes | Yes | Yes | Yes | Yes | Canonical YAML is `actionStateDefaults.facing`. |
| Initial `Target` | Planned | Yes | Yes | Yes | Yes | Partial | Planned | Supported by model/context. Expose through Actor State when a concrete content use case appears. |

## Movement primitive model

Movement-like gameplay operations are treated as one movement primitive family.

Accepted direction:

- `Teleport` is the general engine relocation primitive: move any target entity to any valid destination.
- `Move`, `Pickup`, and `Drop` are constrained movement primitives built on shared relocation semantics.
- The editor exposes both friendly constrained primitives and advanced generic `Teleport`.
- The editor should not collapse everything into arbitrary teleport; constrained primitives remain easier for content creators and agents.

Current movement descriptor concepts:

- movement targets: `Self`, `CanonicalTarget`, explicit `Entity`, `CarriedInventoryCoord`;
- movement destinations: explicit `PlaneCoord`, `InventorySlot`, `AdjacentToSelf`, `AdjacentToEntity`, `AdjacentToCanonicalTarget`.

Policy decisions:

- `Teleport` is advanced and arbitrary; it does not enforce `Pickup` carrying-capacity rules.
- `Pickup` remains the constrained/capacity-aware way to move peer/world entities into actor inventory.
- `Drop` validates that the target is carried by the actor and that the destination is on the actor plane.
- `CanDrop` is intentionally deferred until a concrete action-plan branching use case appears.

## Turn behavior policy

GUI authoring uses fixed default turn behavior per primitive. Descriptor/runtime support for configurable turn flags remains where it already exists so advanced authoring can be reintroduced deliberately later.

Current policy:

- `ReverseDirection` GUI authoring uses fixed defaults.
- Legacy `SetVariable` may still carry turn flags in old content.
- Do not expose turn flags in the first agent/editor API unless a concrete advanced use case requires them.

## Agent API readiness

The movement primitive parity baseline is sufficient for the first in-process agent API facade.

Agent API currently has an in-process `AgentContentEditorApi` facade in the Editor project. It wraps editor/content services for document/session snapshots, validation, entity template updates, actor initial facing, action plans/steps, canonical checks, and canonical/advanced supported effects. It rejects legacy `SetVariable` effect authoring.

Agent API should continue to:

- wrap `ContentEditorService`, not edit YAML/DTOs directly;
- expose stable and advanced supported capabilities through typed commands;
- avoid all legacy variable authoring;
- return structured results and validation diagnostics;
- reuse movement target/destination descriptors for `Teleport` and `Drop`;
- keep initial `Target`, `CanDrop`, and advanced turn-flag authoring deferred until concrete use cases appear.

See `Agent-Editor-API-Plan.md` for the implementation plan and next transport/protocol considerations.
