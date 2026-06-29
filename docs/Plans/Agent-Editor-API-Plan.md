# Agent Editor API Plan

## Goal

Provide a stable, constrained API that agents can use to author content through the same engine/editor capability model used by the editor service and future frontend/editor surfaces. The current Avalonia GUI is legacy-priority and should not define the agent API contract.

The API should not expose legacy arbitrary-variable authoring. It should operate on canonical engine concepts:

- entity templates and presentations,
- actor action-state defaults,
- entity targeting rules and numeric target slots,
- action plans and steps,
- canonical checks,
- movement primitives: `Teleport`, `Move`, `Pickup`, `Drop`,
- typed movement target/destination descriptors.

## Readiness baseline

Movement primitive parity is now sufficient to begin API planning:

- Core runtime supports `Teleport`, `Move`, `Pickup`, and `Drop`.
- Descriptor/YAML support exists for movement target/destination fields.
- Content validation reports malformed movement descriptors.
- Editor service can author full movement descriptors and movement fields.
- The current Avalonia GUI can exercise common movement primitive authoring paths, but broad GUI parity is no longer required for future API work.
- `CanDrop` is intentionally deferred until a concrete branching use case appears.

## API shape recommendation

Build the agent API as a thin command layer over `ContentEditorService`, not as a direct DTO/YAML editor.

Initial command families:

1. Document/session commands
   - load/create/save content document,
   - validate document,
   - retrieve YAML preview/diff.

2. Entity template commands
   - list/create/update/delete templates,
   - edit presentation,
   - edit inventory dimensions/weight/capacity,
   - edit carried entities.

3. Actor state commands
    - set/clear initial facing,
    - list/set/remove targeting rules for template-scoped target acquisition,
    - set behavior-step `targetSlot` when a plan consumes a non-default target slot.

4. Action plan commands
   - list/create/delete plans,
   - add/remove/reorder steps,
   - set step labels,
   - set default action plan.

5. Check commands
   - add/update/remove `CanMove`, `BlockingEntity`, `CanPickup`,
   - edit `CanPickup` inventory coordinate.

6. Effect commands
   - set/clear `Wait`, `Move`, `Pickup`, `ReverseDirection`, `CallPlan`, `Teleport`, `Drop`,
   - edit `Pickup` inventory coordinate,
   - edit `CallPlan` reference,
   - edit movement target/destination for `Teleport` and `Drop`.

## Guardrails

- Do not expose `SetVariable` as an authoring command.
- Do not expose arbitrary `directionVariable`, `targetVariable`, or `variableName` fields.
- Do not expose legacy target-acquisition or turn-only facing mutation as normal canonical authoring.
- Prefer typed IDs and descriptors over raw strings where possible.
- Every mutating command should return validation status or enough context for the caller to validate immediately after.
- Command failures should be structured and actionable, not only exception text.

## First implementation slice

Status: Implemented as an in-process facade in `src/GameGameGame.Editor/AgentContentEditorApi.cs`.

Before adding any network/tool protocol, add an in-process API facade around `ContentEditorService` with tests.

Suggested first slice:

- `AgentContentEditorApi` or similarly named service in the Editor project.
- Methods that wrap existing service operations and return simple result DTOs.
- Tests that author a minimal movement-capable plan:
  1. create/open document,
  2. create action plan with one step,
  3. set initial facing,
  4. add `CanMove`,
  5. set `Move`,
  6. set `Drop` or `Teleport` with typed movement fields,
  7. validate and inspect YAML.

Current test coverage includes authoring movement-capable content through the facade, validating canonical authoring, inspecting generated YAML, and rejecting legacy `SetVariable` authoring.

After the in-process API is exercised against generated test content, expose it through whatever agent transport/protocol is chosen.

## First generated-content exercise

Exercise prompt: use the facade to author a barrel, a trap, and a rat.

Results:

- Barrel: authorable with the current API as a passive container entity.
- Rat: partially authorable with the current API by reusing the existing wandering action plan and canonical initial facing.
- Rat taking two actions per turn: blocked by current engine turn semantics. Current turn/action execution stops when an effect consumes the turn, and the currently supported movement/action effects consume through action resolution.
- Trap bumping entities in all four directions per turn: blocked by current engine semantics and intentionally blocked API surface. The API correctly rejects legacy `SetVariable` authoring, so content cannot rotate arbitrary facing variables through the agent API. Current engine movement/teleport-style effects also do not provide a canonical multi-effect-per-turn primitive for this behavior.

