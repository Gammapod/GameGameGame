# Engine-Editor Capabilities Manual

This is the central manual for GameGameGame engine/editor capability parity. It is intended for:

- engine/editor maintainers deciding how new Core features should be authored, validated, and exercised through editor tooling;
- content creators and content-editing agents that need to know what the editor can safely author today;
- agent API work, which should use the same canonical authoring model as the editor service and future frontend/editor surfaces.

Update this document whenever an engine capability is added, removed, renamed, promoted to editor support, intentionally kept engine-only, or moved into legacy compatibility.

## Evolution policy

The engine and editor service / agent API should evolve together without forcing every new engine capability to become a current Avalonia GUI workflow. The current Avalonia GUI is maintenance-mode/legacy-priority; future human-facing editor investment is expected to move toward an integrated game/editor frontend.

Use staged support:

1. **Engine/runtime support**: Core can execute or represent the capability.
2. **Descriptor/YAML support**: content can serialize the capability without hand-written runtime code.
3. **Validation support**: malformed content receives actionable diagnostics where possible.
4. **Editor service support**: tooling and future agent APIs can author the capability through typed operations.
5. **Agent/headless API support**: agents, tests, scripts, and future frontends can author or inspect the capability through structured operations.
6. **Frontend/editor UI support**: a human-facing surface can create/edit the capability. The current Avalonia GUI may provide legacy/maintenance support for existing workflows, but new capabilities do not require Avalonia GUI parity unless explicitly selected.

New capabilities may pass through these stages over time. Prefer typed descriptors and canonical engine concepts over ad-hoc editor-only fields. Do not add editor-only concepts that Core cannot consume.

## Authoring support tiers

### Stable authoring support

Stable support is appropriate for editor-service workflows, agent/headless API commands, tests, and future frontend/editor surfaces. These capabilities have canonical descriptors, validation, and editor-service support, and are intended for normal content authoring. Current Avalonia GUI support may exist for some stable capabilities, but it is no longer a required parity target for new work.

Current stable authoring areas:

- entity templates and presentations;
- inventory dimensions, weight, carrying capacity, and carried entity layout;
- legacy low-level action plans and action-plan steps remain loadable and editable as compatibility when an existing legacy plan is selected, but are hidden from current Avalonia GUI authoring paths where canonical ordered behavior-chain authoring is available;
- actor initial `Facing` through `actionStateDefaults.facing`;
- checks: `CanMove`, `BlockingEntity`, `CanPickup`;
- effects: `Wait`, `Move`, `Pickup`, `ReverseDirection`, `CallPlan`;
- movement effects: `Teleport`, `Drop` are functional and supported, but their GUI is intentionally advanced/generic rather than polished/specialized.
- transitional primitive-backed `MoveFacing` and `PickupTarget` action-plan descriptors with explicit fallback references are supported through Core/content validation, editor services, and the agent API; GUI polish remains generic/minimal, and these linked descriptors are not the long-term canonical editor-facing model.
- canonical ordered behavior-chain descriptors with `MoveFacing`, `PickupTarget`, `DropFacing`, `PushFacing`, `DestroyTarget`, and `CreateFacing` Action Steps have Core runtime, Action Step catalog metadata, descriptor/YAML, hardened validation/default handling, editor service, agent API, and GUI support that makes canonical chains visually primary over legacy low-level authoring.
- compact canonical behavior-chain trace formatting is available in Core for tests, debugging, and future editor/agent diagnostics.
- canonical action-plan preview is available through editor service and agent API commands, summarizing plan shape, ordered Action Steps, state hints/defaults, validation diagnostics, guidance, and YAML preview text.

### Advanced but supported

Advanced support is usable and validated but may evolve as the engine grows. Content creators and agents may use these capabilities, but should expect workflow polish and command shapes to improve.

Current advanced support:

- low-level action-plan step/check/effect authoring only as compatibility for selected existing legacy plans while canonical ordered behavior-chain action plans are being implemented;
- primitive-backed linked action plans while canonical ordered behavior chains are being implemented;
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

## Current Avalonia GUI status

The Avalonia desktop editor is legacy-priority / maintenance-mode. It remains useful for existing browsing and editing workflows, and the capability tables below still record what it can currently display or author. However:

