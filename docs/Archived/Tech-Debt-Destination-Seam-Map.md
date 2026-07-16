# Tech Debt Destination Seam Map

Status: Archived completed sprint record. Keep as a destination map for future optional follow-ups.

Read when:

- reviewing completed destination-first extractions after Console/Avalonia removal;
- deciding future optional extraction batches;
- choosing tests for low-risk refactors.

## Completed sprint summary

- Dead Console and Avalonia surfaces were removed.
- Shared Agent API, scenario run/report, catalog scan, and recording adapter seams now live under Content/Headless ownership boundaries.
- Frontend snapshot and mutation groups were split behind `FrontendEditorService`.
- Content editor projection/template/carried/action-plan services were split behind `ContentEditorService`.
- Registry validation and spawn/materialization were split behind `PrototypeContentRegistry`.
- Core behavior-chain and primitive execution/dispatch entrypoints were split into partial executor files.
- Broad `GameGameGame.Tests` and `GameGameGame.SadConsole.Tests` suites are green.

## Optional future follow-ups

- Split individual Core action-step/primitive implementation families out of `ActionPlanInterpreter` if Core work resumes there.
- Extract a legacy low-level content action-plan editor if compatibility editing starts changing again.
- Consolidate tiny duplicated validator helpers only if they become maintenance friction.

## Cleanup principle

Split by **destination concern**, not by source file. A batch should move all code that belongs to one destination together, even when that code currently spans several large files.

Preferred ordering:

1. Identify stable destination and public seam.
2. Move pure projection/validation/helper code first when possible.
3. Keep public service APIs stable until downstream callers are ready.
4. Run narrow tests for the moved concern, then broader non-frontend tests.

## Destination map

