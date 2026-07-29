---
id: plan.cost-system-vertical-slice-sprint
title: Cost System Vertical Slice Sprint Plan
kind: plan
status: active
truth_rank: 40
truth_domains: [planning-priority, implementation-navigation, test-trace]
owners: [core-owner]
audience: [core-owner, content-editor, frontend-owner]
read_when:
  - implementing Action Step costs
  - changing action-step execution, inventory-cost consumption, or cost failure/fallthrough semantics
  - authoring or validating content that spends carried entities as action costs
related:
  - source.invariants
  - source.testing-charter
  - source.engine-editor-capabilities
  - source.content-authoring-manual
  - source.action-step-outcome-and-affordance-logic
  - source.vertical-slice-map
---

# Cost System Vertical Slice Sprint Plan

Status: Active focused sprint plan for adding optional entity-inventory costs to canonical Action Steps. This plan records the agreed design direction before implementation; production code changes must follow the TDD workflow in `docs/Source of Truth/testing-charter.md`.

## Goal

Allow authored action-plan behavior-chain steps to declare a cost paid from the actor's inventory. The actor must carry the required cost entities, including entities nested recursively within the actor's inventory contents. If the cost is missing, the step fails and continues with normal canonical fallback/fallthrough. If the cost is present, the action is attempted and the selected cost entities are destroyed after the action succeeds.

Example use cases:

- a crafting class creates a drone at the cost of 3 scrap;
- a wizard casts Teleport or Polymorph at a hefty cost of mana-token entities;
- a caterpillar becomes a cocoon at the cost of 1 leaf.

## Agreed runtime semantics

For any Action Step with associated cost:

1. Check that all required cost entities exist recursively within the actor's inventory.
2. If any cost is missing, the step fails, emits a structured missing-cost trace/log fact, and continues with the next Action Step in fallback order.
3. If all cost is present, perform the underlying Action Step normally.
4. If the underlying Action Step fails/falls through, preserve the cost entities.
5. If the underlying Action Step succeeds and consumes the turn, destroy the selected cost entities recursively.
6. The successful step and cost destruction should be observable as one authoritative action outcome, with no author-visible intermediate state where the action succeeded but cost remains, or cost was removed before a failed action.

Cost matching is Core-owned and uses runtime `Entity.TemplateId`. Content/editor layers validate that referenced template IDs exist and provide authoring support; they do not own runtime legality.

## Scope

### In scope

1. Core runtime support for optional per-Action-Step cost metadata.
2. Recursive actor-inventory cost lookup by runtime `Entity.TemplateId`.
3. Atomic cost consumption through existing recursive entity destruction after successful consuming step execution.
4. Structured missing-cost, selected-cost, and consumed-cost trace/outcome facts sufficient for scenario reports and future frontend logs.
5. Descriptor/YAML load/save/round-trip support for cost metadata.
6. Content validation for unknown cost templates and invalid quantities.
7. Action Step catalog/preview metadata that surfaces cost fields and summaries.
8. Editor service and agent/headless API parity for setting/clearing step costs.
9. A focused content fixture/scenario that exercises sufficient cost, missing cost/fallthrough, and nested cost lookup.
10. Source-of-truth updates after implementation.

### Out of scope / follow-up

- Non-entity numeric resource pools such as `mana: 100`.
- Cost matching by presentation name, glyph, color, or arbitrary predicate.
- Costs paid on attempt regardless of underlying action success.
- Partial payment, debt, refunds, crafting recipes with multiple output quantities, or stochastic cost selection.
- Polished SadConsole cost-editing UX beyond consuming shared editor service facts.
- Direct player Action Choice cost-specific menus unless required to keep existing authored-step submission truthful.

## Proposed authoring model

Minimal YAML shape:

```yaml
steps:
  - kind: CreateEntity
    templateId: Drone
    placement: Facing
    directionMode: Forward
    costs:
      - templateId: Scrap
        quantity: 3
```

Descriptor concept:

```csharp
public sealed record ActionStepCostDescriptor(string TemplateId, int Quantity);
```

Each behavior-chain step descriptor may carry zero or more cost entries. Zero cost preserves existing behavior. The canonical YAML field is plural `costs`, accepted after content-editor review because it matches the Core `Costs` property and existing list-field authoring conventions.

Open implementation detail for the first failing-test pass:

- Decide whether duplicate entries for the same `templateId` are accepted and summed, or rejected by validation. Preferred first-slice behavior: validation rejects duplicate cost template entries to keep previews and editor mutations simple.

## Six-step implementation plan with TDD trace

Implementation must not start until each step has intentionally failing tests or explicitly records why no new test is needed. Each step below includes the invariant/test trace required by `docs/Source of Truth/testing-charter.md`.

