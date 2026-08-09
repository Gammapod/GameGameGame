# SadConsole Frontend Refactor / Consolidation Sprint Plan

Status: Archived/completed sprint record. This plan records the focused technical-debt sprint for `src/GameGameGame.SadConsole`; it does not replace the canonical-action release plan or the broader SadConsole frontend roadmap.

Read when:

- working on SadConsole frontend refactors, especially mutation handling, play-mode componentization, or legacy shell quarantine;
- deciding whether frontend code may call Core directly;
- reviewing TDD traces for frontend-owned refactor work.

Related source of truth:

- `docs/Source of Truth/Frontend-UX-Invariants.md` records frontend/shared-service boundaries and test traces.
- `docs/Source of Truth/Frontend-UX-Standards.md` records the accepted Editor/Simulation mode model and UI principles.
- `docs/Plans/SadConsole-Frontend-Roadmap.md` records the broader SadConsole/debug-browser backlog.
- `docs/Archived/Canonical-Actions-Vertical-Slice-Plan.md` preserves canonical-action release-direction history.

## Sprint target

Reduce SadConsole frontend coordination debt without changing gameplay semantics or authoring semantics.

By the end of this sprint:

1. the legacy `SadConsoleShell` is quarantined as reference-only/deprecated code and is no longer a target for new editor/play-mode behavior;
2. service-backed editor mutations use a shared frontend execution/session abstraction for status, snapshot replacement, and exception handling;
3. componentized play mode has a clearer split between runtime command submission, frontend prompt state, and frame/component building;
4. direct Core references are documented as allowed only for approved frontend-facing contracts/value DTOs, while direct semantic execution from SadConsole is identified or removed;
5. frontend tests continue to prove presentation/session behavior without duplicating Core or Content rules.

## Non-goals

- Do not revive the removed Console frontend or Console-specific workflows.
- Do not rewrite Core, Content, Editor, Headless, or materialization services as part of this sprint unless a blocker is explicitly re-scoped.
- Do not make SadConsole own mutation legality, action legality, turn advancement policy, materialization rules, provenance rules, or structured log facts.
- Do not fully redesign gameplay UX, mouse UX, rendering, glyph standards, or editor workflows. Keep user-visible behavior equivalent except where quarantine/removal is explicit.
- Do not force SadConsole to reference only Content. The sprint goal is an explicit Core-reference policy, not an artificial facade.

## Working rules

1. Prefer small, test-backed extractions over broad rewrites.
2. Keep existing componentized editor/play-mode tests passing after each phase.
3. Add characterization tests before moving behavior from oversized coordinators.
4. If a refactor reveals missing shared semantics, stop and create a Core/Content gap instead of filling it in SadConsole.
5. Run targeted frontend tests after each phase, plus the SadConsole test suite before sprint completion.

Suggested verification command:

```powershell
dotnet test tests/GameGameGame.SadConsole.Tests/GameGameGame.SadConsole.Tests.csproj
```

If normal build outputs are locked by a running SadConsole instance, use a temporary output/base-intermediate path under `C:\Users\Scramble\AppData\Local\Temp\opencode`.

## Phase 0: Quarantine legacy `SadConsoleShell`

Goal: make the legacy shell's status mechanically and socially obvious so new work does not continue accumulating there.

Scope:

1. Move or namespace-mark `SadConsoleShell` and its legacy-only helpers as deprecated/reference-only without changing the default componentized launch path.
2. Ensure startup paths prefer componentized scenario selection/editor/play mock surfaces.
3. Add a short code comment and/or test-visible marker that `SadConsoleShell` is not an active implementation surface.
4. Inventory any tests that still depend on legacy shell behavior; preserve only characterization tests needed to keep the app compiling until deletion.

Testable outcomes:

- The SadConsole project still builds.
- Default launch and tested scenario-selection/editor flows do not require `SadConsoleShell`.
- No new tests are added against legacy shell internals except quarantine/build characterization.
- Existing componentized screen tests still pass.

TDD / invariants trace:

- Frontend invariant 2, **Frontend state is presentation state**: quarantine must not move layout/focus/prompt state into shared services.
- Frontend invariant 6, **Scenario launch is frontend-neutral**: startup must still consume shared catalog/session services rather than legacy shell-specific launch semantics.
- Frontend invariant 7, **Editor-like workflows consume editor/content APIs**: editor flows must remain backed by `FrontendEditorService`/shared Content editor services.
- Suggested tests: `SadConsoleScenarioSelectionScreenTests`, `SadConsoleScenarioEditScreenTests`, `SadConsoleEntityTemplateEditScreenTests`, `SadConsoleActionPlanEditScreenTests`, and build/compile coverage for `GameGameGame.SadConsole`.

