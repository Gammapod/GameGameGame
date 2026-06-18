# Behavior Primitive/Fallback Foundation Archive

Status: Archived. This document records the completed primitive/fallback foundation work that led to the current behavior model consolidation direction. It is retained as implementation history, not as the active canonical behavior plan.

Active successor plan: `docs/Plans/Behavior-Model-Consolidation-Plan.md`.

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

## Revised 2a fallback-chain implementation plan

This section refines the near-term scope before reaction slots such as `onBump` are planned. The 2a model is: one primitive-backed action plan attempts one interaction; if that attempt fails and an explicit fallback plan is configured, the fallback is attempted; if no fallback is configured, the current root actor's turn resolves terminally.

The future `onBump` model remains conceptualized in the high-level roadmap and is intentionally out of scope for these phases.

### Phase 2a.1: Entity action state baseline

Purpose: separate persistent entity action state from action-plan-owned state before adding primitive fallback descriptors.

Engine/Core outcomes:

- `Facing` and `Target` are treated as persistent entity action state rather than canonical data owned by an `ActionPlanContext` instance.
- Existing interpreted plans can still read/write `Facing` and `Target` through a compatibility path during the transition.
- Existing low-level descriptor/runtime compatibility tests continue to pass.

Editor/content/frontend outcomes:

- Entity initial `Facing` authoring remains available and writes the same canonical content field.
- Existing content validation continues to report missing required `Facing`/`Target` sources for plans that need them.
- GUI and agent API behavior for setting initial `Facing` remains unchanged from a user perspective.

Measurable TDD outcomes:

- A plan that changes `Facing` persists that change on the actor entity/action state and a later turn reads the updated value.
- A plan that discovers a blocker can persist `Target` on the actor entity/action state.
- Existing canonical slot tests are revised to assert entity action state behavior rather than context-owned behavior.

Invariant trace:

- Revise `Action plan variables are typed, persist in context, and can be written by checks before later reads.` to distinguish persistent entity action state from legacy named variables and per-invocation context.
- Existing test basis: `ActionPlanContextStoresTypedCanonicalSlots`, `ActionPlanContextCanonicalSlotWritesAreTraced`, `ActionPlanContextCanonicalSlotReadsTraceMissingAndWrongKind`, `ActionPlanContextCanonicalSlotsPersistAcrossPlanExecutions`, `PlanPrimitiveCatalogDescribesCanonicalSlotUsage`, `ActionPlanDescriptorMaterializesCanonicalBuiltInsWithoutVariableNames`, and content validation tests for missing plan slots.

### Phase 2a.2: Primitive-backed descriptor with terminal fallback semantics

Purpose: add the canonical descriptor shape for one primitive attempt plus optional fallback while preserving old step/check/effect descriptors.

Engine/Core outcomes:

- A primitive-backed action plan descriptor can represent `primitive` plus optional `fallbackPlanId`.
- Missing fallback means terminal resolution of the current root actor's turn, not an implicit `Wait` fallback and not continuation to another ranked option.
- Fallback calls reuse or replace the existing `CallPlan` depth guard so temporal recursion remains explicitly guarded.
- Low-level step/check/effect descriptors continue to load and execute.

Editor/content/frontend outcomes:

- Content loader/saver round-trips primitive-backed plans and low-level plans during coexistence.
- Validation reports malformed primitive kind and missing fallback references.
- Editor service and GUI can display whether a plan is primitive-backed or low-level/advanced.
- Agent API can create a primitive-backed plan with a primitive kind and optional fallback reference.

Measurable TDD outcomes:

- YAML with a primitive-backed plan loads, materializes/interprets, saves, and reloads.
- A primitive plan with missing fallback reference produces an actionable validation diagnostic.
- A primitive plan with no fallback terminates resolution when its primitive fails.
- A fallback cycle fails with a trace once the depth guard is exceeded.

Invariant trace:

- Revise `Ranked action plans must distinguish failure that continues to the next action from failure that consumes the turn.` to fallback-chain semantics: action plan resolution distinguishes failure that follows an explicit fallback from terminal resolution that ends the current root actor's turn.
- Existing test basis: `PlanInterpreterUsesFirstSuccessfulConsumingRankedStep`, `PlanInterpreterReturnsFailureWhenNoStepConsumesOrStops`, `BuiltInCanMoveCheckFailureFallsThroughToSetVariableEffect`, `CallPlanEffectFailsWithTraceWhenDepthGuardIsExceeded`, `YamlContentLoaderCreatesRegistryFromDeclarativeContent`, `EditableContentDocumentCanLoadMaterializeSaveAndReloadYaml`, and content/editor validation tests for called-plan references.

### Phase 2a.3: First executable primitive: `MoveFacing`

Status: Implemented.

Purpose: prove primitive fallback behavior with the smallest useful turn-action primitive instead of a monolithic `Wandering` primitive.

Engine/Core outcomes:

- `MoveFacing` reads the actor's persistent `Facing` state and attempts adjacent movement.
- If movement succeeds, the root turn resolves successfully and consumes the actor's turn.
- If movement fails because an entity blocks the move, `MoveFacing` writes persistent actor `Target` to the blocker and follows fallback if configured; otherwise the root turn resolves terminally.
- If movement fails for another reason, fallback/terminal behavior is deterministic and traced.

