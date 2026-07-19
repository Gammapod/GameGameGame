---
id: source.action-step-outcome-and-affordance-logic
title: Action Step Outcome And Affordance Logic
kind: source-of-truth
subkind: action-logic
status: active
owners: [core-owner]
audience: [core-owner, content-editor, frontend-owner]
lane: action-logic
truth_rank: 25
truth_domains: [action-logic, runtime-behavior]
read_when:
  - answering what an Action Step does on success failure and fallthrough
  - translating actor actee and spatial relationship into available verbs
  - designing editor affordance displays action-plan previews or content-facing interaction summaries
do_not_read_when:
  - checking layer support tiers
  - checking content-authoring workflows and common chains
  - changing stable Core behavior or tests
related:
  - source.engine-editor-capabilities
  - source.content-authoring-manual
  - source.invariants
---
# Action Step Outcome And Affordance Logic

Status: Source-of-truth companion for reasoning about canonical Action Step outcomes and current actor/actee verb affordances.

Read when:

- answering "what does this Action Step do on success, failure, and fallthrough?";
- translating actor metadata, actee metadata, and spatial relationship into available verbs;
- designing editor affordance displays, action-plan previews, or content-facing interaction summaries.

Do not read when:

- checking layer support tiers; use `docs/Source of Truth/Engine-Editor-Capabilities.md`;
- checking content-authoring workflows and common chains; use `docs/Source of Truth/Content-Authoring-Manual.md`;
- changing stable Core behavior or tests; use `docs/Source of Truth/invariants.md` first.

This document is descriptive. It does not add new engine/editor concepts. The executable sources remain Core Action Step implementations, `ActionStepCatalog`, controlled-command services, and their traced tests.

## Outcome vocabulary

| Term | Meaning |
|---|---|
| Consumes turn | The Action Step succeeds as the observable root action and the canonical behavior chain stops. |
| Continues | The Action Step succeeds without consuming the turn and the chain attempts the next step. |
| Falls through | The Action Step cannot complete; the chain attempts the next ordered step when one exists. |
| Terminal no-turn | If every attempted step falls through or succeeds non-consumingly without a later consuming step, root resolution ends without consuming a turn. |
| Writes state | Persistent actor action state or world state changes. |
| Preserves state | The named state is intentionally left unchanged on both success and failure unless another step changes it. |

## Action Step outcome table

