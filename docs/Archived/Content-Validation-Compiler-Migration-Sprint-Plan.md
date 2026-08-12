---
id: plan.content-validation-compiler-migration-sprint
title: Content Validation Compiler Migration Sprint Plan
kind: plan
status: archived
truth_rank: 40
truth_domains: [planning-priority, implementation-navigation, test-trace]
owners: [core-owner]
audience: [core-owner, content-editor, frontend-owner]
read_when:
  - refactoring Content validation toward a compiler-shaped pipeline
  - changing Content validation diagnostics, editor validation consumption, or reference discovery
  - preparing for future multi-document content composition without implementing imports
related:
  - source.invariants
  - source.testing-charter
  - source.engine-editor-capabilities
  - source.vertical-slice-map
---

# Content Validation Compiler Migration Sprint Plan

Status: Archived completed refactor sprint plan. This sprint introduced the one-document `ContentCompiler`, unified registry/canonical authoring diagnostics, structured formerly string-heavy validation issues, source attribution, a one-document symbol/reference index, and compiler routing for shared Content/editor consumers. Retained as implementation context for future content workspace / multi-document surface work.

## Goal

Make the current one-file content validation path compiler-shaped enough that a later content workspace / multi-file composition sprint can be implemented without first untangling validation ownership.

This sprint should keep current authored content behavior intact while introducing clearer seams:

1. one entry point for compiling one editable document into semantic content plus diagnostics;
2. one validation pipeline that includes registry validation and canonical-authoring validation;
3. structured diagnostics that are suitable for future file/source attribution;
4. a one-document symbol/reference index that editor tooling can use by type rather than by YAML file shape;
5. editor, agent, preview, and scenario consumers routed through the compiler seam where practical.

## Non-goals

- Do **not** implement multi-file loading, imports, includes, package manifests, shipped-vs-user content separation, or cross-file merge semantics.
- Do **not** change the authored YAML schema except for optional diagnostic/source metadata in runtime result objects if selected.
- Do **not** reorganize checked-in content YAML files.
- Do **not** change Core runtime behavior, Action Step semantics, scenario materialization semantics, or editor authoring capabilities.
- Do **not** make file structure visible as the primary editor organization model; this sprint prepares type-first projections but does not redesign the full editor UI.

## TDD policy for this sprint

This is planned semantic Content/Editor code work and must follow `docs/Source of Truth/testing-charter.md`.

Several phases are behavior-preserving refactors, but each phase must still have at least one testable outcome before production changes. For new compiler/index APIs, write intentionally failing tests first. For pure consumer rewiring, preserve existing invariant-traced behavior and add characterization tests only where the new seam is not otherwise observable.

If implementation discovers pressure to change content semantics, pause this plan and add a semantic-change trace before proceeding.

## Phase 1: Introduce one-document `ContentCompiler`

### Intent

Add a single facade that compiles an `EditableContentDocument` into the current semantic registry plus diagnostics, without changing current document loading or validation semantics.

Candidate shape:

- `ContentCompiler.Compile(EditableContentDocument document, ContentCompileOptions? options = null)`
- `ContentCompileResult`
  - `PrototypeContentRegistry? Registry`
  - `ContentValidationResult Validation`
  - optional `IReadOnlyList<ContentDiagnostic> Diagnostics` shortcut
  - optional `string? DocumentId` / `string? SourcePath` for future attribution

The first version should support exactly one editable document and should internally use the same materialization path as `EditableContentDocument.ToRegistry()`.

### Testable outcomes

- A valid one-document YAML fixture compiles to a non-null `PrototypeContentRegistry` with the same entity/action-plan/scenario-access behavior as `document.ToRegistry()`.
- Registry validation diagnostics produced through the compiler match current `PrototypeContentRegistry.Validate()` diagnostics for representative valid and invalid fixtures.
- Compile failures from invalid DTO/materialization paths are returned as diagnostics rather than forcing every editor consumer to catch exceptions.

### TDD / invariant trace

Affected invariants from `docs/Source of Truth/invariants.md`:

- Content Pipeline:
  - `YAML content loads from strings and files into registries that can be validated.`
  - `Editable content documents round-trip through materialization and saved YAML.`
  - `Built-in content must load and validate, but tests should not pin valid design choices...`

Existing tests associated with those invariants:

