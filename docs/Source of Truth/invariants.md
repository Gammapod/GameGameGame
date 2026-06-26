# GameGameGame Invariants

This document records minimal functional requirements that should influence tests. Keep this list small and stable.

## Entity And Space

- Every meaningful game object is an entity with a stable ID.
- Entity locations are represented by occupancy of nodes in planes.
- At most one entity may occupy a node at a time.
- Traversals through containment or inventory relationships must be cycle-safe.

## Inventory And Weight

- An entity has no usable inventory space if its inventory width or height is `0`.
- Missing or zero weight contributes `0` weight.
- An entity's own weight does not count against its own carrying capacity.
- Recursive carried weight includes all entities inside the entity's inventory space, plus anything those entities recursively carry.
- Pickup must fail if `current carried weight + target total weight > carrying capacity`.
- Peer inventory transfer must use deterministic row-major source and destination selection and must respect existing inventory capacity/space rules.

## Actions And Turns

- An entity is an actor only if it has a decidable Action Plan/fallback chain or decision trigger.
- Actions must produce structured traces for failed checks and resolutions.
- Action Plan resolution must distinguish failure that follows an explicit fallback from terminal resolution that ends the current root actor's turn.
- Spatial recursion may exist, but temporal recursion must be explicitly guarded.
- Canonical behavior chains execute ordered Action Steps in one Action Plan without requiring linked fallback plans.
- Canonical behavior chains continue after a failed/non-consuming step and stop after the first successful turn-consuming step.
- Canonical behavior-chain traces must report attempted steps, state reads/writes, fallback continuation/stopping, and terminal turn outcome.
- Canonical Action Steps must preserve their documented state contracts for `Facing`, `Target`, movement, target selection, inventory transfer, fallthrough, and deterministic tie-breaks.

## Action Plan Data

- Entity action state such as `Facing` and `Target` is typed and persists on the actor entity across plan executions.
- Legacy action plan variables are typed, persist in invocation context, and can be written by checks before later reads while compatibility support remains.
- Canonical behavior descriptors and legacy action-plan descriptors preserve structured built-in inputs and materialize executable plans.
- The Action Step/primitive catalog describes every exposed primitive, value kind, implied state contract, and field contract.
- An Action Plan descriptor has exactly one active authored shape for canonical behavior, transitional primitive, legacy low-level steps, or empty/passive state; mixed authored shapes are invalid.
- Empty canonical behavior chains are invalid for authored content and resolve as no turn if encountered at runtime.

## Content Pipeline

- YAML content loads from strings and files into registries that can be validated.
- Editable content documents round-trip through materialization and saved YAML.
- Content editor operations preserve declared IDs, presentations, carried layouts, Action Plans/behavior assignments, legacy action plans, and validation results.
- Built-in content must load and validate, but tests should not pin valid design choices such as balance values, glyphs, positions, or action plan behavior.
- Persisted scenario definitions materialize through the shared content materialization path, reference normal content templates, insert the selected player at the requested start, and report authoring diagnostics before simulation.
- Console scenario launch consumes scenario materialization outputs rather than hardcoded prototype player or plane IDs.
- Root-only scenario materialization remains compatibility behavior while supported and must be distinguishable from persisted scenario/player materialization.

## Scenario Tooling

- Headless scenario runs use shared Content/Core services and schedule contained actors deterministically for scenario-root inventory spaces.
- Scenario reports treat expected in-simulation inability to act as runtime observation, not as an engine/runtime failure.
- Scenario run reports expose setup, actor order, turn traces, final state, cycle-safe inventory/containment summaries, validation diagnostics, runtime observations, runtime failures, and capability gaps; combined persisted-scenario review reports compose validation, action-plan previews, materialization, and scenario run reports without changing simulation semantics.
- Scenario recording captures the initial state plus one frame after each full simulated turn, writes PNG frames and a GIF for valid recordings, and produces diagnostics without artifacts for invalid authoring requests.

## Test Coverage Map

