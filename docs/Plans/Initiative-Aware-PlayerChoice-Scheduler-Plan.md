---
id: plan.initiative-aware-player-choice-scheduler
title: Initiative-Aware PlayerChoice Scheduler Refactor Plan
kind: plan
status: draft
truth_rank: 55
truth_domains: [planning-priority, implementation-navigation]
owners: [core-owner]
audience: [core-owner, frontend-owner, content-editor]
read_when:
  - replacing special first-player turn handling
  - implementing multiple player-controlled actors in play mode
  - changing SimulationHistorySession controlled actor semantics
related:
  - source.invariants
  - source.engine-editor-capabilities
  - source.frontend-ux-invariants
  - plan.instance-controller-playable-starts-sprint
  - plan.canonical-actions-vertical-slice
---
# Initiative-Aware PlayerChoice Scheduler Refactor Plan

Status: Draft follow-up plan. This plan records the larger refactor revealed by instance-level `controller: Player` authoring: current play mode still treats one selected player entity as special and first in the turn cycle, rather than advancing initiative and pausing whenever the next actor's runtime control source is `PlayerChoice`.

## Problem statement

Current SadConsole play mode effectively runs:

1. ask the focused `session.PlayerEntityId` for input;
2. submit that action through history;
3. run all non-controlled action plans as automatic actors;
4. ask the same focused entity again.

That violates the intended Core model:

- `PlayerChoice` is mutable runtime actor state, not a permanent special player entity role.
- Any actor may be `Automatic` or `PlayerChoice`.
- Initiative / actor order should determine when an actor is considered.
- When a `PlayerChoice` actor is reached, the engine/shared scheduler should produce an Action Choice request and pause for frontend input.
- `Scenario.PlayerEntityId` remains observer/default focus metadata and must not define turn authority.

The instance-controller sprint exposed the issue because multiple authored `controller: Player` entities are now initialized correctly and excluded from automatic action plans, but only the focused one can be prompted by SadConsole. Secondary controlled actors become idle instead of receiving input when their initiative position is reached.

## Target behavior

Introduce a shared initiative-aware stepping model:

```text
advance to next actor in deterministic initiative order

if actor control source is Automatic:
  resolve actor's authored/effective action plan
  record outcome
  continue stepping

if actor control source is PlayerChoice:
  produce Core ActionChoiceRequest for that actor
  pause and return the request to frontend/session consumer

when frontend submits a choice:
  execute the selected authored/canonical action for that actor
  record outcome/history
  continue stepping until the next PlayerChoice request or selected boundary
```

This should support:

- player-controlled actors in arbitrary initiative positions;
- multiple player-controlled actors in one scenario;
- no player-controlled actors, advancing automatic turns without input;
- runtime control-source changes in future work;
- rollback/history restore of current turn position and pending request state;
- frontends presenting the active controlled actor as a scheduler result, not as a hardcoded player entity.

## Non-goals

- Do not redesign the full global time/speed system.
- Do not implement party AI, queued team commands, or simultaneous command phases.
- Do not add runtime possession/control-transfer effects in this slice.
- Do not require final polished UX for selecting among controlled actors; the active actor should come from scheduler state.
- Do not remove legacy direct controlled-command compatibility until Action Choice covers the necessary frontend paths.

## Phase 0: Baseline trace and failing tests

Goal: prove the existing invariant violation before changing scheduler/history code.

TDD / invariant trace:

- Affected invariants:
  - Runtime control source is mutable Core actor state and `PlayerChoice` actors produce Core-owned Action Choice requests instead of fallback policy resolution.
  - Scenario runs use shared Content/Core services and schedule contained actors deterministically for scenario-root inventory spaces.
  - Simulation history snapshots preserve restorable world state, action state, visible trace/turn-report context, and rollback policy.
  - Playable frontend sessions launch through shared Content materialization outputs rather than hardcoded prototype player or plane IDs.
- Existing tests to review/preserve:
  - `ActionChoiceRequestRequiresPlayerChoiceControlSource`
  - `ScenarioMaterializerAllowsMultiplePlacedPlayerControllerEntities`
  - `GameplaySessionControllerExcludesSecondaryPlayerControlsFromAutonomousPlans`
  - `SubmitSuccessfulControlledCommandCreatesIntervalAndNextFrame`
  - `SubmitFailedControlledCommandAddsCurrentFrameLogWithoutAdvancingFrameOrTurn`
  - `RollbackRestoresFrameSnapshotAndVisibleTraceContext`
  - `ScenarioRunServiceRunsPersistedScenarioByIdWithInsertedPlayer`
  - `ScenarioRunServiceRunsRootInventoryActorsInInitiativeOrder`
- New intentionally failing tests before implementation:
  - `InitiativeStepperStopsAtFirstPlayerChoiceActorInOrder`
  - `InitiativeStepperRunsAutomaticActorsBeforeLaterPlayerChoiceActor`
  - `InitiativeStepperPromptsMultiplePlayerChoiceActorsInOrder`
  - `InitiativeStepperAdvancesPlayerlessTurnsWithoutPrompt`
  - `SimulationHistoryRecordsPendingPlayerChoiceActorAcrossFrames`
  - `RollbackRestoresPendingPlayerChoiceActor`
  - `GameplaySessionControllerPromptsSecondaryPlayerControlWhenItsTurnArrives`