- `YamlContentLoaderCreatesRegistryFromDeclarativeContent`
- `YamlContentLoaderCanLoadRegistryFromFile`
- `EditableContentDocumentCanLoadMaterializeSaveAndReloadYaml`
- `PrototypeRegistryValidationPassesForBuiltInContent`
- representative registry diagnostics such as `PrototypeRegistryValidationReportsMissingTemplateDefaultPlan`, `PrototypeRegistryValidationReportsMissingCalledPlan`, and `PrototypeRegistryValidationReportsMissingPresentationAsStructuredDiagnostic`

Existing tests to revise into intentionally failing tests:

- None expected; this phase adds a new facade and should preserve existing paths.

New intentionally failing tests before production changes:

- `ContentCompilerCompilesEditableDocumentToRegistry`
- `ContentCompilerReturnsRegistryValidationDiagnostics`
- `ContentCompilerReturnsMaterializationFailureAsDiagnostic`

## Phase 2: Unify registry and canonical-authoring validation under the compiler

### Intent

Remove the current split-brain validation consumption pattern where callers must know to combine:

- `Document.ToRegistry().Validate()`
- `Document.ValidateCanonicalAuthoring()`

The compiler result should expose the combined validation result for editor-facing validation. Existing `ValidateCanonicalAuthoring()` can remain temporarily as a compatibility wrapper, but the canonical implementation should live behind the compiler/validator pipeline.

### Testable outcomes

- Compiler validation includes canonical-authoring diagnostics for arbitrary legacy variable fields and default plan variables.
- Compiler validation includes scenario/player-control diagnostics currently produced by `ValidateCanonicalAuthoring()`.
- Duplicate diagnostics already produced by both the registry path and canonical path are not emitted twice for the same content problem.
- `FrontendEditorSnapshotBuilder` no longer has to manually concatenate two validation calls to show canonical diagnostics.

### TDD / invariant trace

Affected invariants from `docs/Source of Truth/invariants.md`:

- Content Pipeline:
  - `Content editor operations preserve declared IDs, presentations including semantic presentation/palette IDs and legacy fallback glyph/color, carried layouts, Action Plans/behavior assignments, legacy action plans, and validation results.`
  - `Frontend editor snapshots and service-backed template/action-plan mutations expose authored scenarios... validation diagnostic, YAML preview, diff state... through shared content/editor services...`
  - `Persisted scenario definitions materialize through the shared content materialization path... and report authoring diagnostics before simulation...`
- Action Plan Data:
  - `An Action Plan descriptor has exactly one active authored shape... mixed authored shapes are invalid.`
  - `Empty canonical behavior chains are invalid for authored content...`

Existing tests associated with those invariants:

- `EditableContentDocumentCanonicalAuthoringValidationReportsArbitraryVariableFields`
- `EditableContentDocumentCanonicalAuthoringValidationReportsDefaultPlanVariables`
- `ScenarioValidationReportsMissingControlledEntityReferences`
- `ScenarioValidationReportsInvalidPlayerControlBindingShapes`
- `PrototypeRegistryValidationReportsMixedActionPlanShapes`
- `PrototypeRegistryValidationReportsEmptyBehaviorChain`
- `ContentEditorServiceValidationReportsCurrentDocumentErrors`
- `FrontendEditorServiceTests`

Existing tests to revise into intentionally failing tests:

- Revise or supplement `EditableContentDocumentCanonicalAuthoringValidationReportsArbitraryVariableFields` so the primary assertion goes through `ContentCompiler.Compile(...).Validation` while keeping `ValidateCanonicalAuthoring()` compatibility covered if it remains public.
- Revise or supplement scenario-validation tests to prove compiler validation carries current scenario diagnostics.

New intentionally failing tests before production changes:

- `ContentCompilerIncludesCanonicalAuthoringDiagnostics`
- `ContentCompilerIncludesScenarioAuthoringDiagnostics`
- `FrontendEditorSnapshotUsesCompilerValidationDiagnostics`

## Phase 3: Convert remaining validation string errors to structured diagnostics

### Intent

Reduce `ContentDiagnosticCode.General` usage in validation paths where the subject is known, so future source attribution and editor navigation are possible.

Priority candidates:

- carried entity references to missing templates in `EntityTemplateValidator`;
- merged inventory layer owner/space/connectivity/join diagnostics;
- compile/materialization exceptions that can be tied to action plan, entity template, scenario, or merged layer context.

