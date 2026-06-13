# Movement Primitives Parity Plan

## Goal

Lock in movement primitives as a coordinated Core + Editor capability set before building the agent/editor API layer.

Movement primitives should share a common relocation model while still exposing constrained, author-friendly primitives in content and editor tooling:

- `Teleport`: move any target entity to any valid destination.
- `Move`: teleport self to an adjacent peer/world location derived from canonical `Facing`.
- `Pickup`: teleport a peer/world entity into a carried inventory space.
- `Drop`: teleport a carried entity into a peer/world inventory/location space.

`Teleport` is the engine-level ur-primitive. `Move`, `Pickup`, and `Drop` are constrained movement primitives built on the same semantics where practical, not unrelated one-off effects.

## Accepted direction

- Keep friendly primitives in the editor. Do not require authors or agents to express every common movement operation as arbitrary `Teleport` action plans.
- Add general relocation concepts carefully, with typed source/destination locations instead of arbitrary variable names.
- Preserve canonical slot authoring: primitives imply their required slots and literal parameters.
- Finish current cleanup before implementing movement changes:
  - remove hidden legacy variable input plumbing,
  - make GUI turn behavior fixed by default,
  - keep advanced descriptor/runtime fields only where already supported,
  - keep `SetVariable` legacy display/load/runtime compatibility only.
- Defer agent/editor API until movement primitive structure is implemented and validated through Core + Editor parity.

## Location model to design first

Before implementing arbitrary `Teleport` or `Drop`, define a typed movement location model that can represent at least:

- actor/self identity,
- canonical `Target` entity identity,
- explicit entity identity for advanced teleport,
- world/peer grid coordinate,
- actor-relative/facing-derived destination,
- carried inventory coordinate,
- carried entity identity.

The location model should be serializable in descriptors/YAML, catalogued for editor field generation, and validated without arbitrary variable-name wiring.

Open design questions:

- Is “peer” represented as same world/container as the actor, or as a formal location scope?
- Does `Drop` destination mean actor world location by default, an adjacent facing location, or an authored destination?
- Does `Teleport` require both explicit source and destination, or can source be inferred from the target entity's current location?
- Which movement operations consume turns by default?
- Which failed preconditions should be checks (`CanTeleport`, `CanDrop`) versus effect failure behavior?

## Proposed primitive semantics

### `Teleport`

General relocation primitive.

Initial intended shape:

- target: any entity, selected through typed location/entity reference fields;
- destination: any valid destination location;
- source: inferred from target's current location unless a future use case requires explicit source matching.

Core responsibilities:

- validate target exists,
- validate destination exists and accepts the target,
- remove target from current container/location,
- insert target at destination,
- produce clear trace/diagnostic output.

Editor responsibilities:

- expose typed target and destination fields,
- validate descriptor field combinations,
- keep advanced authoring separate from simple `Move`/`Pickup`/`Drop` workflows.

### `Move`

Constrained teleport of self to an adjacent peer/world location.

Initial intended shape:

- target: self/actor,
- destination: adjacent location derived from canonical `Facing`,
- precondition: destination must be traversable/unblocked.

Existing `Move` should migrate toward this model without regressing current canonical `Facing` behavior.

### `Pickup`

Constrained teleport of a peer/world entity into carried inventory.

Initial intended shape:

- target: canonical `Target` written by a targeting/check primitive,
- destination: authored carried inventory coordinate,
- precondition: target is pickup-able and destination inventory cell is valid/available.

Existing `Pickup` should migrate toward this model without regressing current canonical `Target` + literal inventory coordinate behavior.

### `Drop`

Constrained teleport of a carried entity into a peer/world location.

Initial intended shape:

- target: a carried entity or inventory coordinate,
- destination: peer/world location, likely actor-relative or authored depending on the final location model,
- precondition: carried entity exists and destination can accept it.

`Drop` should not be added as an action-plan primitive until the typed location model can express its target and destination cleanly.

## Implementation phases

### Phase 0: Finish current cleanup - complete

Completed before movement changes:

- Removed remaining hidden variable input plumbing from GUI/view-model.
- Fixed GUI-authored `ReverseDirection` turn behavior while keeping descriptor/runtime fields as advanced compatibility.
- Kept legacy `SetVariable` display/load/runtime compatibility only.
- Updated docs/tests after cleanup.

### Phase 1: Introduce typed relocation model in Core - initial complete

Initial implementation added typed movement destinations and common relocation evaluation/execution in Core.

Expected outcomes:

- Core can represent supported movement target/destination references.
- Core can move an entity between compatible locations through a common relocation path.
- Existing direct move/pickup behavior can be expressed through the relocation path or an adapter around it.
- Invalid locations produce structured failures/traces.

Implemented initial destination forms:

- explicit `PlaneCoord`,
- inventory slot by owner entity + inventory coordinate,
- adjacent location by anchor entity + direction.

Remaining Phase 1 follow-up before Phase 2/3 if needed:

- decide whether movement targets need a matching typed reference model now, or whether explicit `EntityId` is sufficient until `Teleport` descriptors are introduced,
- decide whether the relocation path should replace `TryMove`/`TryPlace` internals now or during Phase 4,
- refine failure reasons if generic relocation needs movement-specific diagnostics beyond the current reused reasons.

### Phase 2: Add catalog/descriptor/YAML representation - initial complete