| Destination concern | Proposed files / types | Current source seams | Primary tests |
|---|---|---|---|
| Frontend editor snapshot projection | `FrontendEditorSnapshotBuilder` (extracted), optional future `FrontendEditorProjectionModels.cs` | `FrontendEditorService.GetSnapshot`, `ListScenarios`, `ListEntityTemplates`, `ListActionPlans`, `ListAvailableActionSteps`, `GetActionSteps`, `GetActionStepNames`, display helper lookups | `FrontendEditorServiceTests`, SadConsole editor context/screen tests |
| Frontend editor mutation shell | Keep `FrontendEditorService` as facade; delegate to focused mutation services | Save remains in facade; mutation groups delegated | `FrontendEditorServiceTests`, `FrontendEditorServiceAndAgentApiShareContentEditorSessionAsParallelSurfaces` |
| Frontend entity-template mutations | `FrontendEntityTemplateMutationService` (extracted) | create/duplicate/delete template, presentation/metadata, initial facing, default plan | `FrontendEditorServiceTests`, `ContentEditorServiceUpdatesEntityPresetAndPresentation` |
| Frontend carried-layout mutations | `FrontendCarriedLayoutMutationService` (extracted) | place/remove/move/replace/overwrite carried entity plus validation helpers | `FrontendEditorServiceTests`, `ContentEditorServicePlacesAndMovesCarriedEntityInInventoryLayout` |
| Frontend action-plan mutations | `FrontendActionPlanMutationService` (extracted) | create/passive/duplicate/delete action plan, replace/insert/remove/move steps, set target label | `FrontendEditorServiceTests`, `ContentEditorServiceAddsReordersAndRemovesActionPlanSteps`, `SetActionPlanStepTargetLabel*` |
| Frontend targeting-rule mutations | `FrontendTargetingRuleMutationService` (extracted) | set/clear targeting rule and label/range/capability validation | targeting-rule tests, `SetTemplateTargetingRuleCanWriteCapabilityAdjectives`, frontend editor tests |
| Content entity-template editor core | `EntityTemplateEditorService` (extracted) | `ContentEditorService` entity template CRUD, default plan assignment, presentation update, template references, default variables, action-state defaults, targeting rules | `ContentEditorServiceTests`, `EditableContentDocumentTests` |
| Content carried-layout editor core | `CarriedEntityLayoutEditor` (extracted) | `ContentEditorService` carried placement/list/find/move/remove/replace/validate | `ContentEditorServicePlacesAndMovesCarriedEntityInInventoryLayout`, registry layout validation tests |
| Content action-plan editor core | `ActionPlanEditorService` (canonical/lifecycle slice extracted), optional future `LegacyActionPlanStepEditor` | action plan CRUD, behavior-chain edits, primitive helpers extracted; legacy low-level check/effect edits remain in facade | `ContentEditorServiceTests`, `ContentEditorAuthoringTests`, Agent API tests |
| Action-plan preview | `ActionPlanPreviewService` (extracted) | `ContentEditorService.PreviewActionPlan` facade delegates to service; guidance, shape formatting, preview steps/state hints moved | preview tests, Agent API combined report tests |
| Content targeting/action-state editor core | `EntityActionStateEditor`, `EntityTargetingRuleEditor` | default variables, action state defaults, initial facing, targeting rules | targeting tests, content editor tests |
| Runtime canonical behavior-chain executor | `BehaviorChainExecutor` (extracted entry/dispatch slice) | `ExecuteBehavior` and `ApplyBehaviorStep` dispatch extracted; individual action-step implementations still remain in interpreter partial | `CoreActionPlanTests` canonical behavior-chain tests, action-step contract tests |
| Runtime legacy low-level plan executor | `LegacyActionPlanExecutor` | `ActionPlanInterpreter` loop over `ActionPlanStep`, `EvaluateChecks`, `ApplyEffect`, `ApplyCallPlan` | legacy low-level plan tests, variable/check/effect tests |
| Runtime primitive compatibility executor | `PrimitiveActionPlanExecutor` (extracted entry/dispatch slice) | `ExecutePrimitive` and `ApplyPrimitive` dispatch extracted; primitive implementations still remain in interpreter partial | primitive-backed plan tests |
| Runtime canonical action-step implementations | `ActionStepRuntime` or folder `ActionSteps/*` | `ApplyAcquireNearestTarget`, `ApplySeekTarget`, `ApplyFleeTarget`, `ApplyMaintainChebyshevDistanceTwo`, `ApplyStrafeTarget`, `ApplyMoveFacingPrimitive`, inventory/interaction steps | `CoreActionPlanTests` per-step contracts, bulk/aperture tests |
| Runtime transfer/action-step helpers | `InventoryActionStepRuntime`, `TargetedMovementActionStepRuntime` | `TryReadTransferTarget`, `TransferToFirstOpenInventory`, `FindFirstCarriedEntity`, distance/direction helpers | transfer/action-step tests |
| Registry validation orchestrator | Keep `PrototypeContentRegistry.Validate` as facade; delegate validators | `Validate`; action-plan, entity-template, action-state, and legacy variable validation delegated | `ContentRegistryValidationTests`, built-in content validation |
| Entity template validation | `EntityTemplateValidator` (extracted) | missing presentations, template default plan refs, default variable materialization, carried layout, carried template refs, targeting rules | template/reference/targeting validation tests |
| Action-plan shape/reference validation | `ActionPlanValidator` (extracted), optional future `ActionPlanShapeValidator` | shape validation, missing refs, mixed shapes, primitive/behavior validation, legacy movement descriptor validation | action-plan validation tests |
| Action state / slot validation | `ActionStateContractValidator` (extracted) | slot read/write/defaultable validation, initial action-state slot inference, primitive slot descriptors, canonical behavior step state contracts | slot/default state validation tests |
| Legacy variable validation | `LegacyPlanVariableValidator` (extracted) | legacy low-level check/effect variable reads/writes, call-plan variable propagation, SetVariable value-kind inference, variable-name extraction | legacy variable compatibility tests |
| Registry materialization/spawn | `PrototypeEntitySpawner` (extracted) | `SpawnEntity`, carried-template recursive spawn, plan/default merges, world action-state defaults; registry keeps template assignment map and action-plan creation facade | materialization/spawn tests, alpha/beta fixture tests |