Exit criteria:

- Legacy shell is clearly quarantined and no longer listed as a destination for new work.
- Any remaining dependency on it is documented as deletion follow-up or temporary compatibility.

## Phase 1: Shared editor mutation executor/session

Goal: consolidate repeated service-backed editor mutation handling while preserving shared Content editor service ownership of mutation semantics.

Scope:

1. Introduce a frontend-owned helper such as `EditorMutationExecutor`, `EditorScreenSession`, or similarly named abstraction.
2. Centralize:
   - service-present guard messages;
   - invoking `FrontendEditorService` mutation delegates;
   - unexpected exception-to-UI-status conversion;
   - `FrontendEditorMutationResult` snapshot replacement handoff;
   - success/failure status message propagation.
3. First migrate low-risk componentized screens before touching `SadConsoleEditorContext`:
   - `InventoryGridEditScreen`;
   - `ActionPlanEditScreen`;
   - then selected `EntityTemplateEditScreen` mutation paths if the helper shape holds.
4. Keep workflow-specific selection repair and overlay cleanup in the screen/controller unless a clear shared hook emerges.

Testable outcomes:

- Existing action-plan, entity-template, inventory-grid, and scenario-edit tests pass unchanged or with only test-name updates for refactored internals.
- At least one new focused test proves an exception thrown during a frontend-invoked mutation is converted into a user-facing failed/stay result without crashing the screen model.
- At least one new focused test proves successful mutation still replaces the active snapshot and preserves/clamps screen selection as before.
- No test asserts mutation legality in SadConsole; legality remains covered by Content/editor service tests.

TDD / invariants trace:

- Frontend invariant 7, **Editor-like workflows consume editor/content APIs**: executor invokes shared editor services; it does not mutate YAML or content models directly.
- Frontend invariant 13, **Editor dirty state and preview-stale state have one user-facing recovery action**: mutations must continue to mark dirty/stale through the existing service/snapshot model and not introduce a second stale state.
- Frontend invariant 11, **Typing is an explicit submode**: mutation execution must not blur text-entry confirmation/cancel behavior.
- Existing shared traces from `Frontend-UX-Invariants.md`: `ContentEditorServiceUpdatesEntityPresetAndPresentation`, `ContentEditorServicePlacesAndMovesCarriedEntityInInventoryLayout`, `ContentEditorServiceAddsReordersAndRemovesActionPlanSteps`, `ContentEditorServiceValidatesCurrentDocumentAfterEdits`, `FrontendEditorServiceTests`, `SadConsoleScenarioEditScreenTests`.
- Suggested SadConsole tests: `SadConsoleActionPlanEditScreenTests`, `SadConsoleEntityTemplateEditScreenTests`, `SadConsoleScenarioEditScreenTests`, plus new executor unit tests.

Exit criteria:

- A reusable mutation executor/session abstraction exists and is consumed by at least two componentized editor screens.
- Broad catch blocks are not multiplied across mutation paths; unexpected mutation failures have one frontend handling policy.

## Phase 2: Componentized play-mode session controller

Goal: separate runtime command/session submission from `GameplayMockScreen` frame building and expose any remaining direct semantic execution as explicit debt.

Scope:

1. Extract a `GameplaySessionController` or equivalent from `GameplayMockScreen` to own:
   - `PlayableScenarioSession` reference;
   - `ControlledActorCommandService` / `ActionChoiceService` / `SimulationHistorySession` wiring;
   - command/action-choice submission;
   - history and `ActionLogProjection` refresh;
   - display-target refresh calls that are already shared-service-backed.
2. Keep `GameplayMockScreen` responsible for frontend state and frame composition:
   - inspected entity;
   - selected action/prompt indexes until Phase 3;
   - HUD/component rows;
   - layout bounds and valid-highlight presentation.
3. Isolate or remove direct SadConsole use of lower-level semantic execution APIs such as direct `ActionPlanInterpreter`, direct `World.AdvanceTurn`, direct `World.RecordTrace`, and `PostActionStateUpdater` calls.
4. If no shared command/session API exists for a path, keep the behavior behind an explicitly named temporary compatibility method and record a Core-owner follow-up rather than expanding it.

Testable outcomes:

- Existing `GameplayMockScreenTests` pass after extraction.
- New controller tests prove move/wait/pickup/drop submissions still advance or fail through shared Core services exactly as before from the frontend's point of view.
- A grep/code review can identify zero or explicitly quarantined direct semantic execution calls in SadConsole play-mode code.
- `GameplayMockScreen.BuildFrame` no longer directly performs runtime action submission.

