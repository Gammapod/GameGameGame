# Behavior Primitive Action Plans

Status: Phase 1 model definition.

This document defines the intended canonical authoring model for action plans. The goal is to expose action plans as rearrangeable behavior primitives rather than as arbitrary scripts made from low-level checks, effects, and mutable variables.

## Canonical model

An action plan represents one attempted behavior and one optional followup action plan.

Core rule:

- one action-plan resolution should produce exactly one action;
- internal state changes are engine-defined consequences of behavior primitives, not author-authored variable mutation;
- if an attempted behavior cannot resolve its action and no followup is configured, the plan resolves to `Wait`;
- followup plans are alternate attempts, not additional actions in the same resolution.

Canonical action plan shape:

```yaml
  wandering:
    primitive: Wandering
    followupPlanId: handleBlocker
```

The exact descriptor field names may change during implementation, but canonical authoring should preserve this model: one primitive behavior plus one generic followup plan reference.

## Compatibility stance

Primitive-backed plans should coexist with existing step/check/effect plans for now because coexistence is cleaner and preserves current content compatibility.

Long-term direction: primitive-backed action plans should replace low-level step/check/effect authoring as the normal content model. Low-level plans may remain as legacy, advanced, or internal implementation machinery until there is a deliberate migration/removal plan.

## Authoring principles

Authors should be able to:

- assign an action plan to an entity;
- configure the plan's generic followup plan;
- initialize primitive-required state through supported editor controls;
- rearrange plans by changing followup references and entity assignments.

Authors should not need to:

- create arbitrary actions;
- create arbitrary low-level checks/effects;
- set arbitrary internal variables;
- define required internal state slots separately from assigning a primitive that needs them.

## Required state and defaults

Behavior primitives imply their required state. If a primitive needs initial state, the engine/editor should provide a default that authors can change.

Initial defaults:

| State | Default | Notes |
|---|---|---|
| `Facing` | `West` | Used by `Wandering`. |
| `Target` | `Self` | Used by target-oriented plans such as `SeekTarget`, `PickupTarget`, and `BumpTarget`. |

Validation should still ensure assigned primitive-backed plans have a valid source/default for required state after descriptor support exists.

## Primitive catalog, first pass

### `Wandering`

Intent: move in the current facing direction; if blocked, prepare the blocker for followup and reverse facing for the next turn.

Required state:

- `Facing`, defaulting to `West` when initialized through canonical authoring.

Authorable configuration:

- optional initial `Facing` on the entity;
- optional generic `followupPlanId`.

Engine-owned behavior:

1. Attempt to move in `Facing` direction.
2. If movement succeeds, the plan resolves to that move action.
3. If movement fails because movement is blocked:
   - set canonical `Target` to the blocking entity when one exists;
   - reverse canonical `Facing` for the next turn;
   - resolve the configured followup plan, or `Wait` if no followup is configured.
4. If movement fails without a usable blocker/followup target, resolve the configured followup plan if present, otherwise `Wait`.

### `SeekTarget`

Intent: move toward the current target entity; if movement fails for any reason, use the generic followup.

Required state:

- `Target`, defaulting to `Self` when initialized through canonical authoring.

Authorable configuration:

- optional initial `Target` on the entity;
- optional generic `followupPlanId`.

Engine-owned behavior:

1. Determine the direction that brings the actor closest to `Target`.
2. Prefer counter-clockwise directions when more than one direction ties.
3. Attempt to move in the selected direction.
4. If movement succeeds, the plan resolves to that move action.
5. If movement fails for any reason, resolve the configured followup plan, or `Wait` if no followup is configured.

Followup plans can perform their own differentiation, such as checking whether the current target is adjacent, attacking, retargeting, or clearing target state once those primitives exist.

### `PickupTarget`

Intent: attempt to pick up the current target; if pickup fails, use the generic followup.

This is the primitive interpretation of the current `handleBlocker` pickup behavior.

Required state:

- `Target`, defaulting to `Self` when initialized through canonical authoring.

Authorable configuration:

- optional generic `followupPlanId`;
- pickup destination policy may initially use the same behavior as existing content, such as the first configured inventory coordinate, until a better canonical parameter is designed.

Engine-owned behavior:

1. Attempt to pick up `Target`.
2. If pickup succeeds, the plan resolves to that pickup action.
3. If pickup fails, resolve the configured followup plan, or `Wait` if no followup is configured.

### `BumpTarget`

Intent: perform the current fallback interaction used by `handleBlocker` when pickup is not possible.

Required state:

- `Target`, defaulting to `Self` when initialized through canonical authoring.

Authorable configuration:

- optional generic `followupPlanId`, though the first pass may not need one.

Engine-owned behavior:

1. Resolve whatever the current `handleBlocker` fallback effectively does today.
2. If that is only recording a successful bump/turn-consuming interaction, `BumpTarget` should do that in the first pass.
3. If it cannot resolve, use the configured followup plan, or `Wait` if no followup is configured.

## Current low-level implementation reuse

The following existing concepts can be reused during implementation:

- `ActionPlanId` and `ActionPlanTemplateId`;
- canonical `Facing` and `Target` action-state slots;
- `ActionPlanContext`;
- `MoveAction`, `PickupAction`, `WaitAction`, and existing movement services;
- existing plan-call recursion/depth protection, reframed as followup resolution;
- existing validation and diagnostic infrastructure;
- editor session/service infrastructure;
- the agent API result and validation surface.

The following should not be part of canonical primitive authoring:

- arbitrary `SetVariable` effects;
- arbitrary variable names;
- arbitrary low-level check/effect assembly;
- configurable `ConsumesTurn` and `ContinuePlan` flags.

## Phase 2 implementation targets

Phase 2 should prove the model with executable behavior and content descriptors.

Testable outcomes:

- primitive-backed descriptors can represent `Wandering`, `SeekTarget`, `PickupTarget`, and `BumpTarget` or an explicitly chosen first subset;
- unblocked `Wandering` moves according to `Facing`;
- blocked `Wandering` updates `Target`, reverses `Facing`, and resolves its followup or `Wait`;
- unblocked `SeekTarget` moves toward `Target` with counter-clockwise tie-breaking;
- blocked/failed `SeekTarget` resolves its followup or `Wait`;
- `PickupTarget` reproduces the pickup portion of current `handleBlocker` behavior;
- `BumpTarget` reproduces the current `handleBlocker` fallback behavior;
- existing low-level plan YAML remains loadable during the coexistence period.