## Source file seam notes

### `FrontendEditorService.cs`

Current role: SadConsole-facing facade combining mutation input validation, editor-service calls, snapshot construction, and DTO definitions.

Likely split:

1. **Snapshot projection first**
   - Extract `FrontendEditorSnapshotBuilder` with read-only inputs: `ContentEditorSession`, diagnostics, Action Step catalog.
   - Move summary DTO construction helpers there.
   - Keep DTO records either in same file initially or a `FrontendEditorModels.cs` file.
   - Benefit: reduces class size without changing mutation behavior.

2. **Mutation groups second**
   - Extract template, carried-layout, action-plan, and targeting-rule mutation helpers.
   - Carried-layout mutations are extracted to `FrontendCarriedLayoutMutationService` behind the `FrontendEditorService` facade.
   - Action-plan mutations are extracted to `FrontendActionPlanMutationService`.
   - Entity-template mutations are extracted to `FrontendEntityTemplateMutationService`.
   - Targeting-rule mutations are extracted to `FrontendTargetingRuleMutationService`.
   - Each helper returns `FrontendEditorMutationResult` and accepts a callback/snapshot provider or returns a lower-level mutation status that facade wraps.
   - Prefer not duplicating `GetSnapshot()` calls in every extracted method; introduce one helper such as `MutationResultFactory` if needed.

3. **Validation helper extraction**
   - Current validation helpers are coupled to user-facing messages and snapshot failure behavior.
   - Keep them near the mutation group they serve rather than in a generic validator.

First safe batch: `FrontendEditorSnapshotBuilder` extraction.

### `ContentEditorService.cs`

Current role: central content mutation service plus preview/projection helper plus ID generation.

Likely split:

1. **ActionPlanPreviewService** - complete
   - Pure read/projection extracted behind `ContentEditorService.PreviewActionPlan` facade.
   - Owns document-backed registry validation, plan descriptor lookup, template action-state defaults, guidance, YAML preview, and shape formatting.

2. **ActionPlanEditor / BehaviorChainEditor** - canonical/lifecycle slice complete
    - `ActionPlanEditorService` owns action-plan CRUD, references, primitive assignment, and canonical behavior-chain mutations.
    - Legacy low-level step/check/effect edits remain in `ContentEditorService`; a separate compatibility editor can still extract them later if needed.

3. **EntityTemplateEditor + CarriedEntityLayoutEditor** - entity/carried slice complete
   - `EntityTemplateEditorService` now owns template CRUD, presentation update, default plan assignment, default variables, action-state defaults, targeting rules, and template-reference deletion guard.
   - `CarriedEntityLayoutEditor` now owns carried placement/list/find/move/remove/replace/validate and generated carried IDs.

4. **TargetingRuleEditor**
   - Small but important because it bridges content authoring and action-step capability metadata.

Public API strategy: keep `ContentEditorService` as stable facade for now and delegate internally. Move callers only after extracted services prove stable.

First safe batch: `ActionPlanPreviewService` extraction, then carried-layout editor.

### `ActionPlanInterpreter.cs`

Current role: runtime execution orchestration plus all canonical Action Step implementations plus legacy low-level plan support.

Risk: high semantic density. Use small red/green refactors and run targeted Core action tests frequently.

Likely split:

1. **Legacy low-level executor**
   - Loop over `ActionPlanStep`, `EvaluateChecks`, `ApplyEffect`, `ApplyCallPlan`.
   - This is distinct from canonical behavior chains and transitional primitive plans.

2. **BehaviorChainExecutor**
   - Own ordered Action Step resolution, continued/stopped behavior, trace wrapping, and behavior-step dispatch.
   - Calls action-step runtime dispatcher.

3. **Primitive compatibility executor**
   - Own transitional `ActionPlanPrimitiveDescriptor` fallback semantics and primitive dispatch.
   - Can share implementations with canonical action-step runtime where behavior is identical.

