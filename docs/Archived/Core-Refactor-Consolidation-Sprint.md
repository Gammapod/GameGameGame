# Core Refactor / Consolidation Sprint Plan

Status: Completed / archived sprint record. This behavior-preserving refactor sprint split oversized action-plan tests, decomposed `EditableContentDocument`, and extracted `ActionPlanInterpreter` responsibilities behind the existing facade. Retained for historical implementation context; active priorities remain in `docs/Plans/` and source-of-truth docs.

Read when:

- investigating the completed Core/Content refactor/consolidation sprint;
- understanding why action-plan tests, `EditableContentDocument`, or `ActionPlanInterpreter` are split across the current files;
- looking for historical validation and friction notes from the consolidation sprint.

Related source of truth:

- `docs/Source of Truth/invariants.md` records the stable behavior contracts and test traces that this sprint must preserve.
- `docs/Source of Truth/testing-charter.md` records the TDD/test-trace workflow.
- `docs/Source of Truth/Engine-Editor-Capabilities.md` records capability support tiers and layer coverage; this sprint should not change support status unless an accidental gap is discovered and explicitly promoted.
- `docs/Source of Truth/Content-Authoring-Manual.md` records content-facing authoring limits; this sprint should not change authoring semantics.

## Sprint target

Reduce the highest-friction Core/Content maintenance hotspots while preserving engine/editor behavior:

1. Split the oversized `CoreActionPlanTests` fixture into focused suites so action-plan invariant failures are easier to localize.
2. Decompose `EditableContentDocument` so the YAML document DTO, serialization, canonicalization, validation, and mapping responsibilities are easier to maintain without changing the YAML schema or editor/content API behavior.
3. Extract `ActionPlanInterpreter` internals behind the existing interpreter facade so canonical behavior-chain execution, primitive compatibility execution, legacy low-level execution, and shared helpers are separated without changing plan semantics or trace contracts.

This is a behavior-preserving refactor sprint. Do not modify existing game content YAML under `src/GameGameGame.Content/**.yaml`.

## Non-goals

- Do not add, remove, rename, promote, or demote any engine/editor capability.
- Do not change canonical Action Step semantics, turn consumption, target writes, inventory transfer rules, Bulk/Aperture rules, control-source behavior, or trace shape intentionally.
- Do not change the authored YAML schema or saved YAML defaults/null handling intentionally.
- Do not replace `ActionPlanInterpreter` as the public facade during this sprint.
- Do not rename invariant-traced tests unless `docs/Source of Truth/invariants.md` is updated in the same change.

## TDD policy for this sprint

Most phases are refactors, not semantic changes. The required testable outcome is therefore preservation of existing invariant-traced tests plus any new characterization tests needed before extracting a seam.

Before each implementation phase:

1. Confirm the phase's testable outcomes below.
2. Review the listed invariant trace.
3. Run or isolate the listed existing tests to establish the current green characterization baseline.
4. If the extraction exposes an untested behavior dependency, add or revise a characterization test before production changes. For behavior-preserving characterization tests, the expected result may already pass before the refactor; intentionally failing tests are required only if a phase is changed into a semantic behavior change.
5. Make the smallest extraction/move needed.
6. Run targeted tests, then the relevant broader suite.

If implementation discovers a desired semantic change, pause this plan and create/update a behavior plan with intentionally failing tests before changing production behavior.

## Phase 1: Split `CoreActionPlanTests` into focused suites

### Phase notes / friction log