TDD / invariants trace:

- Frontend invariant 1, **Frontends do not invent simulation semantics**: controller must submit through Core-owned services and not define legality/failure/turn policy.
- Frontend invariant 3, **Player action should converge on shared action contracts**: play-mode submission should prefer Action Choice paths where available and isolate direct-command compatibility.
- Frontend invariant 4, **Action evaluation remains authoritative**: highlights/prompts remain hints; submitted actions resolve through shared services.
- Frontend invariant 5, **Logs derive from structured outcomes**: log refresh uses `ActionLogProjection`/history, not parsed status text.
- Existing shared traces: `ControlledActorCommandMoveReturnsStructuredSuccessAndAdvancesTurn`, `ControlledActorCommandFailedMoveRecordsFailureWithoutAdvancingTurn`, `ControlledActorCommandPickupReportsTargetAndDestinationAnchors`, `ControlledActorAffordanceQueryReportsPickupSourcesAndDestinations`, Action Choice request/submission tests for Move/Pickup/Drop, and `ActionOutcomeProjection...` tests listed in `Frontend-UX-Invariants.md`.
- Suggested SadConsole tests: `GameplayMockScreenTests`, plus new `GameplaySessionControllerTests`.

Exit criteria:

- Runtime submission/history/log refresh is factored out of `GameplayMockScreen`.
- Any remaining direct Core semantic execution in SadConsole is either removed or documented as temporary compatibility requiring Core-owner follow-up.

## Phase 3: Action Choice prompt controller

Goal: isolate frontend-owned prompt-stack behavior from runtime submission and frame rendering.

Scope:

1. Extract an `ActionChoicePromptController` or equivalent from `GameplayMockScreen` to own:
   - closed/action-list/pickup-target/pickup-destination/drop-source/drop-destination state;
   - selected action, target, and destination indexes;
   - Select and Cancel stack transitions;
   - valid target/destination cycling over Core-provided choice facts;
   - status-message generation for prompt navigation.
2. Keep action legality and final submission outside the prompt controller. It may choose among existing `ActionChoice` DTOs; it must not recompute legality.
3. Preserve the current action-step-first UX model from `Frontend-UX-Standards.md`.

Testable outcomes:

- Existing `GameplayMockScreenTests` for action selector behavior pass.
- New prompt-controller tests cover:
  - Enter opens action list from closed state;
  - selecting Pickup opens target then destination prompts;
  - selecting Drop opens source then destination prompts;
  - Cancel returns one prompt layer at a time;
  - empty valid-target lists produce an explanatory status instead of entering a dead-end mode.
- No prompt-controller test asserts Core legality; tests use supplied choice DTOs/facts.

TDD / invariants trace:

- Frontend invariant 14, **Play-mode Select/Cancel prompts behave as a stack**: this phase creates the direct test home for prompt-stack behavior.
- Frontend invariant 3, **Player action should converge on shared action contracts**: prompt choices are derived from Core Action Choice facts.
- Frontend invariant 4, **Action evaluation remains authoritative**: prompt controller chooses valid supplied facts but does not resolve actions.
- Frontend standards, **Action highlighting and selection standards**: action-step-first menu, target/source list, and destination list remain the accepted near-term pathway.
- Suggested SadConsole tests: new `ActionChoicePromptControllerTests` and existing `GameplayMockScreenTests`.

Exit criteria:

- Prompt-stack state is no longer interwoven with `GameplayMockScreen` rendering code.
- The prompt controller has focused tests that can support future target-first UX without rewriting gameplay frame rendering.

## Phase 4: Decompose `SadConsoleEditorContext` by submode

Goal: reduce the largest frontend coordinator only after lower-risk componentized mutation/session patterns are proven.

Scope:

1. Add characterization tests for the first selected submode before extraction.
2. Extract one workflow at a time, in this preferred order:
   - command menu;
   - template presentation edit;
   - targeting-rule edit;
   - inventory brush;
   - action-plan step edit;
   - preview/materialization.
3. Keep `SadConsoleEditorContext` as an orchestration façade initially so existing tests and callers remain stable.
4. Reuse the Phase 1 mutation executor where service mutations are involved.

Testable outcomes:

- `SadConsoleEditorContextTests` continue to pass after each extraction.
- Each extracted submode has at least one focused test covering its state transitions independent of full editor context rendering.
- The public behavior of command menu activation, selection preservation, cancel semantics, save/refresh, and preview stale messages remains unchanged unless explicitly documented.