Do not require perfect source spans yet; the goal is stable structured subject identity.

### Testable outcomes

- Missing carried entity template references produce a `ContentDiagnostic` with a specific code, parent `EntityTemplateId`, carried `EntityId`, and referenced/missing template identity if a suitable field is added.
- Merged inventory layer validation emits structured diagnostics for unknown owner entities and directional conflicts instead of only string `General` errors.
- Existing plain `Errors` strings remain populated from structured diagnostics for compatibility.

### TDD / invariant trace

Affected invariants from `docs/Source of Truth/invariants.md`:

- Content Pipeline:
  - `Content editor operations preserve declared IDs... carried layouts... and validation results.`
  - `Persisted scenario definitions materialize through the shared content materialization path... and report authoring diagnostics before simulation...`
- Entity And Space:
  - `Authored topology policy may add directed inventory-boundary adjacency... Merged inventory aligned joins may add explicit source-cell links... each resolved (source cell, direction) must have zero or one destination.`

Existing tests associated with those invariants:

- `PrototypeRegistryValidationReportsCarriedEntityOutsideInventoryBounds`
- `PrototypeRegistryValidationReportsOverlappingCarriedEntities`
- `PrototypeRegistryValidationReportsDuplicateCarriedEntityIds`
- `PrototypeRegistryValidationReportsCarriedEntitiesOnTemplateWithoutUsableInventory`
- `YamlContentLoaderLoadsAlignedMergedLayerJoin`
- `ContentValidationRejectsMergedLayerJoinDirectionalConflict`
- `ScenarioMaterializerResolvesAlignedMergedLayerJoinsToSourceCellLinks`
- `ContentEditorServiceValidationReportsCurrentDocumentErrors`

Existing tests to revise into intentionally failing tests:

- Revise the existing missing-carried-template assertion if present; otherwise add new coverage because current validation only appends a plain string for that case.
- Revise `ContentValidationRejectsMergedLayerJoinDirectionalConflict` to assert a structured diagnostic code and subject fields while preserving the existing human-readable error text expectation if still useful.

New intentionally failing tests before production changes:

- `PrototypeRegistryValidationReportsMissingCarriedTemplateAsStructuredDiagnostic`
- `ContentValidationReportsMergedLayerUnknownOwnerAsStructuredDiagnostic`
- `ContentValidationReportsMergedLayerJoinDirectionalConflictAsStructuredDiagnostic`

## Phase 4: Add diagnostic attribution fields without source-span mapping

### Intent

Prepare diagnostics for future multi-document/workspace composition by adding optional attribution metadata that can be populated by the compiler without changing validation semantics.

Candidate additions:

- `DocumentId` or `SourceId`
- `SourcePath`
- `SymbolKind`
- `SymbolId`

This phase should not attempt YAML line/column mapping. A synthetic one-document ID is sufficient for this sprint.

### Testable outcomes

- Compiler-produced diagnostics include a stable document/source identity when compile options provide one.
- Existing direct `registry.Validate()` callers remain source-compatible; diagnostics may have null attribution when no compiler context exists.
- Editor/frontend diagnostic DTOs preserve attribution fields if present but do not require them.

### TDD / invariant trace

Affected invariants from `docs/Source of Truth/invariants.md`:

- Content Pipeline:
  - `Content editor operations preserve declared IDs... and validation results.`
  - `Frontend editor snapshots... expose... validation diagnostic... through shared content/editor services...`

Existing tests associated with those invariants:

- `ContentEditorServiceValidationReportsCurrentDocumentErrors`
- `FrontendEditorServiceTests`
- `FrontendEditorServiceAndAgentApiShareContentEditorSessionAsParallelSurfaces`
- `ContentToolDispatcherCreatesBehaviorPlanAndPreviewWithValidationSummary`

Existing tests to revise into intentionally failing tests:

- None expected unless frontend/editor diagnostic DTO constructor signatures change.

New intentionally failing tests before production changes:

- `ContentCompilerAnnotatesDiagnosticsWithDocumentSource`
- `FrontendEditorSnapshotPreservesDiagnosticSourceAttribution`

## Phase 5: Build a one-document symbol/reference index

### Intent

Add compiler-owned symbol and reference discovery for the current one-document model. This is the migration bridge for future automatic dependency linking and type-first editor displays.