4. **Action Step runtime implementations**
   - Split by families:
     - movement/facing: move, backstep, turn, seek/flee/maintain/strafe;
     - target acquisition and overrides;
     - inventory/containment transfer: pickup/drop/give/take/enter/exit;
     - world mutation: push/destroy/create.

First safe batch: extract pure helper functions and a small `ActionStepRuntimeDispatcher` wrapper only after adding no behavior changes. Avoid splitting all action steps in one PR/batch.

### `PrototypeContentRegistry.cs`

Current role: immutable content registry, runtime materialization/spawn, action-plan creation, validation orchestration, and all validation rules.

Likely split:

1. **Validation orchestrator and validators**
   - Keep `PrototypeContentRegistry.Validate()` as facade.
   - Create validators that receive registry data snapshots rather than owning mutation.
   - Action-plan descriptor validation is extracted to `ActionPlanValidator`; entity-template validation is extracted to `EntityTemplateValidator`; action-state slot validation is extracted to `ActionStateContractValidator`; legacy variable validation is extracted to `LegacyPlanVariableValidator`.

2. **Entity/template validation** - complete
   - Missing presentations, carried layout, default plan refs, targeting rules, default variable materialization, and carried template refs now live in `EntityTemplateValidator` behind the registry facade.

3. **Action plan validation** - descriptor-only slice complete
   - Mixed shape, empty behavior chain, missing references, apply-plan references, primitive fallback references, materialization exceptions, and legacy movement descriptors now live in `ActionPlanValidator` behind the registry facade.

4. **State/slot/legacy variable validation** - complete
   - Action-state slot inference, required/defaultable slot reads, slot writes, primitive slot descriptors, canonical behavior step state contracts, and call-plan slot propagation now live in `ActionStateContractValidator`.
   - Legacy low-level check/effect variable reads/writes, call-plan variable propagation, SetVariable value-kind inference, and variable-name extraction now live in `LegacyPlanVariableValidator`.

5. **Spawn/materialization** - complete
   - `PrototypeEntitySpawner` owns recursive template spawning, carried inventory materialization, plan/default merges, and world action-state default application behind `PrototypeContentRegistry.SpawnEntity()`.

Validation batches complete: `ActionPlanValidator`, `EntityTemplateValidator`, `ActionStateContractValidator`, and `LegacyPlanVariableValidator` extract descriptor/template/state/variable validation while preserving `PrototypeContentRegistry.Validate()` behavior.

## Recommended implementation order

1. **Projection-only extraction:** `FrontendEditorSnapshotBuilder` - complete.
2. **Preview-only extraction:** `ActionPlanPreviewService` from `ContentEditorService` - complete.
3. **Registry validation extraction:** action-plan descriptor validator, entity-template validator, action-state contract validator, and legacy variable validator - complete.
4. **Frontend mutation groups:** carried-layout, action-plan, entity-template, and targeting-rule mutation services - complete.
5. **Core interpreter split:** behavior-chain and primitive execution/dispatch extracted into partial executor files; next split should move action-step/primitive implementation families if more Core cleanup is needed.

## Verification menu

Use targeted tests by destination:

- Frontend snapshot/mutation: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FrontendEditorServiceTests|FrontendEditorServiceAndAgentApiShareContentEditorSessionAsParallelSurfaces"`
- Content editor/Agent API: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "ContentEditorServiceTests|ContentEditorAuthoringTests|AgentContentEditorApiTests"`
- Registry validation: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "ContentRegistryValidationTests|PrototypeRegistryValidation|PrototypeContent"`
- Core interpreter/action steps: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "CoreActionPlanTests|CoreActorTurnResolverTests|CoreBulkApertureTests|ControlledActorCommandServiceTests"`
- Broad non-frontend check after each batch: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --no-restore`

Transient prototype content should not have scenario-specific tests unless promoted as stable shipping content. Prefer focused inline fixtures for engine/content/editor contracts and broad infrastructure checks over beta-showcase choreography assertions.