- new engine/editor capabilities should prioritize Core runtime, descriptor/YAML support, validation, editor service operations, agent/headless API support, tests, and documentation;
- Avalonia GUI support is optional unless explicitly selected in the active plan;
- do not add or refactor Avalonia GUI workflows solely to preserve broad engine/editor parity;
- future human-facing editor work is expected to target an integrated game/editor frontend rather than treating the current Avalonia UI as the long-term editor surface.

## Current editor capability summary

The editor can currently:

- create/open/save/reload content documents;
- edit entity templates, presentations, inventory dimensions, weights, capacities, and carried entities;
- assign/clear default action plans;
- create new GUI action plans as empty/passive plans so authors can add canonical behavior-chain Action Steps without first creating legacy low-level steps;
- edit actor initial `Facing`;
- create/edit/delete/reorder action plans and steps;
- author `CanMove`, `BlockingEntity`, and `CanPickup` checks;
- author `Wait`, `Move`, `Pickup`, `ReverseDirection`, `CallPlan`, `Teleport`, and `Drop` effects;
- edit pickup inventory coordinates and call-plan references;
- edit movement target/destination fields for `Teleport` and `Drop`;
- validate content and surface diagnostics for missing references, missing canonical slots, malformed movement descriptors, inventory layout issues, and legacy/arbitrary variable fields;
- load and validate canonical ordered behavior-chain descriptors for `MoveFacing`, `PickupTarget`, `DropFacing`, `PushFacing`, `DestroyTarget`, and `CreateFacing` using Action Step catalog metadata;
- author content through the first in-process `AgentContentEditorApi` facade over editor/content services;
- create transitional primitive-backed `MoveFacing` action-plan descriptors with optional fallback references through editor services and the agent API;
- create a transitional `MoveFacing -> PickupTarget` linked fallback chain through editor services and the agent API without low-level check/effect authoring;
- author canonical ordered behavior chains through editor services and the agent API without low-level check/effect authoring or linked fallback plan descriptors, including a convenience helper for the common `MoveFacing -> PickupTarget` chain;
- preview action plans through editor service and agent API commands before save/manual YAML inspection, including canonical plan shape, Action Step metadata, state hints such as `Facing=West` and `Target=Self`, validation diagnostics, and YAML preview text;
- view and edit canonical ordered behavior chains through the GUI Action Plans tab, including add/remove/reorder for catalog-backed Action Steps, plan-shape guidance, canonical-chain summaries, and default-state hints;
- load/display legacy variable-based content and legacy `SetVariable` effects without exposing them for new canonical GUI authoring;
- hide the legacy low-level steps/checks/effects GUI section unless the selected plan is already a legacy low-level plan.

The editor intentionally does not currently:

- author arbitrary action-plan variables through GUI;
- author `SetVariable` through GUI;
- author `directionVariable`, `targetVariable`, or `variableName` fields through GUI;
- provide polished/specialized behavior-template workflows beyond the Action Plans tab controls;
- author initial `Target` actor state through GUI;
- expose `CanDrop`, which is deferred until a concrete branching use case appears;
- provide an external agent transport/protocol layer yet.

## Canonical behavior-chain action plans

Canonical action-plan authoring is being remodeled around ordered behavior chains. The completed first slice is archived at `docs/Archived/Behavior-Model-Consolidation-First-Slice.md`; the completed follow-up behavior-system sprint is archived at `docs/Archived/Behavior-System-Next-Steps.md`. The earlier behavior-primitive linked-plan foundation is archived/superseded by this direction; it remains supported as transitional compatibility/prototype work.

Current vocabulary/model assumptions:

- **Action Plan**: the behavior definition assigned to an entity as its default behavior or invoked by another supported mechanism.
- **Canonical behavior chain**: the preferred new authoring shape for normal behavior. It is an ordered list of engine-defined Action Steps on one Action Plan.
- **Action Step**: one engine-defined behavior attempt inside a canonical behavior chain, such as `MoveFacing`, `PickupTarget`, `DropFacing`, `PushFacing`, `DestroyTarget`, or `CreateFacing`.
- **Fallback / fallthrough**: in canonical behavior chains, fallback means continuing to the next ordered Action Step in the same Action Plan when the current step fails or cannot act. It does not mean creating linked fallback plans for new normal authoring.
- **Primitive-backed linked plans**: transitional compatibility/prototype descriptors. They may remain loadable/supported where documented, but they are not the desired new authoring model.
- **Legacy low-level steps/checks/effects**: compatibility authoring for existing plans. New normal workflows should prefer canonical behavior chains and engine-defined slots instead of arbitrary variable names.
- **Canonical state slots**: engine-defined persistent actor state such as `Facing` and `Target`. Prefer these over string-keyed action-plan variables for new authoring.