TDD / invariants trace:

- Frontend invariant 2, **Frontend state is presentation state**: extracted controllers own focus/submode/cursor/picker state only.
- Frontend invariant 7, **Editor-like workflows consume editor/content APIs**: extracted controllers invoke shared editor service paths through the mutation executor where applicable.
- Frontend invariant 9, **Menus and ordinary editor controls have a directional primary path**: command menu extraction should preserve Up/Down/Select/Cancel operation.
- Frontend invariant 11, **Typing is an explicit submode**: presentation/label text edits must remain explicit confirm/cancel submodes.
- Frontend invariant 13, **Editor dirty state and preview-stale state have one user-facing recovery action**: preview invalidation must remain consistent after extractions.
- Suggested SadConsole tests: `SadConsoleEditorContextTests`, plus new focused controller tests as each submode is extracted.

Exit criteria:

- At least two `SadConsoleEditorContext` submodes are extracted behind stable tests.
- The remaining context file is smaller and more obviously an orchestration façade rather than the owner of every workflow.

## Phase 5: Core reference policy and cleanup pass

Goal: make SadConsole's Core/Content dependency boundary explicit and actionable.

Scope:

1. Document accepted SadConsole Core-reference categories:
   - value DTOs used by frontend-facing shared contracts, such as `EntityId`, `GridCoord`, `Direction`, `PlaneId`, and `PlaneCoord`;
   - approved Core runtime/query/projection services, such as controlled-command, affordance, Action Choice, history, and log projection services;
   - shared action-step descriptor/enumeration data surfaced by Content/editor DTOs.
2. Document disallowed or temporary-debt categories:
   - direct `WorldState` mutation for gameplay outcomes;
   - direct `ActionPlanInterpreter` execution from SadConsole;
   - direct turn advancement/trace recording from SadConsole;
   - frontend-only wrappers that decide action or authoring legality.
3. Add a frontend UX decision entry if the policy changes durable review guidance.
4. Optionally add a lightweight architecture test or documented grep checklist for disallowed direct semantic calls.

Testable outcomes:

- A policy note exists in the appropriate frontend source-of-truth lane or this plan's completion notes.
- A grep/checklist identifies remaining direct semantic calls and classifies each as removed, allowed, or temporary debt.
- No refactor forces Content to become a pass-through façade solely to hide Core references.

TDD / invariants trace:

- Frontend invariant 1, **Frontends do not invent simulation semantics**: policy blocks direct semantic execution paths.
- Frontend invariant 4, **Action evaluation remains authoritative**: policy allows frontend consumption of authoritative Core services while rejecting duplicate evaluation.
- Frontend invariant 5, **Logs derive from structured outcomes**: policy allows `ActionLogProjection` consumption and blocks parsed-display-string logs.
- Frontend invariant 7, **Editor-like workflows consume editor/content APIs**: policy keeps authored mutations behind Content/editor services.
- Suggested verification: targeted grep for `ActionPlanInterpreter`, `World.AdvanceTurn`, `World.RecordTrace`, `PostActionStateUpdater`, direct YAML writes, and direct mutable content-model edits under `src/GameGameGame.SadConsole`.

Exit criteria:

- Future frontend reviews have a clear rule: SadConsole may reference Core for approved frontend-facing contracts, but must not perform direct semantic execution or authoring mutations.

## Sprint completion checklist

- `SadConsoleShell` is quarantined and no longer a destination for new behavior.
- At least two componentized editor screens consume the shared mutation executor/session helper.
- Componentized play-mode runtime submission is separated from frame rendering.
- Action Choice prompt stack has a focused frontend-owned test home, or this is recorded as the next immediate follow-up if time runs out.
- `SadConsoleEditorContext` has at least one or two extracted submodes, or extraction is deferred with characterization tests and a clear next target.
- Core-reference policy is documented and remaining direct semantic calls are classified.
- Targeted SadConsole tests pass.
- Any discovered missing shared Core/Content capabilities are logged for the appropriate owner instead of being solved in SadConsole.

## Phase completion notes

### Phase 0: Quarantine legacy `SadConsoleShell`

Status: Completed.

Changes:

- Marked `SadConsoleShell` with an `Obsolete` attribute and explicit quarantine comments.
- Added test-/review-visible quarantine constants to `SadConsoleShell`.
- Marked `LegacySimulationConsoleFactory` with an `Obsolete` attribute and a quarantine constant.
- Confirmed the active startup path in `Program.cs` already launches `ScenarioSelectionConsole`, `GameplayMockConsole`, or `ComponentGalleryConsole`; it does not construct `SadConsoleShell`.

