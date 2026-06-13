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

## Actions And Turns

- An entity is an actor only if it has a decidable action plan or decision trigger.
- Actions must produce structured traces for failed checks and resolutions.
- Ranked action plans must distinguish failure that continues to the next action from failure that consumes the turn.
- Spatial recursion may exist, but temporal recursion must be explicitly guarded.

## Action Plan Data

- Action plan variables are typed, persist in context, and can be written by checks before later reads.
- Action plan descriptors preserve structured built-in inputs and materialize executable plans.
- The primitive catalog describes every exposed check, effect, value kind, and field contract.

## Content Pipeline

- YAML content loads from strings and files into registries that can be validated.
- Editable content documents round-trip through materialization and saved YAML.
- Content editor operations preserve declared IDs, presentations, carried layouts, action plans, and validation results.
- Built-in content must load and validate, but tests should not pin valid design choices such as balance values, glyphs, positions, or action plan behavior.

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
- Actor scheduling: `TurnServiceOnlySchedulesEntitiesWithActionPlans`, `InterpretedEntityActionPlanCanBeScheduledByTurnService`.
- Structured traces: `ActionPlanContextVariableUpdatesAreTraced`, `CallPlanEffectRunsNestedPlanWithSharedContextAndTrace`, `PickupFailsWhenTargetTotalWeightWouldExceedCapacity`.
- Ranked action plan failure/turn behavior: `PlanInterpreterUsesFirstSuccessfulConsumingRankedStep`, `PlanInterpreterReturnsFailureWhenNoStepConsumesOrStops`, `BuiltInCanMoveCheckFailureFallsThroughToSetVariableEffect`.
- Temporal recursion guard: `CallPlanEffectFailsWithTraceWhenDepthGuardIsExceeded`.
- Typed action plan variables and check writes: `ActionPlanContextStoresTypedVariables`, `PlanVariableRefReadsTypedVariableFromContext`, `PlanInterpreterCommitsCheckVariableWritesBeforeEffect`, `PrototypeRegistryValidationAcceptsVariablesWrittenByChecksBeforeLaterReads`.
- Descriptor materialization and structured inputs: `ActionPlanDescriptorKeepsBuiltInInputsAsData`, `ActionPlanDescriptorMaterializesExecutableBuiltIns`, `BuiltInPlanPartsExposeStructuredInputs`.
- Primitive catalog completeness and field contracts: `PlanPrimitiveCatalogExposesAllCheckEffectAndValueKinds`, `PlanPrimitiveCatalogDescribesCheckFieldsAndVariableContracts`, `PlanPrimitiveCatalogDescribesEffectFieldsAndReferences`.
- YAML loading: `YamlContentLoaderCreatesRegistryFromDeclarativeContent`, `YamlContentLoaderCanLoadRegistryFromFile`.
- Editable document roundtrip: `EditableContentDocumentCanLoadMaterializeSaveAndReloadYaml`.
- Content editor operations: `ContentEditorServiceUpdatesEntityPresetAndPresentation`, `ContentEditorServicePlacesAndMovesCarriedEntityInInventoryLayout`, `ContentEditorServiceAddsReordersAndRemovesActionPlanSteps`, `ContentEditorServiceSetsActionPlanStepChecksAndEffects`, `ContentEditorServiceEditsTemplateDefaultPlanVariables`, `ContentEditorServiceValidatesCurrentDocumentAfterEdits`, `ContentEditorServiceValidationReportsCurrentDocumentErrors`.
- Built-in content validation: `PrototypeRegistryValidationPassesForBuiltInContent`.