Lessons:

- The facade is usable for ordinary template/action-plan authoring.
- The `SetVariable` rejection is working as intended and should not be relaxed just to recover legacy directional scripting.
- Multi-action-per-turn and multi-direction trap behavior should be modeled as future engine capabilities, not as arbitrary variable mutation.
- Saving an existing content file through the facade may canonicalize legacy YAML, such as `defaultPlanVariables.facing` into `actionStateDefaults.facing`. Agent exercises should use temporary/generated content files unless the task explicitly intends to migrate checked-in content.

Recommended next API work before external transport:

1. Add a documented generated-content exercise workflow that uses temporary files or new documents instead of editing checked-in prototype content.
2. Add dry-run/save-preview guidance or API support so agents can inspect canonicalization changes before writing files.
3. Add higher-level content authoring helpers for common patterns, such as passive containers and actors that reuse an existing action plan.
4. Keep multi-action turns, directional trap behavior, and other new gameplay semantics out of the agent API until Core supports canonical engine concepts for them.

## Current priority: behavior-primitive action-plan authoring

The next in-process API work should remodel canonical action-plan authoring around behavior primitives before the API is stabilized or exposed through an external transport. The engine can keep low-level checks/effects as internal, legacy, or advanced machinery, but normal content authoring should expose plans as primitive behaviors with typed configuration.

### Phase 1: Define the behavior primitive model

Status: Satisfied by `../Archived/Behavior-Primitive-Action-Plans.md`; keep open only for small clarifications discovered during implementation.

Testable outcomes:

- A checked-in document defines behavior primitives, required state, initializable state, implicit state writes, and followup ports.
- The capability manual identifies behavior-primitive authoring as canonical and low-level step/check/effect authoring as advanced or compatibility-oriented.
- `Wandering` is specified as the first primitive: requires `Facing`, attempts one move, sets `Target` to the blocker for followup, reverses `Facing` for next turn when blocked, and resolves to one action.
- `SeekTarget`, `PickupTarget`, and `BumpTarget` are specified as first-pass primitives, with generic followup behavior and default required state.

### Phase 2: Add Core/content descriptor support

Testable outcomes:

- Tests can materialize or interpret primitive descriptors.
- An unblocked `Wandering` actor moves in its current `Facing` direction.
- A blocked `Wandering` actor updates canonical `Target` to the blocker and reverses canonical `Facing` for the next turn.
- A blocked `Wandering` plan calls its configured followup and still resolves to exactly one consumed action.
- A `SeekTarget` actor moves toward `Target` using counter-clockwise tie-breaking and calls followup or waits when movement fails.
- `PickupTarget` reproduces the pickup portion of current `handleBlocker` behavior.
- `BumpTarget` reproduces the current `handleBlocker` fallback behavior.
- Existing low-level descriptor/YAML compatibility tests continue to pass.

### Phase 3: Add validation and editor/content parity

Testable outcomes:

- Validation reports missing required state for entities assigned primitive-backed plans.
- Validation reports missing or malformed followup references.
- Canonical validation passes for valid `Wandering` content.
- Canonical validation continues to reject arbitrary variable mutation and legacy `SetVariable` authoring.

### Phase 4: Adjust the agent/editor API around behavior primitives

Testable outcomes:

- The agent API can create/configure a `Wandering` primitive plan without manual `CanMove`, `Move`, `BlockingEntity`, `ReverseDirection`, or `CallPlan` authoring.
- The agent API can assign the primitive plan to an entity and set initial `Facing`.
- The agent API can configure the `Wandering` followup port.
- The agent API can define targeting rules on an entity and set behavior-step `targetSlot` for target-consuming behavior.
- Unsupported arbitrary internal state mutation is rejected or clearly marked non-canonical.
- Generated primitive-backed content validates with zero canonical diagnostics.

### Phase 5: Re-run generated-content exercises and revise roadmap

Testable outcomes:

- Barrel authoring still succeeds as a passive container.
- Rat authoring succeeds as a `Wandering` entity with initial `Facing` and configured followup.
- Rat two-actions-per-turn remains classified as scheduler/speed engine work unless a speed capability has been added.
- Trap all-direction bumping remains classified as new behavior/action primitive or scheduler work unless such a capability has been added.
- Roadmap and capability docs are updated with the exercise results.