- Stable entity IDs: `EditableContentDocumentCanCreateEntityTemplateWithGeneratedStableId`, `ContentEditorServiceListsJoinedEntityPresets`.
- Entity locations are node occupancy: `EntityLocationsAreRepresentedByNodeOccupancy`.
- One entity per node: `MovementCannotPlaceEntityOnOccupiedNode`, `PrototypeRegistryValidationReportsOverlappingCarriedEntities`.
- Cycle-safe traversal: `TraversingRecursiveInventoryWeightIsCycleSafe`.
- Zero inventory dimensions are unusable: `ZeroInventoryDimensionMakesInventoryUnusable`, `PrototypeRegistryValidationReportsCarriedEntitiesOnTemplateWithoutUsableInventory`.
- Missing inventory space contributes no carried weight: `MissingInventoryPlaneContributesNoCarriedWeight`.
- Own weight does not count against own capacity: `OwnWeightDoesNotCountAgainstOwnCarryingCapacity`.
- Recursive carried weight: `RecursiveCarriedWeightIncludesNestedInventoryContents`.
- Pickup capacity failure: `PickupFailsWhenTargetTotalWeightWouldExceedCapacity`.
- Peer inventory transfer source/destination/capacity behavior: `GiveTargetTransfersFirstCarriedEntityToTargetInventoryRowMajor`, `TakeTargetTransfersFirstTargetInventoryEntityToActorInventoryRowMajor`, `GiveTargetCanTransferPlayerEntityWhenInventoryRulesAllowIt`.
- Actor scheduling: `TurnServiceOnlySchedulesEntitiesWithActionPlans`, `InterpretedEntityActionPlanCanBeScheduledByTurnService`.
- Structured traces: `ActionPlanContextVariableUpdatesAreTraced`, `CallPlanEffectRunsNestedPlanWithSharedContextAndTrace`, `PickupFailsWhenTargetTotalWeightWouldExceedCapacity`.
- Fallback/terminal action plan failure and turn behavior: `PrimitiveBackedPlanWithoutFallbackTerminatesRootTurnWhenPrimitiveFails`, `PrimitiveBackedPlanUsesExplicitFallbackWhenPrimitiveFails`, `PlanInterpreterUsesFirstSuccessfulConsumingRankedStep`, `PlanInterpreterReturnsFailureWhenNoStepConsumesOrStops`, `BuiltInCanMoveCheckFailureFallsThroughToSetVariableEffect`.
- Temporal recursion guard: `CallPlanEffectFailsWithTraceWhenDepthGuardIsExceeded`, `PrimitiveFallbackCyclesUsePlanCallDepthGuard`.
- Canonical behavior-chain execution and traces: `BehaviorChainRunsMoveFacingThenPickupTargetWithoutLinkedFallbackPlan`, `BehaviorChainStopsAfterFirstSuccessfulActionStep`, `BehaviorChainTraceFormatterSummarizesFallbackStateAndTerminalOutcome`.
- Canonical Action Step state contracts and deterministic movement/targeting/transfer: `BackstepMovesOppositeFacingConsumesTurnAndPreservesFacing`, `BackstepBlockedByEntityWritesTargetAndFallsThrough`, `BackstepOutOfBoundsFailsWithoutMeaningfulTargetWrite`, `AcquireNearestTargetSelectsNearestSamePlaneTargetAndWritesTarget`, `AcquireNearestTargetFallsThroughWithoutOverwritingWhenNoCandidateExists`, `AcquireNearestTargetContinuesToSeekTargetInSameTurn`, `SeekTargetAdjacentFallsThroughAndPreservesTargetForDestroyTarget`, `SeekTargetBlockedByIncidentalEntityPreservesGoalTarget`, `FleeTargetMovesAwayFromTargetAndPreservesTarget`, `FleeTargetFallsThroughWhenNoValidIncreasingMoveExists`, `MaintainChebyshevDistanceTwoBacksAwayWhenTooCloseAndPreservesTarget`, `MaintainChebyshevDistanceTwoFallsThroughAtExactDistance`, `StrafeClockwiseMovesPerpendicularToSeekPrimaryAndPreservesTarget`, `StrafeAnticlockwiseMovesOppositePerpendicularAndPreservesTarget`, `StrafeClockwiseUsesSeekTargetPrimaryTieBreakOnDiagonal`, `GiveTargetTransfersFirstCarriedEntityToTargetInventoryRowMajor`, `TakeTargetTransfersFirstTargetInventoryEntityToActorInventoryRowMajor`, `GiveTargetFailureFallsThroughWithoutConsumingStepTurn`, `TakeTargetFailureFallsThroughWhenTargetInventoryIsEmpty`, `GiveTargetCanTransferPlayerEntityWhenInventoryRulesAllowIt`.
- Typed entity action state and legacy action plan variables/check writes: `CanonicalFacingPersistsOnActorActionStateAcrossPlanExecutions`, `CanonicalTargetPersistsOnActorActionStateWhenBlockingEntityIsFound`, `SpawnedActionPlanUsesCanonicalInitialFacingDefault`, `ActionPlanContextStoresTypedVariables`, `PlanVariableRefReadsTypedVariableFromContext`, `PlanInterpreterCommitsCheckVariableWritesBeforeEffect`, `PrototypeRegistryValidationAcceptsVariablesWrittenByChecksBeforeLaterReads`.
- Descriptor materialization and structured inputs: `ActionPlanDescriptorKeepsBuiltInInputsAsData`, `ActionPlanDescriptorMaterializesExecutableBuiltIns`, `BuiltInPlanPartsExposeStructuredInputs`.
- Action Plan shape classification and invalid authored shapes: `ActionPlanShapeClassifierIdentifiesPlanShape`, `ContentEditorServiceSetsCanonicalBehaviorChainAndClearsLegacyPlanShapes`, `PrototypeRegistryValidationReportsMixedActionPlanShapes`, `PrototypeRegistryValidationReportsEmptyBehaviorChain`, `EditorViewModelSelectingBehaviorPlanShowsCanonicalActionSteps`, `EditorViewModelShowsLegacyCompatibilityOnlyForLegacyLowLevelPlans`.
- Primitive catalog completeness and field contracts: `PlanPrimitiveCatalogExposesAllCheckEffectAndValueKinds`, `PlanPrimitiveCatalogDescribesCheckFieldsAndVariableContracts`, `PlanPrimitiveCatalogDescribesEffectFieldsAndReferences`.
- YAML loading: `YamlContentLoaderCreatesRegistryFromDeclarativeContent`, `YamlContentLoaderCanLoadRegistryFromFile`.
- Editable document roundtrip: `EditableContentDocumentCanLoadMaterializeSaveAndReloadYaml`.
- Content editor operations: `ContentEditorServiceUpdatesEntityPresetAndPresentation`, `ContentEditorServicePlacesAndMovesCarriedEntityInInventoryLayout`, `ContentEditorServiceAddsReordersAndRemovesActionPlanSteps`, `ContentEditorServiceSetsActionPlanStepChecksAndEffects`, `ContentEditorServiceEditsTemplateDefaultPlanVariables`, `ContentEditorServiceValidatesCurrentDocumentAfterEdits`, `ContentEditorServiceValidationReportsCurrentDocumentErrors`.
- Built-in content validation: `PrototypeRegistryValidationPassesForBuiltInContent`.
- Scenario materialization and Console launch: `ScenarioMaterializerMaterializesAlphaScenarioWithPlayerInsertion`, `ScenarioMaterializerReportsAuthoringDiagnostics`, `ScenarioMaterializerPersistsAndMaterializesAlphaScenarioDefinitionById`, `ScenarioMaterializerValidatesPersistedAlphaScenarioDefinitions`, `AlphaScenarioFixtureLoadsValidatesAndMaterializesPlayer`, `AlphaScenarioFixtureCanLaunchInConsoleAndAcceptPlayerMove`, `ConsoleScenarioLauncherBuildsPlayableSessionFromPersistedScenario`, `ScenarioMaterializerSupportsRootOnlyScenarioCompatibility`.
- Headless scenario run/report behavior: `ScenarioRunServiceRunsRootInventoryActorsInInitiativeOrder`, `ScenarioRunServiceLabelsRootOnlyCompatibilityRuns`, `ScenarioRunServiceRunsPersistedScenarioByIdWithInsertedPlayer`, `ScenarioRunServiceSummarizesCarriedInventoryContents`, `ScenarioInventorySummaryFormatterIsCycleSafe`, `ScenarioRunServiceShowsBehaviorStepsAndTreatsNoActionAsObservation`, `ScenarioRunServiceReportsMultiTurnMoveFacingScenario`, `ScenarioRunnerReportsUnsupportedCapabilityGap`, `PassiveChestTransferShowcaseValidatesMaterializesAndRuns`, `AgentContentEditorApiRunsPersistedScenarioById`, `AgentContentEditorApiCreatesCombinedPersistedScenarioReport`.
- Scenario recording behavior: `ScenarioRecordingServiceRecordsPersistedScenarioInitialStateAndFullTurns`, `ScenarioRecordingServiceReportsAuthoringDiagnosticsWithoutArtifacts`.