Problems/friction encountered:

- `LegacySimulationConsoleFactory` still exists and can construct the shell, but current code search shows no callers. It is retained for compile-safe quarantine rather than deleted in Phase 0.
- No dedicated tests referenced `SadConsoleShell`; Phase 0 therefore relies on build coverage plus componentized screen tests rather than behavior tests for the quarantined shell.
- Full SadConsole test-suite verification is currently blocked by `SadConsoleEditorContextTests.ActionStepEditorDisablesInvalidReplaceOnEmptyPlanWhileInsertWorks`, which fails because the test fixture does not produce `emptyPlan`. This failure appears unrelated to the quarantine change; targeted componentized screen tests pass.

Follow-up:

- Delete `LegacySimulationConsoleFactory` and `SadConsoleShell` after componentized play-mode coverage makes removal safe.

### Phase 1: Shared editor mutation executor/session

Status: Completed for the sprint's initial acceptance target.

Changes:

- Added frontend-owned `EditorMutationExecutor` for service-present guards, service mutation invocation, unexpected exception-to-status conversion, and snapshot replacement handoff.
- Migrated `InventoryGridEditScreen` mutation paths to the executor for place/delete/move/overwrite behavior.
- Migrated `ActionPlanEditScreen` mutation paths to the executor for replace/insert/delete/label/move behavior while preserving action-plan-specific preferred selection repair.
- Added `EditorMutationExecutorTests` covering null service guard handling, unexpected exception reporting, and successful snapshot replacement.

Problems/friction encountered:

- `ActionPlanEditScreen` needs mutation-specific preferred step indexes, so it cannot use a one-size-fits-all snapshot replacement callback as directly as `InventoryGridEditScreen`. The executor still centralizes invocation/error handling there, while the screen keeps its selection-repair hook.
- The pre-existing full-suite failure from Phase 0 remains and prevents a clean whole-project SadConsole test result. Focused executor/action-plan/entity-template/inventory-grid tests pass.

Verification:

- Passed: `dotnet test tests/GameGameGame.SadConsole.Tests/GameGameGame.SadConsole.Tests.csproj --filter "FullyQualifiedName~EditorMutationExecutorTests|FullyQualifiedName~SadConsoleActionPlanEditScreenTests|FullyQualifiedName~SadConsoleEntityTemplateEditScreenTests|FullyQualifiedName~SadConsoleScenarioEditScreenTests"`
- Passed: `dotnet test tests/GameGameGame.SadConsole.Tests/GameGameGame.SadConsole.Tests.csproj --filter "FullyQualifiedName~EditorMutationExecutorTests|FullyQualifiedName~SadConsoleActionPlanEditScreenTests|FullyQualifiedName~SadConsoleEntityTemplateEditScreenTests"`

Follow-up:

- Consider migrating selected `EntityTemplateEditScreen` and `ScenarioEditScreen` mutation paths after the play-mode extraction phases, but avoid turning the executor into an authoring semantics layer.

### Phase 2: Componentized play-mode session controller

Status: Completed.

Changes:

- Added `GameplaySessionController` to own `PlayableScenarioSession`, controlled-command service wiring, Action Choice service wiring, simulation history, runtime submission, display-target refresh, and structured log refresh.
- Updated `GameplayMockScreen` so frame building consumes controller state instead of owning command services/history/log refresh directly.
- Added controller-focused tests for wait submission/log refresh and Core Action Choice movement submission.
- After Core-owner added the narrow Core/session authored-step Action Choice submission path, replaced the remaining direct `ActionPlanInterpreter` compatibility path with `GameplaySessionController.SubmitAuthoredActionStepChoice`.

Problems/friction encountered:

- Initial Phase 2 work found that not all authored action steps had a frontend-consumable Action Choice submission path. Core-owner confirmed this is a Core concern and added a narrow `AuthoredStep` Action Choice / history submission path, which unblocked removal of SadConsole direct interpreter execution.
- Broader action-choice design questions remain for richer typed choice DTOs, target-first menus, full pre/main/post composition, and step-specific parameter selection; those are not needed for this sprint's refactor goal.
- The first controller movement test attempted to move East into an occupied crate and failed through the shared Core path; the test was corrected to move West to prove successful Action Choice submission without changing semantics.
- A pre-existing full-suite `SadConsoleEditorContextTests.ActionStepEditorDisablesInvalidReplaceOnEmptyPlanWhileInsertWorks` failure blocked this phase's first whole-suite verification. Phase 4 later confirmed it was bad test fixture YAML and fixed it.