Target model:

- an entity Action Plan is an ordered list of engine-defined Action Steps;
- each Action Step is attempted in order until one succeeds or the chain terminates;
- one root action-plan resolution should produce exactly one observable action;
- internal state changes are engine-defined consequences, not arbitrary author-authored variable mutation;
- the final failed/impossible step terminates the root turn without requiring an explicit linked followup plan;
- canonical behavior chains should coexist with current low-level step/check/effect plans and transitional primitive-backed linked plans during implementation, with low-level authoring becoming advanced/legacy over time.

Current and planned Action Step / primitive support:

| Primitive | Status | Required state | Default state | Followup behavior |
|---|---|---|---|---|
| `MoveFacing` | Supported as transitional primitive-backed linked plan; supported as canonical Action Step at Core/descriptor/YAML/validation/editor service/agent API/GUI layers | `Facing` | `West` | Reads persistent actor `Facing`, moves one step, writes `Target` to blocker on blocked movement, and falls through to the next ordered Action Step in the canonical chain. Transitional descriptors follow explicit fallback or terminate the root turn. |
| `Wandering` | Superseded as named first-pass primitive | `Facing` | `West` | The first canonical authored behavior should be represented directly as an ordered chain such as `MoveFacing -> PickupTarget`, not as a separate `Wandering` primitive descriptor. Reverse-facing and future `onBump` behavior are not included yet. |
| `PickupTarget` | Supported as transitional primitive-backed linked plan; supported as canonical Action Step at Core/descriptor/YAML/validation/editor service/agent API/GUI layers | `Target` | `Self` | Reads persistent actor `Target`, attempts pickup into the first canonical inventory coordinate, and falls through to the next ordered Action Step when pickup fails. Transitional descriptors follow explicit fallback or terminate the root turn when pickup fails. |
| `DropFacing` | Supported as canonical Action Step at Core/descriptor/YAML/validation/editor service/agent API/GUI layers | `Facing` | `West` | Drops the first carried entity from actor inventory onto the floor in the actor's `Facing` direction; succeeds and consumes the turn when placement succeeds, otherwise falls through. |
| `PushFacing` | Supported as canonical Action Step at Core/descriptor/YAML/validation/editor service/agent API/GUI layers | `Facing` | `West` | Pushes the blocking entity one cell in `Facing`, then moves the actor into the blocker original cell; a successful push consumes the turn. Fails/falls through if there is no blocker or the pushed entity is blocked/out of bounds. |
| `DestroyTarget` | Supported as canonical Action Step at Core/descriptor/YAML/validation/editor service/agent API/GUI layers | `Target` | `Self` | Recursively destroys persistent actor `Target`, including its inventory space and contained entities. The current first pass rejects self-destruction. |
| `CreateFacing` | Supported as canonical Action Step at Core/descriptor/YAML/validation/editor service/agent API/GUI layers | `Facing` | `West` | Creates a placeholder rock-like entity in the actor's `Facing` direction when the destination is valid/open. This is a prototype for future spawning/projectile/clone steps and is expected to evolve. |
| `SeekTarget` | Conceptualized | `Target` | `Self` | Deferred until movement-toward-target semantics and target acquisition needs are concrete. |
| `BumpTarget` | Conceptualized | `Target` | `Self` | Deferred; may overlap with reaction slots, push, destroy, or future interaction steps. |
| `TeleportTo` | Conceptualized | target location TBD | TBD | Deferred because it likely requires a new location/destination state slot rather than overloading entity `Target`. |
| `Give` | Conceptualized | `Target` plus carried entity selection TBD | TBD | Deferred inventory-transfer primitive for moving carried entities into target inventory. |
| `Take` | Conceptualized | `Target` plus source inventory selection TBD | TBD | Deferred inventory-transfer primitive for taking from adjacent/target inventory into actor inventory. |