- Started: established a green baseline for the existing `CoreActionPlanTests` with `dotnet test --no-build --filter "FullyQualifiedName~CoreActionPlanTests"`.
- Friction: the first normal build/test attempt was blocked by a running `GameGameGame.Content.Tools` process locking `GameGameGame.Core.dll` and `GameGameGame.Content.dll`; the process had to be stopped before compiling newly split test files.
- Incremental progress: moved the action-plan context/action-state tests into `ActionPlanContextTests` and moved shared helper methods out of the main test file while preserving test method names.
- Incremental progress: moved descriptor/catalog tests into `ActionPlanDescriptorAndCatalogTests`, plan-override Action Step tests into `PlanOverrideActionStepTests`, legacy metadata/turn-facing catalog tests into `ActionStepCatalogCompatibilityTests`, primitive-backed compatibility tests into `PrimitiveActionPlanInterpreterTests`, and legacy built-in descriptor/direct effect tests into `LegacyPlanBuiltInDescriptorTests`.
- Validation checkpoint: after each split, the aggregate filter covering the new suites plus remaining `CoreActionPlanTests` passed with the same total test count.
- Current shape: `CoreActionPlanTests.cs` is reduced from roughly 2,900 lines to roughly 1,400 lines. Remaining work starts with legacy low-level interpreter/check/effect tests, then canonical behavior-chain, movement, targeting, transfer, utility Action Step, and interpreted-turn-service tests.
- Incremental progress: moved low-level interpreter tests into `LegacyLowLevelActionPlanInterpreterTests`, legacy built-in check/effect execution tests into `LegacyPlanBuiltInExecutionTests`, and generic canonical behavior-chain tests into `CanonicalBehaviorChainTests`.
- Validation checkpoint: the expanded aggregate filter covering all split action-plan suites plus remaining `CoreActionPlanTests` passed with 156 tests.
- Incremental progress: moved Backstep/canonical Move tests into `CanonicalMovementActionStepTests` and target acquisition / target-slot / target-label tests into `TargetingActionStepTests`.
- Validation checkpoint: the expanded aggregate filter covering all split action-plan suites plus remaining `CoreActionPlanTests` continued to pass with 156 tests.
- Completed: all remaining duplicate/reference coverage was either moved into focused suites or quarantined in `PrototypeActionStepReferenceTests`; the leftover duplicate `CoreActionPlanTests` fixture and helper were removed during sprint wrap-up.
- Final Phase 1 validation after duplicate cleanup: targeted action-plan/action-step filter passed with 198 tests.

### Current hotspot

`tests/GameGameGame.Tests/CoreActionPlanTests.cs` is approximately 2,900 lines and contains roughly 121 tests covering many unrelated invariant areas. The class is valuable as characterization coverage but too broad for fast navigation and failure triage.

### Implementation approach

- Move tests into focused classes while preserving test method names wherever possible.
- Introduce shared helpers only for repeated world/plan setup that is already shared in the existing file.
- Keep the `[Trait(TestSuites.TraitName, TestSuites.Core)]` classification on all new suites.
- Avoid assertion rewrites except where needed to compile after helper extraction.

Candidate target suites:

- `ActionPlanContextTests`
- `ActionPlanDescriptorTests`
- `ActionPlanPrimitiveCatalogTests`
- `LegacyLowLevelActionPlanInterpreterTests`
- `PrimitiveActionPlanInterpreterTests`
- `CanonicalBehaviorChainTests`
- `CanonicalMovementActionStepTests`
- `TargetingActionStepTests`
- `InventoryTransferActionStepTests`
- `PlanOverrideActionStepTests`
- shared helper file such as `ActionPlanTestWorld` or `ActionPlanTestHelpers`

### Testable outcomes

- All tests currently in `CoreActionPlanTests` still exist in focused Core test suites.
- Existing test method names traced by `invariants.md` are preserved, or `invariants.md` is updated in the same change.
- `dotnet test` targeted to `GameGameGame.Tests` Core/action-plan tests passes.
- No production code is changed in this phase unless required for test helper visibility, and no behavior semantics change.

### TDD / invariant trace

Affected invariants from `docs/Source of Truth/invariants.md`: behavior-preserving refactor; the following invariant groups are protected by the tests being moved rather than changed.

- Actions and turns:
  - actor scheduling / action-plan resolution contracts where covered by action-plan tests;
  - fallback/terminal plan failure and turn behavior;
  - temporal recursion guard;
  - canonical behavior-chain execution and traces;
  - canonical Action Step state contracts and deterministic movement/targeting/transfer.