| Step | Actor metadata required | Actee / target metadata required | Spatial relationship required | Success outcome | Failure / fallthrough outcome |
|---|---|---|---|---|---|
| `Move` | `directionMode`; `Facing` when `directionMode` is relative | none | Resolved 8-way adjacent destination is valid/open; diagonal move is blocked only when both orthogonal corners are blocked | Actor moves one cell; turn consumed; post-action `Facing` is actual absolute movement direction | Invalid/out-of-bounds/blocked movement falls through without moving, changing `Facing`, or writing `Target`. |
| `MoveFacing` | `Facing` | none, unless blocked | Destination one cell in `Facing` is valid/open | Actor moves one cell; turn consumed; post-action `Facing` is movement direction | If blocked by entity, writes blocker to `Target` and falls through; invalid/out-of-bounds/no movement falls through without useful target write. |
| `Backstep` | `Facing` | none, unless blocked | Destination one cell opposite `Facing` is valid/open | Actor moves opposite `Facing`; turn consumed; original `Facing` is preserved | If blocked by entity, writes blocker to `Target` and falls through; invalid/out-of-bounds falls through. |
| `TransformAdjacentToInventory` / `PickupTarget` | `Target`; usable actor inventory with space | Target exists, is not actor, and fits actor aperture | Target is in an adjacent pickup-valid peer/world location for the actor | Target moves into first valid actor inventory coordinate in row-major order; turn consumed | Missing/self/invalid/non-adjacent target, no usable actor inventory, no space, or aperture failure falls through. |
| `TransformInventoryToAdjacent` / `DropFacing` | `Facing`; actor carries at least one entity | First carried entity fits source/container aperture rules | Destination one cell in `Facing` on actor plane is valid/open | First carried entity drops to facing cell; turn consumed | Nothing carried, invalid/blocked destination, or aperture failure falls through. |
| `PushFacing` | `Facing` | Blocking entity exists in facing cell | Blocker's next cell in same direction is valid/open | Blocking entity moves one cell; actor moves into blocker original cell; turn consumed | No blocker, blocked push destination, out-of-bounds, or invalid movement falls through. |
| `DestroyTarget` | `Target` | Target exists and is not actor | No adjacency requirement | Target and its contained descendants/inventory space are destroyed; turn consumed | Missing/self/invalid target falls through. |
| `CreateFacing` | `Facing` | Placeholder spawn is engine-defined, not template-authored | Destination one cell in `Facing` is valid/open | Placeholder entity is created in facing cell; turn consumed | Blocked/out-of-bounds/invalid destination falls through. |
| `TurnLeft` | `Facing` | none | none | `Facing` rotates counter-clockwise; turn consumed | Falls through only if required state cannot be resolved. Legacy for new authoring. |
| `TurnRight` | `Facing` | none | none | `Facing` rotates clockwise; turn consumed | Falls through only if required state cannot be resolved. Legacy for new authoring. |
| `ReverseFacing` | `Facing` | none | none | `Facing` reverses; turn consumed | Falls through only if required state cannot be resolved. Legacy for new authoring. |
| `AcquireNearestTarget` | actor has same-plane position | Any same-plane non-self entity | Same plane; nearest by Manhattan distance; tie-break row-major then entity ID | Writes nearest entity to `Target`; succeeds and continues without consuming | No candidate falls through without overwriting `Target`. Legacy for new authoring; prefer template `targetingRules`. |
| `SeekTarget` | `Target` | Target exists, is not actor, same plane | At least one valid/open cardinal move reduces Manhattan distance; tie-break `North`, `South`, `West`, `East` | Actor moves one reducing step; turn consumed; `Target` preserved | Missing/self/off-plane target, adjacent target contact cell, blocked/out-of-bounds, or no reducing move falls through; `Target` preserved. |
| `FleeTarget` | `Target` | Target exists, is not actor, same plane | At least one valid/open cardinal move increases Manhattan distance; tie-break `North`, `South`, `West`, `East` | Actor moves one increasing step; turn consumed; `Target` preserved | Missing/self/off-plane target or no valid escape move falls through; `Target` preserved. |
| `MaintainChebyshevDistanceTwo` | `Target` | Target exists, is not actor, same plane | Valid/open cardinal move improves Chebyshev distance toward 2 | Actor moves toward distance 2; turn consumed; `Target` preserved | Exactly distance 2, missing/self/off-plane target, or no improving move falls through; `Target` preserved. |
| `StrafeClockwise` | `Target` | Target exists, is not actor, same plane | Primary seek direction exists; clockwise perpendicular destination is valid/open | Actor moves clockwise perpendicular to seek direction; turn consumed; `Target` preserved | Missing/self/off-plane target, no primary direction, blocked/out-of-bounds strafe destination falls through; `Target` preserved. |
| `StrafeAnticlockwise` | `Target` | Target exists, is not actor, same plane | Primary seek direction exists; anticlockwise perpendicular destination is valid/open | Actor moves anticlockwise perpendicular to seek direction; turn consumed; `Target` preserved | Same failure set as `StrafeClockwise`; `Target` preserved. |
| `GiveTarget` | `Target`; actor carries at least one entity | Target exists, is not actor, has usable inventory with space; carried item fits both apertures | No adjacency requirement beyond target validity/current state | First actor-carried entity transfers to first valid target inventory coordinate, source and destination both row-major; turn consumed | Missing/self/invalid target, nothing carried, no usable target inventory, no space, or source/destination aperture failure falls through. |
| `TakeTarget` | `Target`; actor has usable inventory with space | Target exists, is not actor, has usable inventory and at least one carried entity; taken item fits apertures | No adjacency requirement beyond target validity/current state | First target-carried entity transfers to first valid actor inventory coordinate, source and destination both row-major; turn consumed | Missing/self/invalid target, empty/no usable target inventory, no usable actor inventory, no space, or aperture failure falls through. |
| `EnterTarget` | `Target`; actor bulk fits target aperture | Target exists, is not actor, has usable inventory with space | Actor is adjacent to target on same plane | Actor moves into first valid target inventory coordinate row-major; turn consumed | Missing/self/non-adjacent target, no usable target inventory, no space, or destination owner aperture failure falls through. |
| `ExitFacing` | `Facing`; actor bulk fits current container aperture | Current inventory owner/container exists | Actor is currently in an entity inventory; destination adjacent to container in `Facing` is valid/open | Actor moves out to adjacent container cell; turn consumed | Not contained, missing container, blocked/out-of-bounds destination, or source/container aperture failure falls through. |
| `ApplyPrePlan` | target label preferred or target slot default `1`; `planId` | Target exists; referenced plan exists | No adjacency requirement | Target entity receives/replaces one-turn `Pre` override; applying actor's turn consumed | Missing target, missing target entity, missing `planId`, or unknown plan falls through. |
| `ApplyMainPlan` | target label preferred or target slot default `1`; `planId` | Target exists; referenced plan exists | No adjacency requirement | Target entity receives/replaces one-turn `Main` override; applying actor's turn consumed | Same failure set as `ApplyPrePlan`. |
| `ApplyPostPlan` | target label preferred or target slot default `1`; `planId` | Target exists; referenced plan exists | No adjacency requirement | Target entity receives/replaces one-turn `Post` override; applying actor's turn consumed | Same failure set as `ApplyPrePlan`. |

