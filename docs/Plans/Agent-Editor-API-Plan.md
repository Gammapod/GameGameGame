# Agent Editor API Plan

## Goal

Provide a stable, constrained API that agents can use to author content through the same engine/editor capability model used by the GUI.

The API should not expose legacy arbitrary-variable authoring. It should operate on canonical engine concepts:

- entity templates and presentations,
- actor action-state defaults,
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
- GUI can exercise common movement primitive authoring paths.
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
   - defer initial target until a concrete content use case requires it.

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
- Prefer typed IDs and descriptors over raw strings where possible.
- Every mutating command should return validation status or enough context for the caller to validate immediately after.
- Command failures should be structured and actionable, not only exception text.

## First implementation slice

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

After the in-process API is stable, expose it through whatever agent transport/protocol is chosen.