- Action Plan Data:
  - typed entity action state and legacy variables;
  - descriptor materialization and structured inputs;
  - primitive catalog completeness and field contracts;
  - action-plan shape classification.
- Inventory, Bulk, and Aperture where action-plan tests cover Give/Take/Enter/Exit transfer behavior.

Existing tests to preserve/move include, but are not limited to:

- `ActionPlanShapeClassifierIdentifiesPlanShape`
- `ActionPlanContextStoresTypedVariables`
- `ActionPlanContextVariableUpdatesAreTraced`
- `ActionPlanContextCanonicalSlotReadsTraceMissingAndWrongKind`
- `CanonicalFacingPersistsOnActorActionStateAcrossPlanExecutions`
- `CanonicalTargetPersistsOnActorActionStateWhenBlockingEntityIsFound`
- `PlanPrimitiveCatalogExposesAllCheckEffectAndValueKinds`
- `PlanPrimitiveCatalogDescribesCheckFieldsAndVariableContracts`
- `PlanPrimitiveCatalogDescribesEffectFieldsAndReferences`
- `ActionStepCatalogExposesAllCanonicalActionStepKinds`
- `ApplyPrePlanBehaviorStepInstallsReferencedPlanOnTargetPreSlot`
- `ApplyPlanBehaviorStepInstallsReferencedPlanOnTargetSlot`
- `PrimitiveBackedPlanWithoutFallbackTerminatesRootTurnWhenPrimitiveFails`
- `PrimitiveBackedPlanUsesExplicitFallbackWhenPrimitiveFails`
- `PrimitiveFallbackCyclesUsePlanCallDepthGuard`
- `PlanInterpreterUsesFirstSuccessfulConsumingRankedStep`
- `PlanInterpreterCommitsCheckVariableWritesBeforeEffect`
- `PlanInterpreterReturnsFailureWhenNoStepConsumesOrStops`
- `CallPlanEffectRunsNestedPlanWithSharedContextAndTrace`
- `CallPlanEffectFailsWithTraceWhenDepthGuardIsExceeded`
- `BehaviorChainRunsMoveFacingThenPickupTargetWithoutLinkedFallbackPlan`
- `BehaviorChainStopsAfterFirstSuccessfulActionStep`
- `BehaviorChainTraceFormatterSummarizesFallbackStateAndTerminalOutcome`
- `CanonicalMoveRelativeBackSetsFacingToActualMovedDirection`
- `CanonicalMoveBlockedByEntityDoesNotWriteTarget`
- `CanonicalMoveDiagonalAllowsOneBlockedCorner`
- `CanonicalMoveDiagonalRejectsTwoBlockedCorners`
- `AcquireNearestTargetSelectsNearestSamePlaneTargetAndWritesTarget`
- `TargetConsumingBehaviorCanReadTargetLabelFromExecutingActor`
- `SeekTargetAdjacentFallsThroughAndPreservesTargetForDestroyTarget`
- `FleeTargetMovesAwayFromTargetAndPreservesTarget`
- `MaintainChebyshevDistanceTwoBacksAwayWhenTooCloseAndPreservesTarget`
- `StrafeClockwiseMovesPerpendicularToSeekPrimaryAndPreservesTarget`
- `GiveTargetTransfersFirstCarriedEntityToTargetInventoryRowMajor`
- `TakeTargetTransfersFirstTargetInventoryEntityToActorInventoryRowMajor`
- `EnterTargetMovesActorIntoAdjacentTargetInventoryRowMajor`
- `ExitFacingMovesActorOutOfContainingInventoryToAdjacentContainerCell`

Existing tests to revise into intentionally failing tests: None expected; this is a test organization refactor. If behavior changes are introduced, stop and revise the plan.

New tests needed before production changes: None expected. Add characterization tests only if an untested helper seam must be extracted.

## Phase 2: Decompose `EditableContentDocument`

### Current hotspot