## Actor/actee/spatial logic table

This table answers: for a given actor, actee, and spatial relationship, what verbs can the actor perform on the actee with current supported semantics?

Use it as a decision table for editor affordance summaries. Final command/step execution remains authoritative because occupancy, aperture, inventory space, and target state can change between query and execution.

| Actor metadata | Actee metadata | Spatial relationship | Verbs currently supported | Extra conditions / notes |
|---|---|---|---|---|
| Actor has plane position and `Facing` | No actee required | Facing destination is open | move, create | `MoveFacing` moves actor; `CreateFacing` creates placeholder. Controlled affordance exposes direct move, not create. |
| Actor has plane position and `Facing` | Entity occupies facing cell | Adjacent in facing direction | target, push, enter, later pickup | `MoveFacing`/`Backstep` can write blocker to `Target` when blocked; `PushFacing` can push if next cell is open; `EnterTarget` can enter if target has usable inventory/space/aperture; pickup requires pickup-valid target plus actor inventory/space/aperture. |
| Actor has usable inventory and aperture admits actee bulk | Actee is a peer/world entity | Adjacent to actor | pickup / transform adjacent to inventory | Controlled affordance exposes adjacent pickup sources and row-major actor-inventory destinations. Preferred canonical name is `TransformAdjacentToInventory`; compatibility `PickupTarget` uses the same current `Target` semantics. |
| Actor carries one or more entities | Chosen actee is carried by actor | Actee is inside actor inventory | drop / transform inventory to adjacent | Controlled affordance exposes carried drop sources; the first Core Action Choice Drop seam exposes adjacent map destinations while legacy/direct affordance data may still enumerate broader actor-plane destinations. Preferred canonical name is `TransformInventoryToAdjacent`; compatibility `DropFacing` drops the first carried item into facing cell only. Aperture applies when exiting actor inventory. |
| Actor carries one or more entities and has current `Target` | Target has usable inventory/space | Target exists; no adjacency requirement | give | `GiveTarget` transfers actor's first carried entity to target inventory using row-major source/destination. Both source and destination aperture checks apply. |
| Actor has usable inventory/space and current `Target` | Target carries one or more entities | Target exists; no adjacency requirement | take | `TakeTarget` transfers target's first carried entity to actor inventory using row-major source/destination. Both source and destination aperture checks apply. |
| Actor has current `Target` | Target exists, not actor, same plane | Any same-plane relationship | seek, flee, maintain distance, strafe | Target-relative movement verbs consume the turn only when their selected cardinal destination is valid/open and improves the step-specific relationship. |
| Actor has current `Target` | Target exists and is not actor | No adjacency requirement | destroy, apply pre/main/post plan | `DestroyTarget` removes target recursively. Apply-plan steps require a valid `planId` and install one-turn overrides on the target. |
| Actor has current `Target` and actor bulk fits target aperture | Target has usable inventory/space | Adjacent on same plane | enter | `EnterTarget` moves actor into target inventory. Destination is first valid target inventory coordinate row-major. |
| Actor is inside an entity inventory and has `Facing` | Actee is current container owner | Destination adjacent to container in `Facing` | exit | `ExitFacing` moves actor to the container's parent plane. Source/container aperture applies. |
| Actor has same-plane position | Any same-plane non-self entity | Same plane | acquire target | `AcquireNearestTarget` writes nearest same-plane entity by deterministic tie-break and continues; this is legacy for new authoring. Prefer `targetingRules`. |
| Actor has `Facing` | No actee required | No spatial movement required | turn left/right/reverse | Metadata-only facing verbs consume a turn but are legacy for new normal authoring. Prefer movement and template targeting rules. |

## Verb availability notes

- "Can perform" means the engine has a supported canonical Action Step or controlled command for the verb and the listed predicates are true.
- Controlled direct-player affordances currently cover `move`, `pickup`, `drop`, `enter`, and `exit` only. Other verbs are behavior-chain Action Steps and may appear in previews/traces rather than direct-player command affordances.
- Actor/actee metadata includes runtime facts, not just authored template facts: current plane, current inventory owner, current `Facing`, current target slots/labels, inventory dimensions, contents, bulk, aperture, and current occupancy.
- Runtime entities do not currently carry template IDs for Action Step filtering. Author target selection by template through `targetingRules`, which write runtime target slots/labels before plan evaluation.
- Constrained inventory verbs (`PickupTarget`, `DropFacing`, `GiveTarget`, `TakeTarget`, `EnterTarget`, `ExitFacing`) enforce Bulk/Aperture transition rules. `Teleport` remains the advanced unconstrained relocation primitive and is outside the canonical verb table above.
