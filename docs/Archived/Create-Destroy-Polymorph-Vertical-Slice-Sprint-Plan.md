---
id: archived.create-destroy-polymorph-vertical-slice-sprint
title: Create, Destroy, And Polymorph Vertical Slice Sprint Plan
kind: archived
status: archived
truth_rank: 45
truth_domains: [planning-priority, implementation-navigation, test-trace]
owners: [core-owner]
audience: [core-owner, content-editor, frontend-owner]
read_when:
  - implementing template-backed entity creation or polymorphing
  - changing DestroyTarget, CreateEntity, CreateFacing, or PolymorphTarget semantics
  - authoring or validating entity-lifecycle content scenarios
related:
  - source.invariants
  - source.testing-charter
  - source.engine-editor-capabilities
  - source.content-authoring-manual
  - source.action-step-outcome-and-affordance-logic
  - source.vertical-slice-map
  - plan.canonical-actions-vertical-slice
---

# Create, Destroy, And Polymorph Vertical Slice Sprint Plan

Status: Archived completed focused sprint plan selected from the canonical action/world-mutation backlog. This was Plan A from the design session: engine/content/editor parity for autonomous Create, Destroy, and Polymorph gameplay, with richer frontend/player-command polish deferred to follow-up work.

## Goal

Add first-class authorable runtime entity lifecycle actions:

- create a new runtime entity from an authored template;
- destroy a selected existing entity;
- polymorph a selected existing entity into another authored template while preserving the required runtime state.

The flagship scenario is a room containing an egg, a rat king, and a snake where:

- rats move forward and backstep when blocked;
- rat kings stand still and create rats;
- snakes stand still and destroy adjacent rats;
- eggs, caterpillars, cocoons, and butterflies repeatedly polymorph through a visible lifecycle.

## Scope

### In scope

1. Core runtime support for template-backed `CreateEntity`.
2. Core runtime support for `PolymorphTarget`.
3. Preserve and document current `DestroyTarget` behavior as the simple destroy step for this slice.
4. Runtime template/type identity sufficient for create, polymorph, targeting, presentation lookup, reports, and future logs.
5. Descriptor/YAML support for new action-step fields.
6. Validation/defaulting for template references and placement fields.
7. Action Step catalog metadata and action-plan preview support.
8. Editor service and agent/headless API parity for authoring the new fields through typed operations.
9. A content fixture/scenario that exercises rat creation/destruction and the lifecycle loop.
10. Targeted tests first, then the smallest Core/Content/Editor implementation needed to pass them.
11. Source-of-truth doc updates after behavior is implemented.

### Out of scope / Plan B follow-up

- Direct player Action Choice prompts for Create, Destroy, or Polymorph.
- SadConsole play-mode menu UX for player-triggered lifecycle actions.
- New frontend visual treatments or animation beyond existing simulation visibility.
- Rich final player-facing wording. Structured facts and traces may improve, but frontend text polish is deferred.
- Random, target-relative, ranged, or inventory spawning placement.
- Non-adjacent polymorph effects, delayed lifecycle timers, reactions, or scripting hooks.

## Gameplay semantics

### `DestroyTarget`

Existing step retained for the sprint:

```yaml
- kind: DestroyTarget
  targetLabel: prey
```

Semantics:

- Reads the actor's target from `targetLabel`, `targetSlot`, or compatible default target state.
- If the target exists and is not the actor, the target ceases to exist in the current runtime.
- Current compatibility behavior recursively destroys the target's inventory space and contained descendants; this remains the selected behavior for the sprint unless a failing test exposes an incompatible invariant.
- Consumes the actor turn on success.
- Falls through on missing, invalid, or self target.
- Target filtering such as "adjacent rats only" belongs in the actor template targeting profile, not inside `DestroyTarget` itself.

### `CreateEntity`

New canonical/prototype-to-stable step for template-backed spawning:

```yaml
- kind: CreateEntity
  templateId: rat
```

Default semantics:

- `templateId` is required for valid authored content.
- `placement` defaults to `AdjacentOpen` when omitted by current editor-service authoring/defaulting workflows.
- Creates one new runtime entity instance from the referenced template.
- The new runtime entity receives a fresh stable runtime entity ID and template-derived defaults, including name/type identity, bulk, aperture, inventory dimensions, default action plan, action-state defaults, targeting profile, policies, topology policy, and authored carried layout.
- The new entity appears adjacent to the creator.
- `AdjacentOpen` placement chooses the first valid/open adjacent cell in the shared Core/topology stable direction order.
- Consumes the actor turn on success.
- Falls through without creating anything when the template is missing/invalid, recursive template materialization fails, no adjacent cell can be selected, or the selected cell is invalid/occupied.

Single-direction authoring is also in scope:

```yaml
- kind: CreateEntity
  templateId: rat
  placement: Facing
  directionMode: Forward
```

Facing placement semantics:

- `placement: Facing` resolves one direction from `directionMode`.
- `directionMode` uses the same absolute/relative enum as canonical `Move`.
- When `directionMode` is omitted for Facing placement, service-backed authoring should default it to `Forward`; validation should still report malformed hand-authored descriptors that cannot be resolved.
- If a relative mode is used, the actor needs `Facing`; missing Facing causes fallthrough.
- The created entity appears in exactly the resolved adjacent cell when valid/open.

Compatibility note:

- Existing `CreateFacing` placeholder-rock behavior remains loadable/executable until explicitly retired. New normal authoring should use `CreateEntity`.

### `PolymorphTarget`

New target-consuming lifecycle step:

```yaml
- kind: PolymorphTarget
  targetSelf: true
  templateId: caterpillar
```

Semantics:

- Reads the selected target from `targetSelf`, `targetLabel`, `targetSlot`, or compatible default target state.
- Looks up the authored destination `templateId`.
- Mutates the target entity in place; it is not destroyed/recreated.
- Preserves runtime entity ID and current location/containment.
- Preserves current facing direction.
- Preserves inventory dimensions.
- Preserves current inventory contents and their coordinates.
- Preserves target slot and target-label state.
- Preserves runtime control source and one-turn action-plan override state unless implementation discovers an existing invariant that requires clearing overrides; if so, revise this plan before implementation.
- Switches to the destination template defaults for template/type identity, presentation lookup identity, name/default type identity, aperture, bulk, targeting profile, policies/topology policy, and default action plan.
- Consumes the actor turn on success.
- Falls through without partial mutation when the target is missing/invalid or the destination template is missing/invalid.

Open implementation detail to settle during the first failing-test pass:

- Runtime `Entity` currently stores dimensions, bulk, aperture, and policies but not an explicit template ID. The first implementation should introduce the smallest Core-owned runtime template identity/catalog model needed without leaking Content DTOs into Core.

## Authoring and user-facing controls

### Action-plan step editor/API fields

Add service/API-backed fields to canonical behavior-chain step descriptors:

- `templateId` for `CreateEntity` and `PolymorphTarget`.
- `placement` for `CreateEntity`, with `AdjacentOpen` default and `Facing` supported.
- existing `directionMode` reused for `CreateEntity` when `placement: Facing`.
- existing `targetLabel`, `targetSlot`, and `targetSelf` reused for `PolymorphTarget`.

Recommended editor labels:

- Step kind: `Create Entity`
  - Template dropdown: entity templates.
  - Placement dropdown: `First open adjacent cell` / `Facing direction`.
  - Direction mode picker shown only for Facing placement.
- Step kind: `Polymorph Target`
  - Target selector: `Self`, target label, or advanced numeric target slot.
  - Template dropdown: entity templates.
- Step kind: `Destroy Target`
  - Existing target selector remains sufficient.

Preview copy examples:

- `Create a new rat in the first open adjacent cell.`
- `Create a new rat in the actor's forward adjacent cell.`
- `Polymorph self into caterpillar; preserve facing, inventory dimensions, inventory contents, and targets.`

Validation diagnostics should include:

- missing `templateId` on `CreateEntity` or `PolymorphTarget`;
- unknown `templateId`;
- missing/unsupported `placement` if hand-authored invalid data cannot be defaulted;
- `placement: Facing` with missing/unresolvable `directionMode`;
- missing target reference for `PolymorphTarget` unless `targetSelf` is true;
- optional warning when the destination polymorph template has different inventory dimensions because dimensions are intentionally preserved.

## Implementation layer plan

Follow `docs/Source of Truth/vertical-slice-map.md` for layer order.

1. Core model/runtime
   - Add runtime template/type identity and a Core-owned runtime template resolver/catalog shape.
   - Ensure content materialization can populate the runtime catalog without Core depending on Content DTOs.
   - Add `CreateEntity` and `PolymorphTarget` execution and traces.
2. Descriptor/YAML model
   - Add action-step fields for `templateId` and `placement`.
   - Preserve old descriptors/content compatibility.
3. Validation/default policy
   - Validate template references and placement fields.
   - Default `CreateEntity.placement` to `AdjacentOpen` in service-backed authoring.
   - Default `CreateEntity` Facing placement `directionMode` to `Forward` when inserted/replaced through editor services.
4. Catalog metadata/discovery
   - Add catalog entries and field contracts for `CreateEntity` and `PolymorphTarget`.
   - Mark `CreateFacing` compatibility/prototype-only in docs/catalog tiers if needed.
5. Editor service and agent API
   - Add typed operations for setting/clearing step template ID and create placement.
   - Surface field values in snapshots/previews.
6. Content fixture/scenario
   - Add or generate a focused scenario with egg, rat king, snake, rats, caterpillar, cocoon, and butterfly templates.
   - Use existing target profiles to make snake select adjacent rats and lifecycle entities self-polymorph.
7. Tests/docs
   - Add failing tests before production changes.
   - Run targeted Core/Content tests, then relevant broader suites.
   - Update source-of-truth capability, authoring, action-logic, and invariant traces once behavior is implemented.

## TDD trace

Implementation must not start until tests representing the planned behavior have been added/revised and fail for the expected reason.

### Testable outcomes

1. `CreateEntity` creates a new runtime entity from an authored template in the first open adjacent cell by default.
2. `CreateEntity` supports `placement: Facing` with authored `directionMode` and creates in exactly that resolved adjacent cell.
3. `CreateEntity` fails/falls through without mutation when no adjacent/open authored destination exists or the template reference is invalid.
4. Created entities carry template-derived defaults and can act using the template's default action plan.
5. `PolymorphTarget` mutates an existing target in place to another template's defaults for type/presentation identity, bulk, aperture, targeting profile, policies/topology policy, and default action plan.
6. `PolymorphTarget` preserves runtime ID, current facing, inventory dimensions, current inventory contents, target slots/labels, control source, and current location/containment.
7. `PolymorphTarget` fails/falls through without partial mutation when the target or destination template is invalid.
8. Descriptor/YAML/editor/API layers can author, validate, preview, round-trip, and run the new fields without hand-editing YAML.
9. The flagship scenario visibly exercises rat creation/destruction and the egg/caterpillar/cocoon/butterfly loop through shared scenario run tooling.

### Affected invariants from `docs/Source of Truth/invariants.md`