`src/GameGameGame.Content/EditableContentDocument.cs` is approximately 970 lines. It combines the root YAML DTO with serialization, canonicalization, canonical authoring validation, scenario/player-control validation, ID generation, editing helpers, and nested DTO/domain mapping types.

### Implementation approach

Preserve the public document API and YAML schema while moving responsibilities out of the main file.

Recommended extraction order:

1. Move nested DTO types and DTO/domain conversion members into one or more partial files, for example `EditableContentDocument.Dtos.cs`.
2. Extract load/save/serialize implementation behind the existing `EditableContentDocument.LoadYaml` and `SaveYaml` methods.
3. Extract legacy canonicalization to a dedicated helper, keeping saved YAML output identical.
4. Extract canonical authoring validation to a dedicated validator.
5. Extract scenario and player-control validation to a dedicated validator.
6. Keep simple editing APIs such as `AddEntityTemplate`, `UpsertScenario`, and `GetScenario` available from `EditableContentDocument` unless a later plan selects a broader editor document model change.

### Testable outcomes

- `EditableContentDocument.LoadYaml`, `SaveYaml`, `ToRegistry`, `ValidateCanonicalAuthoring`, `AddEntityTemplate`, `UpsertScenario`, and `GetScenario` remain source-compatible.
- Existing inline YAML fixtures round-trip with equivalent saved YAML.
- Built-in content still loads and validates.
- Editor service and agent API tests that consume `EditableContentDocument` still pass.
- No authored content YAML files are modified.

### TDD / invariant trace

Affected invariants from `docs/Source of Truth/invariants.md`:

- Content Pipeline:
  - YAML content loads from strings and files into registries that can be validated.
  - Editable content documents round-trip through materialization and saved YAML.
  - Content editor operations preserve declared IDs, presentations, carried layouts, Action Plans/behavior assignments, legacy action plans, and validation results.
  - Built-in content must load and validate without pinning transient design choices.
  - Persisted scenario definitions materialize through the shared content materialization path and report authoring diagnostics before simulation.

Existing tests to preserve/run include:

- `YamlContentLoaderCreatesRegistryFromDeclarativeContent`
- `YamlContentLoaderCanLoadRegistryFromFile`
- `EditableContentDocumentCanLoadMaterializeSaveAndReloadYaml`
- `EditableContentDocumentCanCreateEntityTemplateWithGeneratedStableId`
- `ContentEditorServiceUpdatesEntityPresetAndPresentation`
- `ContentEditorServicePlacesAndMovesCarriedEntityInInventoryLayout`
- `ContentEditorServiceAddsReordersAndRemovesActionPlanSteps`
- `ContentEditorServiceSetsActionPlanStepChecksAndEffects`
- `ContentEditorServiceEditsTemplateDefaultPlanVariables`
- `ContentEditorServiceValidatesCurrentDocumentAfterEdits`
- `ContentEditorServiceValidationReportsCurrentDocumentErrors`
- `ContentEditorAuthorsCanonicalEnterExitBehaviorChain`
- `SnapshotProjectsDefaultActionPlanTargetLabelRequirementsAndOrphanedRules`
- `SnapshotIncludesActionPlanStepTargetReferencesAndTargetConsumptionMetadata`
- `SetActionPlanStepTargetLabelUpdatesAndClearsTargetLabelRequirements`
- `PrototypeRegistryValidationPassesForBuiltInContent`
- `ScenarioDefinitionsRoundTripAuthoredPlayerControlBindings`
- `ScenarioMaterializerReportsAuthoringDiagnostics`
- `ScenarioMaterializerPersistsAndMaterializesAlphaScenarioDefinitionById`
- `ScenarioMaterializerResolvesAuthoredPlayerControlBindings`
- `ScenarioValidationReportsMissingControlledEntityReferences`
- `ScenarioValidationReportsInvalidPlayerControlBindingShapes`

Existing tests to revise into intentionally failing tests: None expected for behavior-preserving decomposition. If extraction changes the YAML schema, default handling, or validation semantics intentionally, add/revise tests first and update this plan.