Exit criteria:

- Failing tests demonstrate that current play mode prompts the focused player first and cannot prompt secondary controlled actors by initiative.

## Phase 1: Core/shared initiative stepper

Goal: add a shared scheduler/stepper service that owns initiative-aware control-source interpretation.

Planned work:

1. Define a turn-step result DTO, likely including:
   - automatic actor outcomes since the previous pause;
   - current actor ID;
   - optional `ActionChoiceRequest` when paused for `PlayerChoice`;
   - turn number / initiative index or equivalent deterministic cursor;
   - diagnostics/runtime failures.
2. Define deterministic actor ordering for the initial slice by reusing current scenario-root inventory row-major ordering unless a more specific existing initiative service already owns it.
3. Step automatic actors through existing `ActorTurnResolver` / authored action-plan semantics.
4. For `PlayerChoice` actors, call `ActionChoiceService` using the actor's effective/default authored plan and return without resolving fallback.
5. Define behavior when a `PlayerChoice` actor has no available supported choices: return structured diagnostics/request failure rather than silently becoming automatic.

Testable outcomes:

- Automatic actors before a player-choice actor act before the prompt.
- Player-choice actors after automatic actors do not act early.
- Multiple player-choice actors are prompted in deterministic order.
- Playerless stepping can advance through automatic actors.

## Phase 2: History/session integration

Goal: make history record and restore scheduler cursor plus pending active actor state.

Planned work:

1. Replace or supplement the single permanent `ControlledEntityId` frame field with scheduler state:
   - active/pending actor ID when paused;
   - initiative cursor;
   - current turn/frame boundary facts.
2. Add history submission APIs that submit a choice for the currently pending request actor.
3. Ensure submissions cannot target a stale/non-pending actor.
4. Record automatic intervals produced while stepping to the next prompt.
5. Preserve failed submission behavior: failed choices log without advancing scheduler/frame unexpectedly.
6. Restore pending actor/request state on rollback.

Testable outcomes:

- Successful player-choice submission advances to the next prompt or turn boundary.
- Failed submission stays on the same pending actor.
- Rollback restores the previous pending actor and visible log state.

## Phase 3: SadConsole play-mode consumption

Goal: make the frontend consume active actor facts from shared scheduler/history state.

Planned work:

1. Refactor `GameplaySessionController` so direct commands and Action Choice submissions target the current pending actor, not `session.PlayerEntityId`.
2. Treat `session.PlayerEntityId` as initial focus/observer fallback only.
3. Update HUD/current-place/entity panels to render the active pending actor's POV when a prompt exists.
4. Keep a clear display distinction between:
   - active actor needing input;
   - inspected entity;
   - original scenario observer/focus.
5. Remove the stopgap that excludes all `PlayerControls` from automatic plans in a frontend-owned dictionary once shared scheduler owns that policy.
6. Preserve playerless scenario behavior: a manual advance key should step automatic actors even when no prompt exists.

Testable outcomes:

- SadConsole prompts the second/third authored `controller: Player` entities when their initiative turns arrive.
- A player-controlled actor later in row-major order does not act before earlier automatic actors.
- No-player scenarios still advance.

## Phase 4: Content/tooling/report alignment

Goal: make headless reports and agent/tool consumers reflect the same scheduler semantics.

Planned work:

1. Update scenario run reports to identify pauses/prompts for `PlayerChoice` actors or to document when headless runs auto-skip prompts for report-only simulation.
2. Update materialization/run/preview reports to distinguish initial control-source setup from active scheduler state.
3. Update player narrative log projection to use scheduler/history rows once automatic and player-choice intervals share one history shape.
4. Update docs and invariant traces with final test names.

## Open design questions

Resolve before implementation:

1. What is the exact scheduler cursor model: row-major actor list snapshot per turn, dynamic lookup each step, or future initiative service abstraction?
2. Should submitting a player choice advance immediately to the next `PlayerChoice` actor, or stop after one actor for frontend animation/review?
3. How should direct movement hotkeys coexist with Action Choice requests when the pending actor has no Move choice?
4. What should headless persisted scenario runs do when they encounter `PlayerChoice` actors and no submitted choices are available?
5. What frontend wording should distinguish active controlled actor from observer/current inspected entity?

## Suggested verification

Run targeted tests first, then relevant broader suites:

```powershell
dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "InitiativeStepper|SimulationHistory|ActionChoice|ScenarioMaterializer"
dotnet test tests/GameGameGame.SadConsole.Tests/GameGameGame.SadConsole.Tests.csproj
dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName!~ScenarioRecordingTests"
```

Use `--artifacts-path C:\Users\Scramble\AppData\Local\Temp\opencode\...` when local frontend/tool processes lock default build outputs.