- Every meaningful game object is an entity with a stable ID.
- Entity locations are represented by occupancy of nodes in planes.
- At most one entity may occupy a node at a time.
- Traversals through containment or inventory relationships must be cycle-safe.
- An entity is an actor only if it has a decidable Action Plan/fallback chain or decision trigger.
- Actions must produce structured traces for failed checks and resolutions.
- Canonical behavior chains continue after a failed/non-consuming step and stop after the first successful turn-consuming step.
- Canonical behavior-chain traces must report attempted steps, state reads/writes, fallback continuation/stopping, and terminal turn outcome.
- Canonical Action Steps must preserve their documented state contracts for `Facing`, `Target`, movement, target selection, inventory transfer, fallthrough, and deterministic tie-breaks.
- Runtime control-source state is cloned/restored with world history.
- Entity action state such as `Facing`, numeric `Target` slots, and labeled targets is typed and persists on the actor entity across plan executions.
- Canonical behavior descriptors and legacy action-plan descriptors preserve structured built-in inputs and materialize executable plans.
- The Action Step/primitive catalog describes every exposed primitive, value kind, implied state contract, and field contract.
- An Action Plan descriptor has exactly one active authored shape for canonical behavior, transitional primitive, legacy low-level steps, or empty/passive state.
- YAML content loads from strings and files into registries that can be validated.
- Editable content documents round-trip through materialization and saved YAML.
- Content editor operations preserve declared IDs, presentations, carried layouts, Action Plans/behavior assignments, action-state defaults, targeting rules, validation results, and service-backed action-step mutations.
- Built-in content must load and validate, without tests pinning transient balance/content choices.
- Scenario runs use shared Content/Core services and schedule contained actors deterministically for scenario-root inventory spaces.
- Scenario reports treat expected in-simulation inability to act as runtime observation, not engine/runtime failure.

### Existing tests to preserve or revise

Preserve as compatibility unless the implementation deliberately updates docs and tests for a breaking change:

- `DestroyTargetRecursivelyRemovesTargetInventoryAndContainedEntities`
- `CreateFacingCreatesPlaceholderRockEntityInFacingDirection`
- `ScenarioRunnerReportsDestroyTargetScenario`
- `ScenarioRunnerReportsCreateFacingScenario`
- `ScenarioRunnerReportsUnsupportedCapabilityGap` entries documenting missing template-backed `CreateFacing(templateId)` should be revised/retired when `CreateEntity(templateId)` support lands.
- `SeekTargetAdjacentFallsThroughAndPreservesTargetForDestroyTarget`
- `TargetConsumingBehaviorCanReadTargetLabelFromExecutingActor`
- `TargetConsumingBehaviorFailsWhenTargetLabelHasNoCurrentTarget`
- `WorldStateClonePreservesMutableSimulationStateWithoutSharingCollections`
- `WorldStateClonePreservesActionControlSource`
- `ActionPlanDescriptorKeepsBuiltInInputsAsData`
- `ActionPlanDescriptorMaterializesExecutableBuiltIns`
- `BuiltInPlanPartsExposeStructuredInputs`
- `PlanPrimitiveCatalogExposesAllCheckEffectAndValueKinds`
- `ContentEditorListsCanonicalActionStepMetadata`
- `YamlContentLoaderLoadsCanonicalBehaviorChain`
- `EditableContentDocumentCanLoadMaterializeSaveAndReloadYaml`
- `ContentEditorServiceAddsReordersAndRemovesActionPlanSteps`
- `ContentEditorServiceValidatesCurrentDocumentAfterEdits`
- `SnapshotIncludesActionPlanStepTargetReferencesAndTargetConsumptionMetadata`
- `ActionPlanStepMutationsDefaultRequiredMoveAndTransferOptions`
- `AgentContentEditorApiAuthorsCanonicalEnterExitBehavior`
- `ContentToolDispatcherCreatesBehaviorPlanAndPreviewWithValidationSummary`
- `ScenarioRunServiceShowsBehaviorStepsAndTreatsNoActionAsObservation`

### New intentionally failing tests to add before production code

Core tests:

- `CreateEntityCreatesTemplateInstanceInFirstOpenAdjacentCellByDefault`
- `CreateEntityFacingPlacementUsesAuthoredDirectionMode`
- `CreateEntityFallsThroughWhenNoAdjacentPlacementIsOpen`
- `CreateEntityInitializesTemplateDefaultsAndDefaultActionPlan`
- `PolymorphTargetAppliesTemplateDefaultsAndPreservesRuntimeIdentityFacingInventoryAndTargets`
- `PolymorphTargetPreservesInventoryDimensionsAndCurrentInventoryContents`
- `PolymorphTargetFallsThroughWithoutPartialMutationWhenTemplateIsMissing`
- `PolymorphTargetCanTargetSelfForLifecycleBehavior`