Verification:

- Passed: `dotnet test tests/GameGameGame.SadConsole.Tests/GameGameGame.SadConsole.Tests.csproj --filter "FullyQualifiedName~GameplayMockScreenTests"`
- Passed: `dotnet test tests/GameGameGame.SadConsole.Tests/GameGameGame.SadConsole.Tests.csproj --filter "FullyQualifiedName~EditorMutationExecutorTests|FullyQualifiedName~SadConsoleActionPlanEditScreenTests|FullyQualifiedName~SadConsoleEntityTemplateEditScreenTests|FullyQualifiedName~SadConsoleScenarioEditScreenTests|FullyQualifiedName~GameplayMockScreenTests"`
- Full suite still blocked: `dotnet test tests/GameGameGame.SadConsole.Tests/GameGameGame.SadConsole.Tests.csproj` currently reports 212 passed / 1 failed, with the same `SadConsoleEditorContextTests.ActionStepEditorDisablesInvalidReplaceOnEmptyPlanWhileInsertWorks` fixture failure noted above.
- Grep result after Core unblock: no `ActionPlanInterpreter`, `PostActionStateUpdater`, `World.RecordTrace`, or `World.AdvanceTurn` calls remain under `src/GameGameGame.SadConsole`.

Follow-up:

- Continue to coordinate with Core-owner for broader Action Choice follow-up when richer typed choice DTOs, target-first menus, full pre/main/post composition, or step-specific parameter selection are promoted.

Core-owner follow-up update (2026-07-17): Core now has a narrow `ActionChoiceService.SubmitAuthoredStepChoice` / `SimulationHistorySession.SubmitAuthoredActionStepChoice` fallback for non-parameterized authored Action Steps, with invariant traces in `CoreActionChoiceTests`. SadConsole now uses that shared Core/session submission method through `GameplaySessionController.SubmitAuthoredActionStepChoice`, and the direct `ActionPlanInterpreter` / `PostActionStateUpdater` / `World.RecordTrace` / `World.AdvanceTurn` calls have been removed from SadConsole. Keep Move/Pickup/Drop on their richer typed Action Choice paths; broader target-first/pre-main-post choice composition remains a separate Core design follow-up.

### Phase 3: Action Choice prompt controller

Status: Completed.

Changes:

- Added `ActionChoicePromptController` to own frontend prompt-stack state and transitions for Closed, Action List, Pickup Target, Pickup Destination, Drop Source, and Drop Destination modes.
- Moved selected action-step index, selected target/source index, selected destination index, selected entity `ActionChoice`, and selected target id out of `GameplayMockScreen`.
- Updated `GameplayMockScreen` to consume prompt-controller state for action selector rendering, HUD/menu rows, valid-selection highlights, selected coordinates, Select/Cancel routing, and runtime submission handoff.
- Added prompt-controller TDD coverage for opening the action list, Pickup target/destination progression, Drop source/destination progression, Cancel stack unwinding, inventory-inspection requests, and empty-target explanation.

Problems/friction encountered:

- The prompt controller still needs caller-provided formatting delegates for entity names and destinations. This keeps presentation wording outside Core but means the controller is not entirely UI-text-free.
- Highlight/selected-coordinate helpers in `GameplayMockScreen` still perform presentation mapping over prompt-controller state. This is acceptable frontend ownership, but it remains a sizeable frame-building responsibility for a future `GameplayFrameBuilder` extraction.
- Tests currently live in `GameplayMockScreenTests` because the existing session fixture is private there. A later cleanup could move shared play-mode fixtures to a reusable test helper and split `ActionChoicePromptControllerTests` into its own file.
- The pre-existing full-suite `SadConsoleEditorContextTests.ActionStepEditorDisablesInvalidReplaceOnEmptyPlanWhileInsertWorks` failure still blocks a clean full SadConsole suite run.

Verification:

- Passed: `dotnet test tests/GameGameGame.SadConsole.Tests/GameGameGame.SadConsole.Tests.csproj --filter "FullyQualifiedName~ActionChoicePromptController|FullyQualifiedName~GameplayMockScreenTests"`
- Passed: `dotnet test tests/GameGameGame.SadConsole.Tests/GameGameGame.SadConsole.Tests.csproj --filter "FullyQualifiedName~EditorMutationExecutorTests|FullyQualifiedName~SadConsoleActionPlanEditScreenTests|FullyQualifiedName~SadConsoleEntityTemplateEditScreenTests|FullyQualifiedName~SadConsoleScenarioEditScreenTests|FullyQualifiedName~GameplayMockScreenTests"`
- Full suite was blocked at this checkpoint by the `emptyPlan` fixture failure noted above; Phase 4 later restored a clean full-suite baseline.