New tests needed before production changes: None expected. Add focused characterization tests first if a canonicalization or scenario-validation path is discovered without direct coverage.

## Phase 3: Extract `ActionPlanInterpreter` internals behind the existing facade

### Phase notes / friction log

- Started from a green targeted baseline: `dotnet test "tests\GameGameGame.Tests\GameGameGame.Tests.csproj" --filter "FullyQualifiedName~ActionPlan|FullyQualifiedName~ActionStep|FullyQualifiedName~InventoryTransfer|FullyQualifiedName~PrototypeActionStepReference"` passed with 193 tests.
- Incremental progress: moved shared direction, distance, seek-ordering, transfer-target, first-carried-selection, inventory-transfer, and placeholder-ID helpers into `ActionPlanInterpreter.Helpers.cs` without changing method bodies or public facade APIs.
- Incremental progress: moved the private legacy step execution loop, `CallPlanEffect` handling, and check/write evaluation helpers into `ActionPlanInterpreter.LegacyExecution.cs` without changing method bodies or public facade APIs.
- Incremental progress: moved one-turn behavior plan override application into `ActionPlanInterpreter.PlanOverrides.cs` without changing method bodies or public facade APIs.
- Broader validation checkpoint: full `dotnet test "tests\GameGameGame.Tests\GameGameGame.Tests.csproj"` passed with 524 tests after the first Phase 3 extraction checkpoints.
- Incremental progress: restored the intended Phase 2 `EditableContentDocument.cs` shape after the next extraction pass exposed duplicated main-file methods; the file is again a partial containing root document dictionaries plus DTO/schema/mapping types only.
- Incremental progress: moved canonical movement and movement-adjacent primitive handlers into `ActionPlanInterpreter.MovementHandlers.cs`.
- Incremental progress: moved target acquisition/seek/flee/distance-maintenance/strafe handlers into `ActionPlanInterpreter.TargetingHandlers.cs`.
- Incremental progress: moved pickup/drop/give/take/enter/exit and target destruction handlers into `ActionPlanInterpreter.InventoryHandlers.cs`.
- Incremental progress: moved primitive kind dispatch from `PrimitiveActionPlanExecutor.cs` into `PrimitiveActionPlanDispatcher.cs`, leaving primitive root/fallback execution in the executor.
- Incremental progress: moved behavior-chain Action Step dispatch from `BehaviorChainExecutor.cs` into `BehaviorStepDispatcher.cs`, leaving ordered chain execution in the executor.
- Validation checkpoint: targeted action-plan/action-step filter passed with 228 tests after handler and dispatcher extraction.
- Broader validation checkpoint: full `dotnet test "tests\GameGameGame.Tests\GameGameGame.Tests.csproj"` passed with 559 tests after handler and dispatcher extraction.
- Wrap-up cleanup: removed the duplicate leftover `CoreActionPlanTests` fixture after confirming its tests existed in focused suites.
- Final validation checkpoint after duplicate cleanup: targeted action-plan/action-step filter passed with 198 tests.
- Final broader validation checkpoint after sprint wrap-up: full `dotnet test "tests\GameGameGame.Tests\GameGameGame.Tests.csproj"` passed with 529 tests.
- Friction: the first handler extraction attempt was too large and did not apply cleanly; the workaround was to rewrite the main facade/DTO host files to their intended compact shapes and then add handler clusters as focused partial files.

### Current hotspot

`ActionPlanInterpreter` is currently a partial class split across files, but the primary file still owns most interpreter responsibilities. It coordinates root plan dispatch, legacy low-level plans, primitive compatibility, canonical behavior-chain Action Steps, movement/targeting helpers, inventory transfer helpers, plan overrides, trace shaping, and call-plan recursion.

### Implementation approach

Keep `ActionPlanInterpreter` as the public facade and preserve constructor/`Execute(...)` source compatibility. Extract internals in small, test-backed steps.

Recommended extraction order:

1. Extract primitive compatibility execution and primitive handlers from the main interpreter file, expanding the existing `PrimitiveActionPlanExecutor.cs` seam or introducing an internal executor.
2. Extract canonical behavior-chain Action Step dispatch/handlers from `BehaviorChainExecutor.cs` into focused handler/executor classes while preserving trace labels/status/detail.
3. Extract legacy low-level check/effect execution and call-plan recursion into a legacy executor, preserving call-depth guard semantics.
4. Extract shared helper utilities only after their call sites are stable: direction resolution, relative movement, seek ordering, distance helpers, transfer target reads, first-carried selection, and placeholder ID generation.
5. After each extraction, run the focused test subset for the moved behavior before continuing.

### Testable outcomes

- Public construction and execution of `ActionPlanInterpreter` remain source-compatible.
- All existing action-plan, movement, targeting, transfer, trace, and turn-consumption tests pass.
- Trace labels, statuses, reasons, details, and child ordering remain stable unless explicitly covered by a separate semantic change plan.
- No editor/content capability or authoring support status changes.

### TDD / invariant trace

Affected invariants from `docs/Source of Truth/invariants.md`:

- Actions and Turns:
  - Actions must produce structured traces for failed checks and resolutions.
  - Action Plan resolution must distinguish explicit fallback failure from terminal root-turn resolution.
  - Spatial recursion may exist, but temporal recursion must be explicitly guarded.
  - Canonical behavior chains execute ordered Action Steps in one Action Plan without linked fallback plans.
  - Canonical behavior chains continue after failed/non-consuming steps and stop after the first successful turn-consuming step.
  - Runtime Action Plan overrides compose as one-turn pre/main/post slots and clear after turn resolution.
  - Apply-plan Action Steps install referenced plans into the target entity's one-turn override slots and consume the actor's turn on success.
  - Canonical behavior-chain traces must report attempted steps, state reads/writes, fallback continuation/stopping, and terminal turn outcome.
  - Shared actor plan resolution must preserve canonical trace wrapping, consuming-step stop behavior, terminal non-consuming stop behavior, and all-continue terminal failure behavior.
  - Canonical Action Steps must preserve state contracts for `Facing`, `Target`, movement, target selection, inventory transfer, fallthrough, and deterministic tie-breaks.
- Inventory, Bulk, and Aperture:
  - constrained inventory transitions respect Bulk/Aperture rules;
  - peer inventory transfer uses deterministic row-major selection;
  - Enter/Exit enforce the documented inventory-owner aperture crossings.
- Action Plan Data:
  - typed entity action state and legacy variables persist correctly;
  - descriptors preserve structured built-in inputs and materialize executable plans;
  - the Action Step/primitive catalog field contracts remain accurate;
  - an Action Plan descriptor has exactly one active authored shape.

Existing tests to preserve/run include the Phase 1 moved suites plus broader consumers:

- `PrimitiveBackedPlanWithoutFallbackTerminatesRootTurnWhenPrimitiveFails`
- `PrimitiveBackedPlanUsesExplicitFallbackWhenPrimitiveFails`
- `PrimitiveFallbackCyclesUsePlanCallDepthGuard`
- `PlanInterpreterUsesFirstSuccessfulConsumingRankedStep`
- `PlanInterpreterCommitsCheckVariableWritesBeforeEffect`
- `PlanInterpreterReturnsFailureWhenNoStepConsumesOrStops`
- `BuiltInCanMoveCheckFailureFallsThroughToSetVariableEffect`
- `CallPlanEffectRunsNestedPlanWithSharedContextAndTrace`
- `CallPlanEffectFailsWithTraceWhenDepthGuardIsExceeded`
- `BehaviorChainRunsMoveFacingThenPickupTargetWithoutLinkedFallbackPlan`
- `BehaviorChainStopsAfterFirstSuccessfulActionStep`
- `BehaviorChainTraceFormatterSummarizesFallbackStateAndTerminalOutcome`
- `ResolvePlanReportsConsumingSuccessWithCanonicalTraceShape`
- `ResolvePlanContinuesAfterFallthroughAndStopsAtTerminalFailure`
- `ResolvePlanReportsFailureWhenNoStepConsumesOrStops`
- `CanonicalMoveRelativeBackSetsFacingToActualMovedDirection`
- `CanonicalMoveBlockedByEntityDoesNotWriteTarget`
- `CanonicalMoveDiagonalAllowsOneBlockedCorner`
- `CanonicalMoveDiagonalRejectsTwoBlockedCorners`
- `AcquireNearestTargetSelectsNearestSamePlaneTargetAndWritesTarget`
- `AcquireNearestTargetFallsThroughWithoutOverwritingWhenNoCandidateExists`
- `SeekTargetAdjacentFallsThroughAndPreservesTargetForDestroyTarget`
- `SeekTargetBlockedByIncidentalEntityPreservesGoalTarget`
- `FleeTargetMovesAwayFromTargetAndPreservesTarget`
- `FleeTargetFallsThroughWhenNoValidIncreasingMoveExists`
- `MaintainChebyshevDistanceTwoBacksAwayWhenTooCloseAndPreservesTarget`
- `MaintainChebyshevDistanceTwoFallsThroughAtExactDistance`
- `StrafeClockwiseMovesPerpendicularToSeekPrimaryAndPreservesTarget`
- `StrafeAnticlockwiseMovesOppositePerpendicularAndPreservesTarget`
- `StrafeClockwiseUsesSeekTargetPrimaryTieBreakOnDiagonal`
- `GiveTargetTransfersFirstCarriedEntityToTargetInventoryRowMajor`
- `TakeTargetTransfersFirstTargetInventoryEntityToActorInventoryRowMajor`
- `EnterTargetMovesActorIntoAdjacentTargetInventoryRowMajor`
- `ExitFacingMovesActorOutOfContainingInventoryToAdjacentContainerCell`
- `GiveTargetFailureFallsThroughWithoutConsumingStepTurn`
- `TakeTargetFailureFallsThroughWhenTargetInventoryIsEmpty`
- `ApplyPrePlanBehaviorStepInstallsReferencedPlanOnTargetPreSlot`
- `ApplyPlanBehaviorStepInstallsReferencedPlanOnTargetSlot`
- `ApplyPrePlanBehaviorStepFailsWhenReferencedPlanIsMissing`
- `TurnServiceOnlySchedulesEntitiesWithActionPlans`
- `InterpretedEntityActionPlanCanBeScheduledByTurnService`
- scenario-report tests that run interpreted plans, including `ScenarioRunServiceShowsBehaviorStepsAndTreatsNoActionAsObservation` and `ScenarioRunServiceReportsMultiTurnMoveFacingScenario`.

Existing tests to revise into intentionally failing tests: None expected for behavior-preserving extraction. If trace shape or turn policy is intentionally changed, revise the relevant traced tests first and update `invariants.md`.

New tests needed before production changes: Add characterization tests first only if an extracted helper's externally visible behavior is not already protected by the listed tests.

## Validation commands

Use targeted commands as phases progress, then a broader test pass before completing the sprint.

Recommended targeted commands:

```powershell
dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~ActionPlan"
dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~EditableContentDocument|FullyQualifiedName~ContentEditorService|FullyQualifiedName~ScenarioMaterializer|FullyQualifiedName~PrototypeRegistryValidationPassesForBuiltInContent"
```

Recommended broader validation before sprint close:

```powershell
dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj
```

If implementation touches additional test projects, run those relevant suites too.

## Sprint completion checklist

- `CoreActionPlanTests` has been split into focused suites with invariant-traced test names preserved or `invariants.md` updated.
- `EditableContentDocument` responsibilities are decomposed without changing YAML schema, roundtrip behavior, validation semantics, or editor/content API behavior.
- `ActionPlanInterpreter` internals are separated behind the existing facade without changing action-plan semantics or traces.
- No existing content YAML files under `src/GameGameGame.Content/**.yaml` were modified.
- Targeted and broad tests pass.
- Source-of-truth docs are updated only if behavior, support tier, authoring capability, or invariant test names changed.