Content/editor tests:

- `YamlContentLoaderLoadsCreateEntityAndPolymorphTargetTemplateFields`
- `EditableContentDocumentRoundTripsCreateEntityAndPolymorphTargetFields`
- `PrototypeRegistryValidationReportsMissingCreateEntityTemplateReference`
- `PrototypeRegistryValidationReportsMissingPolymorphTargetTemplateReference`
- `PrototypeRegistryValidationReportsInvalidCreateEntityPlacement`
- `ContentEditorServiceSetsBehaviorStepTemplateIdAndCreatePlacement`
- `ContentEditorDefaultsCreateEntityPlacementToAdjacentOpen`
- `AgentContentEditorApiAuthorsCreateEntityAndPolymorphTargetBehavior`
- `ActionPlanPreviewShowsCreateEntityAndPolymorphTargetTemplateReferences`

Scenario/integration tests:

- `ScenarioRunServiceRunsCreateDestroyLifecycleScenario`
- `ScenarioRunReportShowsTemplateBackedCreateAndPolymorphAttempts`

### Relevant test commands

Targeted commands may be refined after file locations are selected, but the implementation should run at least:

```powershell
dotnet test "tests/GameGameGame.Tests/GameGameGame.Tests.csproj" --filter "CreateEntity|Polymorph|DestroyTarget|ActionPlanDescriptor|ActionStepCatalog|YamlContentLoader|PrototypeRegistryValidation|ContentEditor|AgentContentEditorApi|ScenarioRun"
```

Then run the broader relevant suite:

```powershell
dotnet test "tests/GameGameGame.Tests/GameGameGame.Tests.csproj"
```

If SadConsole/frontend UI is touched unexpectedly, stop and coordinate a Plan B/frontend-owner follow-up before expanding scope.

## Exit criteria

- `CreateEntity` and `PolymorphTarget` are executable through canonical behavior chains.
- The new steps are authorable and validatable through YAML, editor services, and agent/headless APIs.
- `CreateEntity` defaults to adjacent-open placement and also supports Facing/single-direction authoring.
- Polymorph preserves the explicitly requested runtime state and switches the explicitly requested template defaults.
- Existing `DestroyTarget` simple destruction remains covered and usable for adjacent-rat snake behavior through targeting profiles.
- The flagship lifecycle scenario can be run through shared scenario tooling.
- Capability, authoring, action-logic, and invariant docs are updated after implementation.

## Sprint friction log

- 2026-07-28: Targeted `dotnet test` after the first Core implementation hit locked `GameGameGame.Content.Tools` output DLLs because a running `GameGameGame.Content.Tools` process held `bin\Debug\net10.0` files. Mitigation used: attempted an alternate temporary output path, but that caused duplicate generated assembly attributes because generated `obj` files were picked up by the project compile glob; stopped the stale local `GameGameGame.Content.Tools` process instead, then reran the targeted test successfully. Future mitigation: prefer stopping stale local tool-host processes over alternate project output paths unless the project file excludes generated temp output from compilation.
- 2026-07-28: Frontend-owner review found that SadConsole paths still use launch-time registry/template/action-plan lookups in several places, so dynamic created/polymorphed entities may render with stale or missing presentations and may not appear in launch-time frontend initiative/action-plan projections even though shared headless scenario runs now resynchronize them. Mitigation used in this sprint: kept the user-facing scenario autonomous/headless-valid, added world-aware Content registry lookup and dynamic scenario-run action-plan synchronization, and deferred SadConsole-specific dynamic rendering/projection changes to the Plan B/frontend-owner follow-up.

## Archive note

- Completed Core/Content lifecycle semantics, YAML/DTO support, validation, action-step catalog entries, scenario materialization, dynamic scenario-run action-plan synchronization, and the user-facing flagship room `delta-create-destroy-polymorph-flagship-room`.
- SadConsole demo follow-through was completed in `docs/Archived/SadConsole-Dynamic-Entity-Lifecycle-Demo-Sprint-Plan.md`.