Follow-up:

- Consider extracting `GameplayFrameBuilder` after Phase 4 or during a later play-mode polish pass so `GameplayMockScreen` becomes a thinner coordinator over session, prompt, and frame-building components.

## Mid-sprint shape review

Completed shape versus sprint plan:

- `SadConsoleShell` is quarantined as planned. It remains compile-present but is no longer an active work target.
- Editor mutation execution is centralized enough to prove the pattern in `InventoryGridEditScreen` and `ActionPlanEditScreen`; the executor stayed a UI/session wrapper and did not absorb authoring semantics.
- Runtime play-mode responsibilities are now split across `GameplaySessionController` for shared-service submission/history/log refresh and `ActionChoicePromptController` for frontend-owned prompt stack state.
- After Core-owner's narrow authored-step Action Choice unblock, SadConsole no longer directly invokes `ActionPlanInterpreter`, `PostActionStateUpdater`, `World.RecordTrace`, or `World.AdvanceTurn`.
- `GameplayMockScreen` is smaller in semantic responsibility but still owns frame composition, row formatting, highlighting coordinate projection, and inspection state. That matches the plan after Phase 3; a future `GameplayFrameBuilder` would be the next play-mode consolidation step if we continue in this lane.

Current risks / friction:

- The full SadConsole suite had one pre-existing `SadConsoleEditorContextTests` fixture failure unrelated to the refactor; Phase 4 later fixed it.
- `SadConsoleEditorContext` remains the largest coordinator, though Phase 4 later extracted several low-risk submodes.
- The prompt controller's formatting delegates and test placement are acceptable for this phase but could be polished after the larger coordinator extractions settle.

Recommended next step:

- Proceed to Phase 4 by extracting the lowest-risk `SadConsoleEditorContext` submode first, preferably the command menu, because it is frontend-owned state, has clear directional Select/Cancel behavior, and should not require new Core/Content capability work.

### Phase 4: Decompose `SadConsoleEditorContext` by submode

Status: Completed for the sprint's initial Phase 4 exit target.

Changes:

- Added `SadConsoleEditorCommandMenuController` to own command-menu open/closed state, selected command index, directional selection movement, cancel behavior, and selected-entry handoff.
- Updated `SadConsoleEditorContext` to delegate command-menu state management to the controller while keeping command-entry construction and command execution in the context façade for now.
- Added focused `SadConsoleEditorCommandMenuControllerTests` for blocked open, directional clamping, select/close, and cancel-without-invocation behavior.
- Fixed the bad `emptyPlan` test fixture in `SadConsoleEditorContextTests.ActionStepEditorDisablesInvalidReplaceOnEmptyPlanWhileInsertWorks`; the previous string replacement did not reliably insert the authored plan under `actionPlans`.
- Added `SadConsoleEditorInitialFacingPickerController` to own initial-facing picker active state, option list, selected option index, movement, confirm selection, and clear behavior.
- Updated `SadConsoleEditorContext` to delegate initial-facing picker state/index/option behavior to the new controller while keeping service-backed mutation application in the context façade.
- Added focused `SadConsoleEditorInitialFacingPickerControllerTests` for begin/select-current-facing, movement clamping, confirm-and-clear, and inactive confirm behavior.
- Added `SadConsoleEditorDefaultActionPlanPickerController` to own default-action-plan picker active state, selected option index, movement, confirm selection, and clear behavior.
- Updated `SadConsoleEditorContext` to delegate default-action-plan picker state/index behavior to the new controller while keeping option construction, service-backed mutation, and preview invalidation in the context façade.
- Added focused `SadConsoleEditorDefaultActionPlanPickerControllerTests` for begin/select-current-plan, movement clamping, confirm-and-clear, and inactive confirm behavior.

Problems/friction encountered:

- Command-entry construction still depends on full editor context state (`Section`, selected scenario/template/action plan, preview availability), so it was intentionally left in `SadConsoleEditorContext` rather than forcing a larger extraction.
- Command execution still dispatches into many existing context workflows. Moving that dispatch out now would couple the new controller to too many mutation/preview/template submodes, so the controller currently owns only menu presentation/input state and selected-entry handoff.
- The initial-facing picker was a low-risk second extraction because it owns bounded frontend picker state; service-backed mutation and preview invalidation remain in `SadConsoleEditorContext` to avoid moving authoring semantics into the picker controller.
- The default-action-plan picker was similarly low-risk, but its option construction still depends on the current editor snapshot's authored action plans. That option construction remains in `SadConsoleEditorContext` rather than pushing snapshot knowledge into the picker controller.
- The `emptyPlan` fixture issue was content/test-data friction rather than a production regression. Fixing it restored a clean full SadConsole suite baseline.