Candidate concepts:

- `ContentSymbol`
  - kind: entity template, action plan, scenario, presentation, palette, merged layer, authored entity instance
  - id/name
  - optional source attribution
- `ContentReference`
  - source symbol
  - reference kind: default action plan, carried template, scenario root template, scenario player template, action-plan referenced plan, action-step template, action-step cost template, targeting target template, merged-layer owner, presentation/palette
  - target symbol kind/id
  - resolution status: resolved/missing/ambiguous-reserved-for-future

The index should not create import semantics. Missing references should still be missing within the single compiled document.

### Testable outcomes

- Symbols are grouped by content type regardless of YAML declaration order.
- References are emitted for the existing dependency categories above.
- Missing references are represented in the reference index and correspond to validation diagnostics.
- Editor services can use the index for read-only listing/projection without changing mutation semantics.

### TDD / invariant trace

Affected invariants from `docs/Source of Truth/invariants.md`:

- Content Pipeline:
  - `YAML content loads from strings and files into registries that can be validated.`
  - `Content editor operations preserve declared IDs... Action Plans/behavior assignments... and validation results.`
  - `Frontend editor snapshots... expose authored scenarios, entity templates/carried layouts... action-plan summaries... validation diagnostic... through shared content/editor services...`
- Action Plan Data:
  - `Canonical behavior descriptors and legacy action-plan descriptors preserve structured built-in inputs and materialize executable plans.`
  - `Action Step descriptors may expose non-mutating target-capability affordances... targeting rules can combine target-slot labels... optional target templates... target-capability adjectives...`

Existing tests associated with those invariants:

- `ContentEditorServiceListsJoinedEntityPresets`
- `ContentEditorServiceUpdatesEntityPresetAndPresentation`
- `ContentEditorServiceAddsReordersAndRemovesActionPlanSteps`
- `SnapshotProjectsDefaultActionPlanTargetLabelRequirementsAndOrphanedRules`
- `SnapshotIncludesActionPlanStepTargetReferencesAndTargetConsumptionMetadata`
- `PrototypeRegistryValidationReportsMissingTemplateDefaultPlan`
- `PrototypeRegistryValidationReportsMissingCalledPlan`
- `PrototypeRegistryValidationReportsMissingApplyPrePlanReference`
- `PrototypeRegistryValidationReportsMissingCreateEntityTemplateReference`
- `PrototypeRegistryValidationReportsUnknownCostTemplate`
- `ScenarioValidationReportsMissingControlledEntityReferences`

Existing tests to revise into intentionally failing tests:

- None expected for existing behavior. Add new index tests first; then optionally route read-only editor listings through the index.

New intentionally failing tests before production changes:

- `ContentCompilerBuildsSymbolsGroupedByType`
- `ContentCompilerIndexesTemplateActionPlanAndScenarioReferences`
- `ContentCompilerIndexesBehaviorStepTemplatePlanAndCostReferences`
- `ContentCompilerMarksMissingReferencesWithoutResolvingAcrossFiles`

## Phase 6: Route shared Content/editor consumers through the compiler seam

### Intent

Update high-value consumers so the compiler becomes the normal path for validation and read-only semantic projections, while retaining compatibility wrappers where needed.

Priority consumers:

1. `ContentEditorService.Validate()`
2. `FrontendEditorSnapshotBuilder`
3. `ActionPlanPreviewService`
4. `ScenarioMaterializer`
5. `AgentContentEditorApi.Validate()` / content tool validation result paths

This phase should be incremental; do not churn mutation services if they are not blocked by validation ownership.

### Testable outcomes

- Editor, frontend snapshot, preview, scenario materialization, and agent validation report the same diagnostics as before plus intentional compiler additions from earlier phases.
- Scenario materialization still consumes the same materialized registry/world setup semantics.
- Existing editor mutation tests pass unchanged.

### TDD / invariant trace

Affected invariants from `docs/Source of Truth/invariants.md`:

- Content Pipeline:
  - `Content editor operations preserve declared IDs... and validation results.`
  - `Frontend editor snapshots and service-backed template/action-plan mutations expose... validation diagnostic... YAML preview, diff state... through shared content/editor services...`
  - `Persisted scenario definitions materialize through the shared content materialization path... and report authoring diagnostics before simulation...`
  - `Playable frontend sessions launch through a shared Content-level session launcher that consumes scenario materialization outputs...`