Editor/content/frontend outcomes:

- Validation reports an assigned `MoveFacing` plan when the entity lacks initial or otherwise available `Facing` state.
- Editor service/GUI can author/select `MoveFacing` primitive-backed plans and choose a fallback plan.
- Agent API can create a `MoveFacing` plan, assign it as the entity's current default turn plan, and set initial `Facing`.

Measurable TDD outcomes:

- Unblocked `MoveFacing` moves according to actor `Facing`.
- Blocked `MoveFacing` stores the blocker as actor `Target` and attempts configured fallback.
- Blocked `MoveFacing` without fallback ends the root actor's turn with a clear trace.
- API-authored `MoveFacing` content validates with zero canonical diagnostics.

Invariant trace:

- Uses the revised persistent entity action-state invariant from Phase 2a.1.
- Uses the revised fallback-chain invariant from Phase 2a.2.
- Existing test basis: `ActionPlanDescriptorMaterializesCanonicalBuiltInsWithoutVariableNames`, `BuiltInCanMoveCheckAndMoveEffectMoveActorUsingCanonicalFacingSlot`, `BlockingEntityCheckWritesCanonicalTargetWhenCanonicalFacingIsBlocked`, `PlanPrimitiveCatalogDescribesCanonicalSlotUsage`, `PrototypeRegistryValidationAcceptsCanonicalFacingDefault`, `PrototypeRegistryValidationReportsMissingCanonicalFacingSlot`, `ContentEditorServiceEditsCanonicalActorActionStateDefaults`, `AgentContentEditorApiAuthorsMovementCapableContent`, plus new focused `MoveFacing` primitive tests.

### Phase 2a.4: Target fallback primitive: `PickupTarget`

Status: Implemented.

Purpose: prove a second primitive can consume the `Target` produced by a previous failed primitive and can itself fallback terminally.

Engine/Core outcomes:

- `PickupTarget` reads the actor's persistent `Target` state and attempts pickup using the current canonical pickup destination policy.
- If pickup succeeds, the root turn resolves successfully.
- If pickup fails, fallback is attempted if configured; otherwise the root turn resolves terminally.

Editor/content/frontend outcomes:

- Validation reports assigned `PickupTarget` plans that lack a valid `Target` source.
- Editor service/GUI can author `PickupTarget` primitive-backed plans and fallback references.
- Agent API can create the `MoveFacing -> PickupTarget` fallback chain without low-level checks/effects.

Measurable TDD outcomes:

- A blocked `MoveFacing` can set `Target`, fallback to `PickupTarget`, and pick up a valid adjacent target in one root turn chain.
- Failed `PickupTarget` without fallback terminates the root turn.
- Generated/API-authored content for this chain validates with zero canonical diagnostics.

Invariant trace:

- Uses the revised persistent entity action-state and fallback-chain invariants.
- Existing test basis: `PickupEffectUsesRelocationAfterPickupValidation`, `PickupFailsWhenTargetTotalWeightWouldExceedCapacity`, `CanPickupCheckAndPickupEffectUseCanonicalTargetSlot`, `PrototypeRegistryValidationAcceptsCanonicalTargetWrittenBeforePickup` adapted toward entity `Target` state, and editor/API movement-content authoring tests.

### Phase 2a.5: Recompose `Wandering` as authored fallback chain

Status: Implemented for the first supported chain shape.

Purpose: replace the old monolithic `Wandering` target with content/API authoring guidance that composes smaller primitives.

Engine/Core outcomes:

- The supported first wandering behavior is represented as configured primitive-backed plans, initially `MoveFacing` with fallback to `PickupTarget` or another explicitly supported fallback.
- Reverse-facing behavior is deliberately placed in the chain only after its primitive and terminal semantics are specified; it is not hidden inside unrelated fallback mechanics.

Implemented first chain:

```text
MoveFacing
  fallback -> PickupTarget
```

This chain lets an actor attempt to move using persistent `Facing`; when blocked by another entity, `MoveFacing` stores the blocker as persistent `Target`, then `PickupTarget` can attempt to pick up that target. Reverse-facing and `onBump`/reaction behavior remain separate future planning items.

Editor/content/frontend outcomes:

- Editor/API helpers can author a simple wandering actor using supported primitive-backed plans and initial `Facing`.
- Low-level step/check/effect authoring remains available as advanced/legacy compatibility but is not required for the canonical path.

Measurable TDD outcomes:

- Generated rat/wandering content can be authored through the agent API without low-level `CanMove`, `Move`, `BlockingEntity`, or `CallPlan` construction.
- The generated content validates canonically.
- Roadmap findings are updated to classify full `onBump` behavior as conceptualized/not planned until reaction slots are implemented.

Invariant trace:

- No additional invariant changes beyond the revised entity action-state and fallback-chain invariants are expected.
- Existing test basis: `AgentContentEditorApiAuthorsMovementCapableContent`, canonical validation tests rejecting legacy variable fields/`SetVariable`, and generated-content exercise tests.