Verification:

- Passed: `dotnet test tests/GameGameGame.SadConsole.Tests/GameGameGame.SadConsole.Tests.csproj --filter "FullyQualifiedName~SadConsoleEditorCommandMenuControllerTests|FullyQualifiedName~CommandMenu"`
- Passed: `dotnet test tests/GameGameGame.SadConsole.Tests/GameGameGame.SadConsole.Tests.csproj --filter "FullyQualifiedName~SadConsoleEditorCommandMenuControllerTests|FullyQualifiedName~CommandMenu|FullyQualifiedName~EditorMutationExecutorTests|FullyQualifiedName~SadConsoleActionPlanEditScreenTests|FullyQualifiedName~SadConsoleEntityTemplateEditScreenTests|FullyQualifiedName~SadConsoleScenarioEditScreenTests|FullyQualifiedName~GameplayMockScreenTests"`
- Passed: `dotnet test tests/GameGameGame.SadConsole.Tests/GameGameGame.SadConsole.Tests.csproj --filter "FullyQualifiedName~SadConsoleEditorInitialFacingPickerControllerTests|FullyQualifiedName~InitialFacing"`
- Passed: `dotnet test tests/GameGameGame.SadConsole.Tests/GameGameGame.SadConsole.Tests.csproj --filter "FullyQualifiedName~SadConsoleEditorDefaultActionPlanPickerControllerTests|FullyQualifiedName~DefaultActionPlan"`
- Passed full suite: `dotnet test tests/GameGameGame.SadConsole.Tests/GameGameGame.SadConsole.Tests.csproj` currently reports 229 passed / 0 failed.

Follow-up:

- Continue with additional `SadConsoleEditorContext` submode extraction in a future sprint if needed. The next high-payoff candidate is template presentation edit; targeting-rule edit and inventory brush are valuable but more entangled.

## Sprint wrap-up checkpoint

Outcome summary:

- Legacy `SadConsoleShell` is quarantined and clearly marked as reference-only.
- Editor mutation execution is centralized through `EditorMutationExecutor` in `InventoryGridEditScreen` and `ActionPlanEditScreen`.
- Componentized play-mode runtime submission/history/log refresh moved into `GameplaySessionController`.
- Componentized play-mode prompt-stack state moved into `ActionChoicePromptController`.
- Core-owner unblocked authored-step Action Choice submission; SadConsole no longer directly invokes semantic execution APIs such as `ActionPlanInterpreter`, `PostActionStateUpdater`, `World.RecordTrace`, or `World.AdvanceTurn`.
- `SadConsoleEditorContext` now delegates command-menu state, initial-facing picker state, and default-action-plan picker state to focused controllers.
- The previously failing `emptyPlan` editor-context test fixture was fixed.
- Full SadConsole frontend test suite is green: 229 passed / 0 failed.

Remaining deliberate debt:

- `SadConsoleEditorContext` is still large; template presentation edit, targeting-rule edit, inventory brush, action-plan step edit, and preview/materialization are future extraction candidates.
- `GameplayMockScreen` still owns frame composition, row formatting, highlight-coordinate projection, and inspection presentation; a future `GameplayFrameBuilder` remains a good consolidation candidate.
- `EditorMutationExecutor` adoption is intentionally partial; extending it should remain incremental to avoid creating a vague god-helper.
- `SadConsoleShell` and `LegacySimulationConsoleFactory` remain present but quarantined. Delete them in a future cleanup after confirming no manual/reference path still needs them.

Documentation cleanup status:

- This plan now serves as the sprint record and wrap-up checkpoint.
- `planning-index.md` points to this plan as a focused frontend refactor sprint overlay.
- No frontend UX standards change was needed beyond the already-recorded invariant alignment: the sprint preserved frontend-owned presentation state and shared-service ownership of semantics.

## Rollback / safety plan

- Keep each phase independently mergeable.
- Prefer additive helpers and façade-preserving extractions before deleting old methods.
- If a phase destabilizes tests, revert that phase only; earlier phases should remain useful.
- Do not delete legacy shell code until componentized launch/play paths and tests make the deletion safe.