- Scenario Tooling:
  - `Scenario runs use shared Content/Core services...`
  - `Scenario run reports expose setup... validation diagnostics... runtime observations...`

Existing tests associated with those invariants:

- `ContentEditorServiceValidatesCurrentDocumentAfterEdits`
- `ContentEditorServiceValidationReportsCurrentDocumentErrors`
- `FrontendEditorServiceTests`
- `FrontendEditorServiceAndAgentApiShareContentEditorSessionAsParallelSurfaces`
- `AgentContentEditorApiRunsPersistedScenarioById`
- `AgentContentEditorApiCreatesCombinedPersistedScenarioReport`
- `ActionPlanPreviewService` coverage through `ContentToolDispatcherCreatesBehaviorPlanAndPreviewWithValidationSummary`
- `ScenarioMaterializerReportsAuthoringDiagnostics`
- `ScenarioMaterializerValidatesPersistedAlphaScenarioDefinitions`
- `PlayableScenarioLauncherBuildsFrontendNeutralSessionFromPersistedScenario`
- `ScenarioRunServiceRunsPersistedScenarioByIdWithInsertedPlayer`

Existing tests to revise into intentionally failing tests:

- Revise `ContentEditorServiceValidationReportsCurrentDocumentErrors` to assert the service result is compiler-produced if that is visible through source attribution or a compiler-only diagnostic.
- Revise a scenario materializer diagnostic test to assert canonical compiler diagnostics are included without manual double validation.

New intentionally failing tests before production changes:

- `ContentEditorServiceValidateUsesContentCompiler`
- `ScenarioMaterializerUsesCompilerValidationResult`
- `ActionPlanPreviewUsesCompilerValidationResult`

## Validation commands

Recommended targeted commands as phases progress:

```powershell
dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~ContentCompiler|FullyQualifiedName~PrototypeRegistryValidation|FullyQualifiedName~EditableContentDocumentCanonicalAuthoringValidation"
dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~ContentEditorService|FullyQualifiedName~FrontendEditorService|FullyQualifiedName~ActionPlanPreview|FullyQualifiedName~ScenarioMaterializer"
dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"
```

Recommended broader validation before sprint close:

```powershell
dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj
```

If compiler changes affect SadConsole-facing DTOs, also run relevant SadConsole tests/builds selected by frontend-owner guidance.

## Completion checklist

- A one-document `ContentCompiler` exists and is the preferred validation/compile entry point.
- Compiler validation includes registry diagnostics and canonical-authoring diagnostics.
- High-value previously string-only validation paths now emit structured diagnostics.
- Diagnostics can carry optional document/source attribution without requiring line/column source mapping.
- A one-document symbol/reference index exists and proves type-first grouping plus dependency discovery without import semantics.
- Content editor, frontend snapshot, scenario materialization, preview, and agent validation paths use the compiler seam where practical.
- No multi-file/import/package/shipped-vs-user content behavior is implemented.
- Existing YAML schema and authored content files remain unchanged unless a separately approved doc/content-only cleanup is selected.
- Targeted and broad Content tests pass.
- Update `docs/Source of Truth/invariants.md` only if test names or stable behavior traces change; update `Engine-Editor-Capabilities.md` only if actual support status changes.

## Follow-up after this sprint

After this migration sprint, a later plan may evaluate workspace/multi-document composition. That later plan should start from the compiler result, diagnostics attribution, and symbol/reference index created here rather than adding imports directly to `YamlContentLoader`, `EditableContentDocument`, or individual validators.

## Phase log

Use this section to record completed turns, verification commands, friction discovered during implementation, and mitigation strategies.

### Phase 1 turn 1: One-document compiler facade

- Added `ContentCompiler.Compile(EditableContentDocument)` as the first one-document compiler entry point.
- Added `ContentCompileResult` carrying the compiled `PrototypeContentRegistry?`, unified-for-now `ContentValidationResult`, and a diagnostics shortcut.
- Current Phase 1 behavior intentionally mirrors existing registry validation: successful compiles call `document.ToRegistry()` and `registry.Validate()`; materialization exceptions are converted into a structured `General` diagnostic instead of escaping to editor consumers.
- Added TDD coverage:
  - `ContentCompilerCompilesEditableDocumentToRegistry`;
  - `ContentCompilerReturnsRegistryValidationDiagnostics`;
  - `ContentCompilerReturnsMaterializationFailureAsDiagnostic`.