### Step 1: Core model and no-cost behavior preservation

Goal: introduce cost metadata without changing existing Action Step behavior when no cost is authored.

Planned work:

- Add Core descriptor/runtime representation for optional step costs.
- Thread the cost metadata through behavior-chain materialization without enforcing payment yet.
- Preserve all existing canonical/prototype Action Step behavior for empty cost lists.

Affected invariants:

- `Canonical behavior descriptors and legacy action-plan descriptors preserve structured built-in inputs and materialize executable plans.`
- `The Action Step/primitive catalog describes every exposed primitive, value kind, implied state contract, and field contract.`
- `Canonical Action Steps must preserve their documented state contracts for Facing, Target, movement, target selection, inventory transfer, fallthrough, and deterministic tie-breaks.`

Existing tests to review/preserve:

- `ActionPlanDescriptorKeepsBuiltInInputsAsData`
- `ActionPlanDescriptorMaterializesExecutableBuiltIns`
- `BuiltInPlanPartsExposeStructuredInputs`
- `PlanPrimitiveCatalogExposesAllCheckEffectAndValueKinds`
- Representative existing behavior-chain tests such as `BehaviorChainStopsAfterFirstSuccessfulActionStep` and `ResolvePlanReportsConsumingSuccessWithCanonicalTraceShape`

New intentionally failing tests before production changes:

- `ActionStepCostDescriptorPreservesNoCostAsEmptyList`
- `BehaviorStepWithoutCostPreservesExistingExecutionBehavior`

Exit criteria:

- Empty/missing `cost` is semantically identical to current behavior.
- Descriptor/materialization tests fail first for missing cost support, then pass after the smallest model change.

### Step 2: Recursive actor-inventory cost lookup and missing-cost fallthrough

Goal: check required cost entities recursively within the actor's inventory before executing the underlying step.

Planned work:

- Add a Core cost lookup/evaluation seam that traverses actor-carried inventory contents cycle-safely.
- Match candidate cost entities by `Entity.TemplateId`.
- Fail the current step, emit structured missing-cost facts, and continue canonical fallback/fallthrough when cost is insufficient.
- Ensure the underlying action is not executed when cost is missing.

Affected invariants:

- `Traversals through containment or inventory relationships must be cycle-safe.`
- `Actions must produce structured traces for failed checks and resolutions.`
- `Canonical behavior chains continue after a failed/non-consuming step and stop after the first successful turn-consuming step.`
- `Canonical behavior-chain traces must report attempted steps, state reads/writes, fallback continuation/stopping, and terminal turn outcome.`

Existing tests to review/preserve:

- `ScenarioInventorySummaryFormatterIsCycleSafe`
- `EntityContainmentPathServiceDetectsContainmentCycle`
- `BehaviorChainRunsMoveFacingThenPickupTargetWithoutLinkedFallbackPlan`
- `ResolvePlanContinuesAfterFallthroughAndStopsAtTerminalFailure`
- `ProjectExtractsFailedContinuedAndSuccessfulStoppedActionStepAttempts`

New intentionally failing tests before production changes:

- `CostedActionStepFallsThroughWhenRequiredTemplateIsMissing`
- `CostedActionStepFallsThroughWhenQuantityIsInsufficient`
- `CostedActionStepFindsCostRecursivelyInActorInventoryContents`
- `MissingCostDoesNotExecuteUnderlyingActionStep`
- `MissingCostTraceReportsTemplateQuantityAndAvailableCount`

Exit criteria:

- Missing or insufficient cost behaves like a normal failed/non-consuming Action Step and can fall through to a later step.
- Recursive inventory traversal is covered and cycle-safe.

### Step 3: Successful action execution and atomic cost destruction

Goal: consume selected cost entities only after the underlying Action Step succeeds and consumes the turn.

Planned work:

- Select deterministic concrete cost entities during preflight.
- Execute the underlying Action Step normally when cost is sufficient.
- After successful turn-consuming execution, destroy the selected cost entities recursively using the Core entity-destruction path.
- Preserve selected costs when the underlying action fails/falls through.

Affected invariants:

- `Every meaningful game object is an entity with a stable ID.`
- `Entity locations are represented by occupancy of nodes in planes.`
- `Traversals through containment or inventory relationships must be cycle-safe.`
- `Canonical behavior chains continue after a failed/non-consuming step and stop after the first successful turn-consuming step.`
- `Structured action-step attempt projections derive ordered attempts from canonical behavior-chain traces while preserving full step traces for debug expansion.`

Existing tests to review/preserve:

- Existing destroy/entity-lifecycle tests from the Create/Destroy/Polymorph slice, especially tests covering recursive destruction and world integrity.
- `BehaviorChainStopsAfterFirstSuccessfulActionStep`
- `ResolvePlanReportsConsumingSuccessWithCanonicalTraceShape`
- `ProjectExtractsFailedContinuedAndSuccessfulStoppedActionStepAttempts`
- `WorldStateClonePreservesMutableSimulationStateWithoutSharingCollections`

New intentionally failing tests before production changes:

- `CostedActionStepConsumesRequiredEntitiesAfterConsumingSuccess`
- `CostedActionStepDestroysNestedCostEntityInventoryRecursively`
- `CostedActionStepPreservesCostWhenUnderlyingStepFails`
- `CostedActionStepConsumesOnlySelectedQuantityWhenMoreCostExists`
- `CostedActionStepTraceReportsConsumedCostEntityIds`

Exit criteria:

- Cost destruction happens after successful consuming execution and is not observable on failed underlying actions.
- World occupancy, inventory containment, and clone/rollback foundations remain intact after cost destruction.

### Step 4: Descriptor, YAML, validation, catalog, and preview support

Goal: make cost authorable and inspectable through Content descriptors without hand-written runtime code.

Planned work:

- Add YAML load/save support for `cost` entries on behavior-chain steps.
- Validate missing/unknown `templateId`, non-positive quantity, and duplicate cost template entries according to the first-slice decision.
- Extend Action Step catalog field contracts and action-plan previews with cost summaries.
- Ensure existing content without cost round-trips unchanged.

Affected invariants:

- `YAML content loads from strings and files into registries that can be validated.`
- `Editable content documents round-trip through materialization and saved YAML.`
- `Canonical behavior descriptors and legacy action-plan descriptors preserve structured built-in inputs and materialize executable plans.`
- `The Action Step/primitive catalog describes every exposed primitive, value kind, implied state contract, and field contract.`
- `An Action Plan descriptor has exactly one active authored shape for canonical behavior, transitional primitive, legacy low-level steps, or empty/passive state; mixed authored shapes are invalid.`

Existing tests to review/preserve:

- `YamlContentLoaderCreatesRegistryFromDeclarativeContent`
- `YamlContentLoaderCanLoadRegistryFromFile`
- `EditableContentDocumentCanLoadMaterializeSaveAndReloadYaml`
- `YamlContentLoaderLoadsCanonicalBehaviorChain`
- `PrototypeRegistryValidationReportsMixedActionPlanShapes`
- `ContentEditorServiceValidatesCurrentDocumentAfterEdits`
- `ContentEditorServiceValidationReportsCurrentDocumentErrors`
- `ContentEditorListsCanonicalActionStepMetadata`

New intentionally failing tests before production changes:

- `YamlContentLoaderLoadsBehaviorStepCostEntries`
- `EditableContentDocumentRoundTripsBehaviorStepCosts`
- `PrototypeRegistryValidationReportsUnknownCostTemplate`
- `PrototypeRegistryValidationReportsNonPositiveCostQuantity`
- `PrototypeRegistryValidationReportsDuplicateCostTemplateEntries`
- `ActionStepCatalogDescribesBehaviorStepCostField`
- `ActionPlanPreviewSummarizesBehaviorStepCosts`

Exit criteria:

- Authored cost data survives load/materialize/save.
- Invalid cost authoring produces actionable diagnostics before simulation.
- Plan previews clearly identify cost requirements.

### Step 5: Editor service, agent API, and frontend-neutral surface parity

Goal: make cost fields authorable through shared editor services and agent/headless APIs, not only by hand-editing YAML.

Planned work:

- Expose cost entries in editor snapshots/action-plan step DTOs.
- Add service-backed mutation for replacing/clearing a step cost list.
- Add `AgentContentEditorApi` and content-tool support over the same session/service layer.
- Keep SadConsole/frontend responsibilities limited to consuming shared facts; no frontend-owned runtime legality.

Affected invariants:

- `Content editor operations preserve declared IDs, presentations, carried layouts, Action Plans/behavior assignments, legacy action plans, and validation results.`
- `Frontend editor snapshots and service-backed template/action-plan mutations expose authored scenarios, entity templates/carried layouts, action-state defaults, targeting rules..., action-plan summaries..., stable engine-defined action-step choices, validation diagnostics, YAML preview, diff state..., canonical action-plan step kind replace/insert/remove/move and target-label set/clear operations, and save results through shared content/editor services rather than Avalonia view models or ad-hoc YAML mutation.`
- `Frontend tests must not duplicate Core legality, materialization, turn advancement, inventory, containment, action-resolution, or durable content semantics.`

Existing tests to review/preserve:

- `ContentEditorServiceAddsReordersAndRemovesActionPlanSteps`
- `ContentEditorAuthorsCanonicalEnterExitBehaviorChain`
- `ContentEditorDefaultsRequiredMoveAndTransferOptionsWhenAuthoringBehaviorSteps`
- `FrontendEditorServiceTests`
- `FrontendEditorServiceAndAgentApiShareContentEditorSessionAsParallelSurfaces`
- `AgentContentEditorApiAuthorsCanonicalEnterExitBehavior`
- `ContentToolDispatcherKeepsSessionAcrossSemanticEditCalls`
- `ContentToolDispatcherCreatesBehaviorPlanAndPreviewWithValidationSummary`

New intentionally failing tests before production changes:

- `ContentEditorServiceSetsAndClearsBehaviorStepCosts`
- `FrontendEditorSnapshotIncludesBehaviorStepCosts`
- `AgentContentEditorApiAuthorsBehaviorStepCosts`
- `ContentToolDispatcherAuthorsBehaviorStepCosts`
- `BehaviorStepCostMutationRejectsInvalidStepIndexAndInvalidQuantities`

Exit criteria:

- Agents and future frontends can author costs through typed operations.
- Snapshots, previews, validation, and YAML diffs show cost changes consistently.

### Step 6: Content fixture, scenario reporting, docs, and broader verification

Goal: exercise the full slice through shared content/scenario tooling and update source-of-truth docs after behavior is implemented.

Planned work:

- Add a focused Beta/canonical action fixture with at least:
  - a sufficient-cost actor creating or transforming something;
  - an insufficient-cost actor falling through to a visible fallback;
  - a nested-cost case where the cost token is inside an actor-carried container.
- Ensure scenario run/player narrative/history projections expose missing-cost and consumed-cost facts without parsing formatted trace text.
- Update `Engine-Editor-Capabilities.md`, `invariants.md`, content-authoring docs, action-outcome/affordance docs, and planning index as appropriate after implementation.
- Run targeted suites and relevant broader suites.

Affected invariants:

- `Scenario runs use shared Content/Core services and schedule contained actors deterministically for scenario-root inventory spaces.`
- `Scenario reports treat expected in-simulation inability to act as runtime observation, not as an engine/runtime failure.`
- `Scenario run reports expose setup, actor order, history-interval-derived turn traces, final state, cycle-safe inventory/containment summaries, validation diagnostics, runtime observations, runtime failures, and capability gaps...`
- `Persisted scenario player narrative log reports expose a compact player narrative projection from shared scenario history/action-step outcome projection... and must not derive rows by parsing formatted trace lines.`
- `Built-in content must load and validate, but tests should not pin valid design choices such as balance values, glyphs, positions, or action plan behavior.`

Existing tests to review/preserve:

- `PrototypeRegistryValidationPassesForBuiltInContent`
- `ScenarioRunServiceShowsBehaviorStepsAndTreatsNoActionAsObservation`
- `ScenarioRunServiceSummarizesCarriedInventoryContents`
- `AgentContentEditorApiRunsPersistedScenarioById`
- `AgentContentEditorApiCreatesCombinedPersistedScenarioReport`
- `AgentContentEditorApiRunsPersistedScenarioPlayerNarrativeLogById`
- `ContentToolDispatcherRunsScenarioPlayerNarrativeLogById`
- `ActionLogProjectionFromHistoryIncludesAutonomousActionStepAttemptsWhenAvailable`

New intentionally failing tests before production changes:

- `CostSystemFixtureLoadsValidatesAndRunsSufficientCostCase`
- `CostSystemFixtureReportsMissingCostAsActionObservation`
- `CostSystemFixtureConsumesNestedCostTokenOnSuccess`
- `ScenarioPlayerNarrativeLogProjectsMissingCostWithoutTraceStringParsing`
- `PrototypeRegistryValidationPassesForBuiltInContentWithCostFixture`

Exit criteria:

- The cost fixture validates and runs through shared scenario tooling.
- Missing-cost failures are visible as normal action outcomes/observations, not engine failures.
- Source-of-truth docs and invariant test traces are updated after implementation.

## Verification commands

The implementing agent should refine these based on the actual test locations touched, but the expected verification path is:

1. Targeted Core tests for action-step cost semantics.
2. Targeted Content tests for YAML, validation, editor service, agent API, and scenario fixture behavior.
3. Relevant broader suites, likely:

```powershell
dotnet test tests/GameGameGame.Core.Tests/GameGameGame.Core.Tests.csproj
dotnet test tests/GameGameGame.Content.Tests/GameGameGame.Content.Tests.csproj
```

Add SadConsole tests only if the implementation introduces stable frontend-owned presentation or interaction behavior. Otherwise, frontend/manual smoke work should consume shared Content/Core facts without duplicating legality tests.