Canonical Action Step metadata is exposed by Core as the machine-readable source for editor/API discovery and validation. Initial metadata includes Action Step kind, display name, description/hint text, required state, defaultable state, state writes, and authoring tier.

Canonical behavior-chain traces can be summarized through `BehaviorChainTraceFormatter`. The compact formatter reports the root plan outcome, each attempted Action Step, success/failure reason, whether fallback continued or stopped, canonical state reads/writes such as `Facing` and `Target`, and the terminal consumed-turn outcome.

Canonical action-plan previews are exposed through `ContentEditorService.PreviewActionPlan` and `AgentContentEditorApi.PreviewActionPlan`. The preview is non-mutating and reports the selected plan shape, guidance, ordered canonical Action Steps and metadata when present, state hints/defaults, validation diagnostics relevant to the plan/entity, and YAML preview text. Preview commands should remain descriptive and must not introduce new engine semantics.

Behavior-chain validation/default policy:

- editor/API authoring materializes `Facing = West` for assigned entities when adding or assigning `MoveFacing` behavior where possible;
- defaultable `Target = Self` makes `PickupTarget` valid as a first Action Step, even if it is often a no-op/failure until some future or prior interaction sets a more useful target;
- mixed action-plan shapes are invalid for authored content: use only one of canonical `behavior`, transitional `primitive`, or legacy low-level `steps`;
- editing tools should not save empty behavior chains; removing the last behavior Action Step clears the behavior shape;
- if Core encounters an empty behavior chain anyway, it resolves as no turn rather than as a wait or failure action.

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

Canonical actor action state is persistent entity runtime state. `Facing` and `Target` are stored on the actor's entity action state during execution, while legacy named action-plan variables remain compatibility machinery for older low-level plans.

| State | Tier | Engine context | Content model | YAML | Validation | Editor service | GUI | Notes |
|---|---|---:|---:|---:|---:|---:|---:|---|
| Initial `Facing` | Stable | Yes | Yes | Yes | Yes | Yes | Yes | Canonical YAML is `actionStateDefaults.facing`; spawned actors initialize persistent entity action state. |
| Initial `Target` | Planned | Yes | Yes | Yes | Yes | Partial | Planned | Supported by model/runtime entity action state. Expose through Actor State when a concrete content use case appears. |

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

The movement primitive parity baseline was sufficient for the first in-process agent API facade. The current API/editor service parity baseline supports canonical ordered behavior-chain authoring for the first Action Steps and the first utility Action Step batch.

Agent API currently has an in-process `AgentContentEditorApi` facade in the Editor project. It wraps editor/content services for document/session snapshots, validation, entity template updates, actor initial facing, canonical behavior-chain Action Step metadata and authoring, legacy low-level action plans/steps, transitional primitive-backed linked plans, canonical checks, and canonical/advanced supported effects. It rejects legacy `SetVariable` effect authoring.

Agent API should continue to:

- wrap `ContentEditorService`, not edit YAML/DTOs directly;
- expose stable and advanced supported capabilities through typed commands;
- prefer typed commands for canonical ordered behavior-chain Action Steps for normal movement, pickup, drop, push, destroy, and prototype create authoring;
- avoid all legacy variable authoring;
- return structured results and validation diagnostics;
- reuse movement target/destination descriptors for `Teleport` and `Drop`;
- keep initial `Target`, `CanDrop`, and advanced turn-flag authoring deferred until concrete use cases appear.

See `Agent-Editor-API-Plan.md` for the implementation plan and next transport/protocol considerations.

## Upcoming behavior-system priorities

Near-term work should focus on:

1. exercise generated content with the first utility Action Step batch (`DropFacing`, `PushFacing`, `DestroyTarget`, `CreateFacing`);
2. reassess whether the next batch should prioritize `Wait`/`ReverseFacing`/`SeekTarget`-style behavior control or deferred transfer/location primitives.

Behavior templates, scheduler/speed work, reaction slots, diegetic action-plan entities, and broad new gameplay primitives remain conceptualized until selected for a concrete content/design need.