- Confirmed the intended red step before implementation: the new tests initially failed to compile because `ContentCompiler` did not exist.
- Friction: the first valid compiler fixture authored a behavior-chain step `Wait`, but `Wait` is a legacy low-level effect, not an `ActionPlanBehaviorStepKind`, so YAML deserialization failed before exercising the compiler. Mitigation: changed the fixture to use existing behavior-chain step `MoveFacing`, which is accepted by current validation defaults and keeps the test focused on the compiler seam.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~ContentCompiler|FullyQualifiedName~PrototypeRegistryValidationPassesForBuiltInContent|FullyQualifiedName~YamlContentLoaderCreatesRegistryFromDeclarativeContent|FullyQualifiedName~EditableContentDocumentCanLoadMaterializeSaveAndReloadYaml"` passed: 6 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed: 286 tests.
- Recommended next step: proceed to Phase 2 by moving combined registry + canonical-authoring validation behind `ContentCompiler.Compile(...)`, starting with failing tests for compiler-surfaced arbitrary variable diagnostics and scenario/player-control diagnostics.

### Phase 2 turn 1: Combined compiler validation

- Updated `ContentCompiler.Compile(...)` so successful compiles combine `registry.Validate()` diagnostics with `document.ValidateCanonicalAuthoring()` diagnostics and de-duplicate identical diagnostics.
- Updated `FrontendEditorSnapshotBuilder` to consume `ContentCompiler.Compile(session.Document).Validation` instead of manually concatenating registry validation plus canonical-authoring validation.
- Added TDD coverage:
  - `ContentCompilerIncludesCanonicalAuthoringDiagnostics`;
  - `ContentCompilerIncludesScenarioAuthoringDiagnostics`;
  - `ContentCompilerDeduplicatesRegistryAndCanonicalAuthoringDiagnostics`;
  - `FrontendEditorSnapshotUsesCompilerValidationDiagnostics`.
- Confirmed the intended red step before implementation:
  - compiler canonical/scenario tests failed because Phase 1 compiler only returned registry diagnostics;
  - frontend snapshot test failed because the old snapshot builder emitted duplicate mixed-shape diagnostics from two validation calls.
- Friction: no implementation friction beyond the expected duplicate-diagnostic behavior that the phase was designed to remove. Mitigation used: de-duplicate combined diagnostics with record equality at the compiler boundary, preserving existing diagnostic shapes.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~ContentCompiler|FullyQualifiedName~FrontendEditorSnapshotUsesCompilerValidationDiagnostics|FullyQualifiedName~EditableContentDocumentCanonicalAuthoringValidation|FullyQualifiedName~ScenarioValidationReports"` passed: 11 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~ContentCompiler|FullyQualifiedName~PrototypeRegistryValidation|FullyQualifiedName~EditableContentDocumentCanonicalAuthoringValidation|FullyQualifiedName~ContentEditorServiceValidation|FullyQualifiedName~FrontendEditorService"` passed: 140 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed: 290 tests.
- Recommended next step: proceed to Phase 3 by converting the highest-value remaining string-only validation errors into structured diagnostics, starting with missing carried-template references and merged-layer directional-conflict diagnostics.

### Phase 3 turn 1: Structured carried-template and merged-layer diagnostics

- Added structured diagnostic support for two high-value formerly string-heavy validation areas:
  - missing carried entity template references now use `ContentDiagnosticCode.MissingCarriedEntityTemplateReference` with source `EntityTemplateId`, `CarriedEntityId`, and `ReferencedEntityTemplateId`;
  - merged inventory layer validation now uses `ContentDiagnosticCode.InvalidMergedInventoryLayer` with `MergedInventoryLayerId` and related subject fields where available.
- Extended `ContentDiagnostic` with optional `ReferencedEntityTemplateId` and `MergedInventoryLayerId` fields for compiler/editor navigation readiness without adding source-span mapping yet.
- Converted merged-layer validation messages for too few spaces, unknown owners, unusable owner inventories, disconnected layers, join resolver errors, and directional conflicts from plain `errors` strings to structured diagnostics. The compatibility `Errors` projection still exposes their human-readable messages.
- Added/revised TDD coverage:
  - `PrototypeRegistryValidationReportsMissingCarriedTemplateAsStructuredDiagnostic`;
  - `ContentValidationReportsMergedLayerUnknownOwnerAsStructuredDiagnostic`;
  - `ContentValidationRejectsMergedLayerJoinDirectionalConflict` now asserts the structured merged-layer diagnostic as well as the message.
- Confirmed the intended red step before implementation: new tests initially failed to compile because the new diagnostic codes/fields did not exist.
- Friction: merged-layer diagnostics did not have an existing subject slot on `ContentDiagnostic`, and overloading existing entity/action-plan fields would have made future editor navigation ambiguous. Mitigation used: added narrow optional diagnostic fields for referenced entity-template IDs and merged-layer IDs, keeping existing constructor/static-factory parameters source-compatible by appending optional parameters.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~PrototypeRegistryValidationReportsMissingCarriedTemplateAsStructuredDiagnostic|FullyQualifiedName~ContentValidationRejectsMergedLayerJoinDirectionalConflict|FullyQualifiedName~ContentValidationReportsMergedLayerUnknownOwnerAsStructuredDiagnostic"` passed: 3 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~PrototypeRegistryValidationReportsCarried|FullyQualifiedName~PrototypeRegistryValidationReportsMissingCarried|FullyQualifiedName~ContentValidation|FullyQualifiedName~YamlContentLoaderLoadsAlignedMergedLayerJoin|FullyQualifiedName~ScenarioMaterializerResolvesAlignedMergedLayerJoinsToSourceCellLinks|FullyQualifiedName~ContentCompiler"` passed: 19 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed: 292 tests.
- Recommended next step: proceed to Phase 4 by adding compiler-provided diagnostic attribution metadata (`DocumentId`/`SourcePath` style fields) without attempting YAML line/column source-span mapping.