Expected outcomes:

- `PlanEffectDescriptor`/catalog can describe movement primitive fields without arbitrary variable names.
- YAML can load/save `Teleport`, `Move`, `Pickup`, and `Drop` descriptors once each primitive is implemented.
- Legacy/current `Move` and `Pickup` YAML remains compatible.
- Validation reports missing/invalid movement targets and destinations with actionable diagnostics.

Initial implementation added:

- `MovementTargetDescriptor` with `Self`, `CanonicalTarget`, `Entity`, and `CarriedInventoryCoord` target kinds.
- `MovementDestinationDescriptor` with `PlaneCoord`, `InventorySlot`, `AdjacentToSelf`, `AdjacentToEntity`, and `AdjacentToCanonicalTarget` destination kinds.
- `Teleport` and `Drop` `PlanEffectKind` values plus descriptor factories.
- Primitive catalog field kinds for movement target/destination fields.
- YAML load/save support for movement target/destination descriptors.

Remaining Phase 2 follow-up:

- validation diagnostics for invalid movement target/destination field combinations,
- editor-service field-level authoring operations for movement descriptors,
- GUI exposure after runtime support is implemented or deliberately gated.

### Phase 3: Implement `Teleport` - runtime complete

Expected outcomes:

- Core action-plan effect can teleport a target entity to an authored destination.
- Editor service can author/update teleport fields.
- GUI can expose teleport as an advanced movement primitive.
- Tests cover world-to-world and at least one inventory-related relocation, depending on Phase 1 location support.

Implemented:

- `TeleportEffect` materializes from `PlanEffectDescriptor.Teleport`.
- Runtime target resolution supports self, canonical target, explicit entity, and actor-carried inventory coordinate.
- Runtime destination resolution supports explicit plane coordinate, explicit inventory slot, adjacent to self/entity/canonical target.
- Descriptor-level editor-service authoring can set teleport effects and update movement target/destination fields.
- Validation reports malformed teleport movement target/destination descriptors.
- Tests cover explicit world relocation, canonical target to inventory relocation, and carried-inventory-coordinate target relocation.

Remaining Phase 3 follow-up:

- expose teleport fields in GUI only after the advanced movement authoring UX is designed, done with initial advanced movement fields,
- decide whether generic teleport should enforce extra game rules for inventory capacity or intentionally remain arbitrary relocation.

### Phase 4: Rebase `Move` and `Pickup` on relocation semantics - complete

Expected outcomes:

- Runtime behavior remains compatible with current tests.
- Catalog identifies `Move` and `Pickup` as movement primitives with constrained target/destination fields.
- Editor remains simple: no arbitrary variable names, only required literal fields.

Implemented:

- `MoveAction` evaluates and executes through the shared relocation path using an adjacent-to-self destination.
- `PickupAction` keeps pickup-specific gameplay validation, including adjacency and carrying capacity, then uses the shared relocation path for final placement.
- Generic `Teleport` remains arbitrary advanced relocation; inventory capacity is still enforced by constrained `Pickup`, not by generic relocation.
- Tests assert both `Move` and `Pickup` traces include relocation path usage while existing behavior remains compatible.

### Phase 5: Add `Drop` - runtime complete

Expected outcomes:

- Core action-plan effect supports dropping a carried entity to a valid destination.
- Validation and editor service can author drop parameters.
- GUI exposes drop using constrained movement fields.
- Content can use drop through action plans with engine/editor parity.

Implemented:

- `DropEffect` materializes from `PlanEffectDescriptor.Drop`.
- Runtime target resolution reuses movement target descriptors and supports carried-inventory-coordinate targets.
- Runtime destination resolution reuses movement destination descriptors.
- `DropAction` keeps drop-specific validation, including target-in-actor-inventory and destination-on-actor-plane checks, then uses the shared relocation path for final placement.
- Descriptor-level editor-service authoring can set drop effects and update movement target/destination fields.
- Validation reports malformed drop movement target/destination descriptors.
- Tests cover dropping a carried inventory-coordinate target to an adjacent peer/world location and failure when an explicit target is not carried by the actor.

Remaining Phase 5 follow-up:

- expose drop fields in GUI after constrained movement authoring UX is designed, done with initial movement fields,
- `CanDrop` is intentionally deferred for now; drop effect validation is sufficient for the first authoring pass unless a concrete branching use case appears.

### Phase 6: Agent/editor API readiness check

Start agent API only after:

- movement primitives have stable descriptor shapes,
- editor service can author all movement primitive fields,
- validation catches invalid movement descriptors,
- GUI can exercise common movement primitive authoring paths,
- capabilities manual marks movement primitive parity status accurately.

Status: ready to start planning the agent/editor API. Remaining GUI polish can proceed in parallel with API design as long as it uses the existing editor-service movement field operations.

## Testing policy

Follow TDD for implementation phases:

1. Add a failing Core test for the engine capability.
2. Implement Core behavior.
3. Add Content/YAML/editor-service tests for authoring and validation.
4. Add GUI/view-model tests for exposed workflows.
5. Run targeted tests, then full solution tests.

Use the temp test output path when needed:

```powershell
dotnet test "GameGameGame.slnx" -m:1 -p:BaseOutputPath="C:\Users\Scramble\AppData\Local\Temp\opencode\ggg-test-bin\"
```