### Phase 4 turn 1: Diagnostic source attribution

- Added `ContentCompileOptions` with optional `DocumentId` and `SourcePath`.
- Extended `ContentDiagnostic` with optional `DocumentId`, `SourcePath`, `SymbolKind`, and `SymbolId` fields. Phase 4 populates document/source fields only; symbol fields are reserved for Phase 5 symbol/reference indexing.
- Updated `ContentCompiler.Compile(...)` to apply compile-option attribution to successful validation diagnostics and materialization-failure diagnostics.
- Updated `FrontendEditorSnapshotBuilder` to pass the open session file path as both document ID and source path when building compiler validation for file-backed sessions.
- Extended `FrontendEditorDiagnostic` to preserve `DocumentId` and `SourcePath` for frontend/editor consumers.
- Added TDD coverage:
  - `ContentCompilerAnnotatesDiagnosticsWithDocumentSource`;
  - `FrontendEditorSnapshotPreservesDiagnosticSourceAttribution`.
- Confirmed the intended red step before implementation: tests initially failed to compile because `ContentCompileOptions`, diagnostic attribution fields, and frontend diagnostic attribution fields did not exist.
- Friction: no unexpected friction. Mitigation/design choice: keep attribution optional and compiler-applied so direct `registry.Validate()` callers remain source-compatible and diagnostics outside compiler context continue to have null attribution.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~ContentCompilerAnnotatesDiagnosticsWithDocumentSource|FullyQualifiedName~FrontendEditorSnapshotPreservesDiagnosticSourceAttribution"` passed: 2 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~ContentCompiler|FullyQualifiedName~FrontendEditorService|FullyQualifiedName~ContentToolDispatcherCreatesBehaviorPlanAndPreviewWithValidationSummary"` passed: 90 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed: 294 tests.
- Recommended next step: proceed to Phase 5 by adding the one-document symbol/reference index, using the reserved symbol attribution fields only where the index can assign stable symbol kind/ID facts without inventing multi-file resolution.

### Phase 5 turn 1: One-document symbol/reference index

- Added compiler-owned one-document symbol/reference indexing to `ContentCompileResult`:
  - `ContentSymbol`, `ContentSymbolKind`;
  - `ContentReference`, `ContentReferenceKind`, `ContentReferenceResolution`.
- The compiler now emits symbols for entity templates, action plans, scenarios, presentations, presentation definitions, palettes, merged inventory layers, and authored carried entity instances.
- The compiler now emits references for current one-document dependency categories:
  - template default action plans;
  - carried entity templates;
  - scenario root/player templates;
  - behavior-step referenced plans, spawned/polymorphed templates, and cost templates;
  - legacy low-level `CallPlan` effect references;
  - targeting target templates;
  - merged-layer owner entity references;
  - presentation-to-template and presentation/palette identity references.
- Missing references are marked `Missing` within the current document instead of attempting cross-file/import resolution. `Ambiguous` is reserved for a future workspace/import phase and is not emitted by this one-document compiler.
- Added TDD coverage:
  - `ContentCompilerBuildsSymbolsGroupedByType`;
  - `ContentCompilerIndexesTemplateActionPlanAndScenarioReferences`;
  - `ContentCompilerIndexesBehaviorStepTemplatePlanAndCostReferences`;
  - `ContentCompilerMarksMissingReferencesWithoutResolvingAcrossFiles`.
- Confirmed the intended red step before implementation: new tests initially failed to compile because symbol/reference result properties and vocabulary did not exist.
- Friction: merged-layer owner references target authored entity instances, while the current one-document index does not yet resolve authored instance IDs recursively or validate merged-layer owner existence through the index. Mitigation used: emit stable authored entity instance symbols for direct carried instances and keep merged-layer owner references one-document/factual without changing current validation semantics; deeper recursive provenance can be promoted with workspace/index consumers if needed.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~ContentCompilerBuildsSymbolsGroupedByType|FullyQualifiedName~ContentCompilerIndexes|FullyQualifiedName~ContentCompilerMarksMissingReferences"` passed: 4 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~ContentCompiler|FullyQualifiedName~ContentEditorService|FullyQualifiedName~FrontendEditorService|FullyQualifiedName~PrototypeRegistryValidation"` passed: 191 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed: 298 tests.
- Recommended next step: proceed to Phase 6 by routing high-value shared Content/editor consumers through the compiler seam where practical, starting with `ContentEditorService.Validate()`, `ActionPlanPreviewService`, `ScenarioMaterializer`, and agent validation paths while preserving current mutation semantics.

### Phase 6 turn 1: Shared consumer compiler routing

- Routed high-value shared consumers through `ContentCompiler` while preserving mutation semantics:
  - `ContentEditorService.Validate()` now returns `ContentCompiler.Compile(Document).Validation`;
  - `ActionPlanPreviewService` now gets registry and validation diagnostics from the compiler result for normal preview flows;
  - `ScenarioMaterializer` now compiles once up front, consumes the compiler registry when available, and returns compiler validation diagnostics when compile/materialization fails.
- Agent validation paths that call `Session.Editor.Validate()` now inherit the compiler validation seam through `ContentEditorService` without changing the agent API result shape.
- Added TDD coverage:
  - `ContentEditorServiceValidateUsesContentCompiler`;
  - `ScenarioMaterializerUsesCompilerValidationResult`.
- Confirmed the intended red step before implementation:
  - `ContentEditorServiceValidateUsesContentCompiler` failed because editor validation still returned only registry diagnostics and missed canonical-authoring diagnostics;
  - `ScenarioMaterializerUsesCompilerValidationResult` failed because materialization still performed its own `ToRegistry()` catch path with the old `content registry could not be materialized` message instead of compiler diagnostics.
- Friction: `ActionPlanPreviewService` can only use compiler diagnostics when the compiler can produce a registry; invalid DTO/materialization failures still leave no semantic action-plan registry to preview. Mitigation used: keep the fallback `document.ToRegistry()` behavior for the rare null-registry preview path rather than widening preview semantics in this phase. A later phase can design graceful preview over raw DTOs if needed.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~ContentEditorServiceValidateUsesContentCompiler|FullyQualifiedName~ScenarioMaterializerUsesCompilerValidationResult"` passed: 2 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~ContentCompiler|FullyQualifiedName~ContentEditorService|FullyQualifiedName~ActionPlanPreview|FullyQualifiedName~ScenarioMaterializer|FullyQualifiedName~AgentContentEditorApi"` passed: 97 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed: 299 tests.
- Recommended next step: close this migration sprint with a cleanup/review pass: inspect the compiler/index surface for naming/API polish, decide whether `ContentReferenceIndex` should move out of `ContentCompiler.cs` before further growth, and update source-of-truth docs only if the compiler seam is considered an editor/content capability status change.
