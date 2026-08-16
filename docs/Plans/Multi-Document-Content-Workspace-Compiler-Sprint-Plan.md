---
id: plan.multi-document-content-workspace-compiler-sprint
title: Multi-Document Content Workspace Compiler Sprint Plan
kind: plan
status: active
truth_rank: 45
truth_domains: [planning-priority, implementation-navigation, test-trace]
owners: [core-owner]
audience: [core-owner, content-editor, frontend-owner]
read_when:
  - implementing multi-document ContentWorkspace or ContentCompiler behavior
  - changing Content validation, materialization, scenario launch, or editor APIs to consume composed content
  - designing shipped canonical content plus separate player/user-authored content workflows
related:
  - source.invariants
  - source.testing-charter
  - source.engine-editor-capabilities
  - source.vertical-slice-map
  - source.content-authoring-manual
  - plan.high-level-roadmap
  - plan.content-validation-compiler-migration-sprint
  - plan.content-workspace-surface-implementation
---

# Multi-Document Content Workspace Compiler Sprint Plan

Status: Active sprint plan for moving Content from a one-document compiler/surface to a multi-document workspace compiler suitable for scenario composition, canonical shipped content, and separate user-authored content.

Archived runway/context:

- `docs/Archived/Content-Validation-Compiler-Migration-Sprint-Plan.md` introduced the one-document `ContentCompiler`, structured diagnostics, document/source attribution, and one-document symbol/reference indexing.
- `docs/Archived/Content-Workspace-Surface-Implementation-Plan.md` introduced the one-document compiler-backed type-first surface and scenario/reference projections.

## Directional decision

Do **not** rewrite the entire `GameGameGame.Content` project. Instead:

1. introduce a new `ContentWorkspace` / multi-document `ContentCompiler` subsystem;
2. gradually route validation, materialization, scenario launch, editor services, and agent APIs through it;
3. keep existing one-document `EditableContentDocument` and editor/session APIs as compatibility adapters until all consumers are safely migrated;
4. preserve document ownership and save boundaries even when scenarios compile against composed content.

The target authoring model is compiler-like: files are editable ownership units; the workspace is the validation/composition boundary; scenarios materialize against composed workspace content.

## Goal

Enable scenario authorship and validation across multiple content documents without making source file layout the semantic model.

The sprint should establish:

1. loading/compiling multiple `EditableContentDocument` inputs as one `ContentWorkspace`;
2. stable document IDs, source paths, source classes, read-only/writable policy, dirty/save ownership, and source-aware diagnostics;
3. a workspace symbol table over entity templates, action plans, scenarios, presentations, palettes, merged layers, and other authored declarations already indexed by the one-document compiler;
4. explicit duplicate/conflict policy for symbol declarations;
5. cross-document typed reference resolution that reports missing and ambiguous references without guessing by load order;
6. composed validation and scenario materialization/launch paths that can consume definitions from the whole workspace;
7. compatibility entry points for current one-document workflows.

## Non-goals

- Do **not** rewrite all Content services, tools, or YAML loaders at once.
- Do **not** reorganize canonical/prototype YAML files as part of the first compiler slice unless a later content-editor-authored scenario task explicitly selects that work.
- Do **not** implement implicit overrides or load-order-wins semantics.
- Do **not** require every document to be standalone-valid outside its workspace.
- Do **not** make canonical/user folder layout rules hard schema errors during this sprint.
- Do **not** require one scenario per file, one type per file, or automatic duplicate renaming/reordering/canonicalization.
- Do **not** make scenario manifests the only authority for workspace composition; they remain curated browsing/packaging metadata unless a later phase explicitly promotes them.
- Do **not** add line/column source jumps unless source spans are implemented and test-backed.
- Do **not** expose mutable multi-document edit/save operations through agent APIs or frontend editor workflows before the consultation gates in this plan.

## Design principles

- **Document = edit/save boundary.** A document owns declarations and receives edits for its owned symbols.
- **Workspace = compile/validation boundary.** Scenario materialization and reference resolution should use the composed workspace, not a single file.
- **Symbols are typed.** Symbol keys should include kind and ID, such as `entityTemplate:Slime` or `actionPlan:Slime_Default`.
- **References are typed.** Resolution should know the expected target kind and never rely on display names.
- **No implicit override.** Duplicate same-kind IDs across documents are conflicts unless a future explicit override/extension syntax is designed.
- **Load order is not semantics.** Load order may make reports deterministic, but must not decide which symbol wins.
- **Read-only canonical content is normal dependency input.** Canonical shipped content can be referenced and compiled, but normal authoring operations cannot mutate it.
- **Organization standards begin advisory.** Promotion/category rules for canonical content should initially surface as guidance or warnings, not blockers, until curation workflows exist.

## Content-editor constraints captured before planning

Content-editor consultation for this plan captured these constraints:

- preserve the current authoring loop: open/create session, inspect/list, semantic edit, validate/canonical-validate, preview/materialize/run when relevant, review snapshot diff, save deliberately;
- preserve source ownership: edits and saves affect only the document/session that owns the symbol, never silently rewrite composed/canonical dependency files;
- expose provenance so authors know which file owns a template/action plan/scenario and where a save will go;
- distinguish canonical, delta/working, user, and compatibility/legacy content in projections/manifests without making source file layout the primary model;
- promotion into canonical should be deliberate curation, not an ordinary save side effect;
- cross-document references resolve only when the workspace can prove a unique target;
- distinguish authoring blockers from advisory organization warnings.

## Recommended implementation order

### Phase 1: Workspace document identity and one-document adapter

#### Intent

Introduce the workspace shell while preserving current one-document behavior.

Candidate shape:

- `ContentDocumentSource` / `ContentWorkspaceDocument` metadata:
  - `DocumentId`;
  - `SourcePath` when file-backed;
  - source class/kind, initially such as `Unknown`, `Canonical`, `User`, `Generated`, `Test`, `Compatibility`;
  - read-only/writable policy;
  - load order for deterministic reporting only;
  - dirty/save ownership facts where available.
- `ContentWorkspace` containing ordered loaded documents plus workspace diagnostics.
- `ContentWorkspaceCompiler.Compile(ContentWorkspace workspace, ContentCompileOptions? options = null)` or an equivalent extension of `ContentCompiler`.
- A one-document adapter so existing `ContentCompiler.Compile(EditableContentDocument)` continues to behave as before by compiling a workspace with one document.

#### Testable outcomes

- A workspace can compile one document and produce validation diagnostics equivalent to the existing one-document compiler.
- Workspace document metadata is carried into diagnostics and surface/source summaries.
- Source path is not required for transient/test documents.
- Read-only metadata is represented but does not yet change mutation behavior except where explicitly tested.

#### TDD / invariant trace

Affected invariants from `docs/Source of Truth/invariants.md`:

- Content Pipeline:
  - YAML content loads from strings and files into registries that can be validated.
  - Editable content documents round-trip through materialization and saved YAML.
  - Content editor operations preserve declared IDs, presentations, carried layouts, action plans, and validation results.

Existing tests to preserve/run:

- `ContentCompilerReturnsRegistryValidationDiagnostics`
- `ContentCompilerAnnotatesDiagnosticsWithDocumentSource`
- `ContentEditorServiceValidateUsesContentCompiler`
- `EditableContentDocumentCanLoadMaterializeSaveAndReloadYaml`
- `YamlContentLoaderCreatesRegistryFromDeclarativeContent`

New intentionally failing tests before production changes:

- `ContentWorkspaceCompilerCompilesSingleDocumentThroughCompatibilityAdapter`
- `ContentWorkspaceCompilerCarriesDocumentIdentityAndSourcePath`
- `ContentWorkspaceCompilerAllowsTransientDocumentWithoutSourcePath`

#### Phase 1 implementation log

- 2026-08-11: Added the initial `ContentWorkspace`, `ContentWorkspaceDocument`, `ContentWorkspaceDocumentSummary`, and `ContentWorkspaceSourceKind` types; added `ContentCompileResult.WorkspaceDocuments`; routed `ContentCompiler.Compile(EditableContentDocument, ...)` through a one-document workspace adapter; added `ContentCompiler.Compile(ContentWorkspace, ...)` for the Phase 1 one-document workspace shell. Multi-document compilation deliberately returns a structured not-yet-implemented diagnostic until Phase 2/3 define symbol merge and reference resolution semantics.
- Verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~ContentWorkspaceCompiler|FullyQualifiedName~ContentCompiler|FullyQualifiedName~ContentWorkspaceSurface|FullyQualifiedName~ContentEditorServiceValidateUsesContentCompiler|FullyQualifiedName~EditableContentDocumentCanLoadMaterializeSaveAndReloadYaml|FullyQualifiedName~YamlContentLoaderCreatesRegistryFromDeclarativeContent"` passed 21 tests.

#### Phase 1 friction log

- 2026-08-11: Initial red test run was blocked by a stale `GameGameGame.Content.Tools` process holding `GameGameGame.Core.dll` and `GameGameGame.Content.dll` in `src/GameGameGame.Content.Tools/bin/Debug/net10.0`. Mitigation used: stopped process ID `25644`, reran the intentionally failing test command, confirmed the expected compile failures for missing workspace types, then implemented the Phase 1 production slice.

### Phase 2: Workspace symbol table and duplicate conflict diagnostics

#### Intent

Promote the existing one-document symbol index to a workspace-level symbol table with source ownership facts.

Conflict policy for this sprint:

- same symbol kind + same stable ID in more than one document is a workspace error;
- same display/presentation name is not a symbol conflict unless it is also the stable ID for a symbol kind;
- same ID across different symbol kinds is allowed if all typed references remain unambiguous;
- canonical/user duplicate IDs are errors, not overrides.

#### Testable outcomes

- Workspace symbols are grouped by type across documents.
- Each symbol records its declaring document ID and source path where available.
- Duplicate same-kind IDs across documents produce structured diagnostics with related source facts.
- Duplicate IDs across different kinds do not conflict unless an existing validator already rejects that shape.

#### TDD / invariant trace

Affected invariants:

- Content Pipeline validation/results preservation.
- Action Plan Data descriptor/reference preservation.

Existing tests to preserve/run:

- `ContentCompilerBuildsSymbolsGroupedByType`
- `ContentCompilerIndexesTemplateActionPlanAndScenarioReferences`
- `ContentWorkspaceSurfaceGroupsContentByType`
- `ContentWorkspaceSurfaceCarriesCompilerDiagnosticsAndSource`

New intentionally failing tests before production changes:

- `ContentWorkspaceCompilerBuildsSymbolsAcrossDocuments`
- `ContentWorkspaceCompilerReportsDuplicateSameKindSymbolsAcrossDocuments`
- `ContentWorkspaceCompilerAllowsSameIdAcrossDifferentSymbolKinds`

#### Phase 2 implementation log

- 2026-08-11: Extended multi-document workspace compilation to compile each document independently, aggregate symbols and references, preserve declaring document attribution, and emit `DuplicateSymbolDeclaration` diagnostics for duplicate same-kind symbol IDs across workspace documents. Same IDs across different symbol kinds remain allowed. The composed runtime registry remains `null` for multi-document workspaces until Phase 4 defines merge/materialization behavior.
- Verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~ContentWorkspaceCompiler|FullyQualifiedName~ContentCompiler|FullyQualifiedName~ContentWorkspaceSurface|FullyQualifiedName~ContentReferenceQuery|FullyQualifiedName~ContentValidationRejectsMergedLayerJoinDirectionalConflict|FullyQualifiedName~PrototypeRegistryValidationReportsMissingCalledPlan|FullyQualifiedName~PrototypeRegistryValidationReportsMissingCreateEntityTemplateReference"` passed 29 tests.

#### Phase 2 friction log

- 2026-08-11: Duplicate-symbol diagnostics correctly surfaced both duplicated entity-template IDs and the matching duplicated presentation IDs in the two-document fixture. Mitigation: narrowed the duplicate entity-template test assertion to `SymbolKind == EntityTemplate` while preserving the broader compiler behavior that any same-kind duplicate symbol, including presentations, is a conflict.

### Phase 3: Cross-document typed reference resolution

#### Intent

Resolve references against the workspace symbol table instead of the declaring document alone.

References should be resolved only when the compiler knows the expected target kind and finds exactly one matching symbol. Missing or ambiguous references should become structured diagnostics/reference facts, not text parsing requirements.

#### Testable outcomes

- Entity template default action-plan references can resolve to action plans in another document.
- Scenario root/player template references can resolve to entity templates in another document.
- Presentation/palette references can resolve across documents and still report unknown IDs as missing.
- Behavior-step plan/template/cost references can resolve across documents where the current one-document compiler already knows the reference kind.
- Ambiguous references report all candidate declarations and do not choose by load order.

#### TDD / invariant trace

Affected invariants:

- Content Pipeline persisted scenario materialization references normal content templates and reports authoring diagnostics.
- Action Plan Data descriptors preserve structured inputs and references.

Existing tests to preserve/run:

- `ContentCompilerIndexesTemplateActionPlanAndScenarioReferences`
- `ContentCompilerIndexesBehaviorStepTemplatePlanAndCostReferences`
- `ContentCompilerMarksMissingReferencesWithoutResolvingAcrossFiles`
- `ContentCompilerMarksUnknownPresentationAndPaletteReferencesMissing`
- `PrototypeRegistryValidationReportsMissingCalledPlan`
- `ScenarioMaterializerReportsAuthoringDiagnostics`

New intentionally failing tests before production changes:

- `ContentWorkspaceCompilerResolvesTemplateDefaultActionPlanAcrossDocuments`
- `ContentWorkspaceCompilerResolvesScenarioRootAndPlayerTemplatesAcrossDocuments`
- `ContentWorkspaceCompilerResolvesPresentationAndPaletteAcrossDocuments`
- `ContentWorkspaceCompilerReportsAmbiguousReferencesAcrossDocuments`

#### Phase 3 implementation log

- 2026-08-11: Added workspace-level typed reference resolution over aggregated symbols. Multi-document compile now rewrites reference facts to `Resolved` when exactly one target symbol of the expected kind exists anywhere in the workspace, leaves references missing when no target exists, and marks references `Ambiguous` when more than one same-kind target exists. Added `AmbiguousSymbolReference` diagnostics for ambiguous references. Locally reported missing-reference diagnostics from one-document validation are filtered out when the corresponding workspace reference resolves uniquely; duplicate symbol diagnostics remain independent conflicts.
- Verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~ContentWorkspaceCompiler|FullyQualifiedName~ContentCompiler|FullyQualifiedName~PrototypeRegistryValidationReportsMissingCalledPlan|FullyQualifiedName~PrototypeRegistryValidationReportsMissingCreateEntityTemplateReference|FullyQualifiedName~ScenarioMaterializerReportsAuthoringDiagnostics|FullyQualifiedName~ContentCompilerMarksUnknownPresentationAndPaletteReferencesMissing|FullyQualifiedName~ContentCompilerMarksMissingReferencesWithoutResolvingAcrossFiles"` passed 26 tests.

#### Phase 3 friction log

- 2026-08-11: One-document registry/canonical validation emits local missing-reference diagnostics before workspace-level resolution exists, so cross-document references initially produced both `Resolved` reference facts and stale local missing diagnostics. Mitigation: after workspace reference resolution, filter only recognized local missing-reference diagnostics whose same-document typed reference has resolved uniquely. This preserves missing diagnostics for true misses and keeps ambiguity conflicts explicit instead of treating them as resolved.

### Phase 4: Composed validation registry and scenario materialization

#### Intent

Make scenarios compile/materialize against the composed workspace while preserving existing one-document compatibility behavior.

Candidate behavior:

- `ContentWorkspaceCompileResult` exposes a composed registry/catalog for consumers that need runtime materialization.
- Scenario lookup by ID searches the workspace scenario symbols and errors on missing/duplicate scenario IDs.
- Existing one-document scenario materializer methods remain compatibility adapters.

#### Testable outcomes

- A scenario document can materialize using root/player/action-plan/entity definitions from dependency documents.
- Missing references are reported before materialization and include declaring/reference document facts.
- Duplicate scenario IDs block materialization with conflict diagnostics.
- Existing one-document scenario materialization tests pass unchanged or are intentionally routed through the one-document adapter.

#### TDD / invariant trace

Affected invariants:

- Content Pipeline persisted scenario definitions materialize through shared content materialization path and reference normal content templates.
- Scenario Tooling scenario runs use shared Content/Core services.

Existing tests to preserve/run:

- `ScenarioMaterializerPersistsAndMaterializesAlphaScenarioDefinitionById`
- `ScenarioMaterializerValidatesPersistedAlphaScenarioDefinitions`
- `PlayableScenarioLauncherBuildsFrontendNeutralSessionFromPersistedScenario`
- `ScenarioRunServiceRunsPersistedScenarioByIdWithInsertedPlayer`
- `AgentContentEditorApiCreatesCombinedPersistedScenarioReport`

New intentionally failing tests before production changes:

- `ScenarioMaterializerMaterializesPersistedScenarioFromWorkspaceDependencies`
- `ScenarioMaterializerReportsWorkspaceMissingReferencesWithSourceDocuments`
- `PlayableScenarioLauncherBuildsSessionFromWorkspaceScenario`

#### Phase 4 implementation log

- 2026-08-11: Added composed workspace registry creation for valid multi-document workspaces by merging document DTO declarations into an in-memory composed `EditableContentDocument` after duplicate/conflict and workspace reference diagnostics are evaluated. Added `ScenarioMaterializer.Materialize(ContentWorkspace, scenarioId)` and `PlayableScenarioLauncher.CreateFromWorkspace(...)` compatibility-style entry points. Workspace scenario materialization now looks up the selected scenario from the composed document, uses the composed registry for runtime materialization, and reports workspace diagnostics with document ID/source path context when composition is invalid.
- Verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~ContentWorkspaceCompiler|FullyQualifiedName~ScenarioMaterializerPersistsAndMaterializesAlphaScenarioDefinitionById|FullyQualifiedName~ScenarioMaterializerValidatesPersistedAlphaScenarioDefinitions|FullyQualifiedName~PlayableScenarioLauncherBuildsFrontendNeutralSessionFromPersistedScenario|FullyQualifiedName~ScenarioRunServiceRunsPersistedScenarioByIdWithInsertedPlayer|FullyQualifiedName~AgentContentEditorApiCreatesCombinedPersistedScenarioReport|FullyQualifiedName~ScenarioMaterializerReportsAuthoringDiagnostics"` passed 19 tests.

#### Phase 4 friction log

- 2026-08-11: Phase 2 tests had intentionally asserted that multi-document compile did not yet produce a registry. Phase 4 correctly changes that behavior for valid workspaces. Mitigation: revised the Phase 2 aggregation test to assert a non-null registry while keeping the symbol/source attribution assertions unchanged.

### Phase 5: Protected/source ownership policy for editor services

#### Intent

Enforce the first safe source ownership and protection-policy rules needed for canonical shipped content plus separate user-authored content, without making protected/read-only content intrinsically immutable to trusted curation tools.

Candidate behavior:

- normal edits route to the declaring document and preserve source ownership;
- shared editor services expose an explicit mutation policy, with a frontend-safe/default policy that rejects protected document mutation and a curation/agent policy that deliberately allows protected document mutation;
- protected/canonical edits allowed by curation policy mark the protected owning document dirty and surface that protected content was mutated;
- saving a workspace skips protected documents by default and requires explicit curation/save intent or destination/export policy for protected dirty documents;
- creating new content requires selecting a target document when more than one candidate target exists;
- canonical documents can be referenced by user/scenario documents without being mutated unless an explicit curation operation targets the canonical owner.

#### Testable outcomes

- Attempting to edit a protected symbol through shared editor services is rejected by default without mutating the owning document.
- Attempting to edit a protected symbol through an explicit curation/agent mutation policy succeeds and marks the owning document dirty/protected-mutated.
- Saving a workspace skips protected documents by default.
- Saving protected dirty documents requires explicit curation/save intent or export destination.
- Creating a new template/action plan/scenario in a multi-document workspace requires or records a target document when ambiguous.
- One-document writable sessions preserve current save behavior.

#### TDD / invariant trace

Affected invariants:

- Content editor operations preserve declared IDs, layouts, action plans, validation results, and save results through shared content/editor services.

Existing tests to preserve/run:

- `ContentEditorServiceUpdatesEntityPresetAndPresentation`
- `ContentEditorServiceValidatesCurrentDocumentAfterEdits`
- `ContentToolDispatcherKeepsSessionAcrossSemanticEditCalls`
- `FrontendEditorServiceAndAgentApiShareContentEditorSessionAsParallelSurfaces`

New intentionally failing tests before production changes:

- `ContentWorkspaceEditorRejectsProtectedDocumentMutationByDefault`
- `ContentWorkspaceEditorAllowsProtectedDocumentMutationWhenCurationPolicyIsExplicit`
- `ContentWorkspaceSaveSkipsProtectedDocumentsByDefault`
- `ContentWorkspaceSaveRequiresExplicitIntentForProtectedDirtyDocuments`
- `ContentWorkspaceEditorRequiresTargetDocumentForNewSymbolsWhenAmbiguous`

#### Phase 5 design update

- 2026-08-11 user clarification and content-editor consultation: read-only/protected content should still be editable through trusted agent-facing tools during development/curation, while frontends should be able to selectively enforce protection. Revised this phase from absolute read-only rejection to explicit protection policy: frontend-safe defaults reject protected mutation/save, curation/agent policy can deliberately mutate protected owners and requires explicit protected save/export intent.

#### Phase 5 implementation log

- 2026-08-11: Added `ContentWorkspaceEditor` as the first internal workspace mutation/save policy surface. It routes entity-template updates and creation to owning/target documents, rejects protected document mutation by default, allows protected mutation when `AllowProtectedDocumentMutation` is explicit, marks owning documents dirty, records protected mutation state, skips protected documents on default save, and saves protected dirty documents only when `IncludeProtectedDocuments` is explicit. `ContentWorkspaceDocument` now tracks mutable dirty/protected-mutation state while workspace compile summaries expose `HasProtectedMutation`.
- Verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~ContentWorkspaceCompiler|FullyQualifiedName~ContentEditorServiceUpdatesEntityPresetAndPresentation|FullyQualifiedName~ContentEditorServiceValidatesCurrentDocumentAfterEdits|FullyQualifiedName~ContentToolDispatcherKeepsSessionAcrossSemanticEditCalls|FullyQualifiedName~FrontendEditorServiceAndAgentApiShareContentEditorSessionAsParallelSurfaces|FullyQualifiedName~ContentEditorServiceValidateUsesContentCompiler"` passed 23 tests.

#### Phase 5 friction log

- 2026-08-11: Targeted test build was again blocked by a stale `GameGameGame.Content.Tools` process holding `GameGameGame.Content.dll` in `src/GameGameGame.Content.Tools/bin/Debug/net10.0`. Mitigation used: stopped process ID `15132` and reran the targeted Phase 5 tests successfully. This remains an environment/process lock, not a content policy design issue.
- 2026-08-11: Existing `ContentWorkspaceDocument` was a positional record, which made dirty/protected-mutation state awkward to update safely. Mitigation: converted it to a sealed class with immutable identity/source/protection metadata and mutable `IsDirty` / `HasProtectedMutation` state. Compile output still exposes immutable summaries.

### Phase 5.5: Content-editor consultation checkpoint before mutable API exposure

#### Intent

Stop after workspace compile/materialization and internal ownership policy exist. Get content-editor sign-off before exposing mutable multi-document operations through agent APIs or frontend editor workflows.

Review points:

- save ownership and dirty document behavior;
- protected/canonical mutation policy: default rejection for frontend/final-product workflows, explicit allowed mutation for agent curation workflows, and explicit save/export requirements;
- conflict and ambiguity diagnostic structure;
- whether advisory organization warnings are distinguishable from materialization blockers;
- whether current authoring loop remains intact.

#### Testable outcomes

- Content-editor sign-off is captured before mutable multi-document API/tool exposure.
- Any pressure to introduce implicit overrides, source-layout hard rules, or automatic canonicalization triggers a plan revision before production changes continue.

#### Phase 5.5 consultation log

- 2026-08-11 content-editor conditional sign-off: Phase 1-5 direction is acceptable and no major semantic change is required before Phase 6. The protected-policy model is the right split: protected by default, explicit curation mutation allowed, protected save separate from edit policy, frontend can selectively enforce protection, and agent/headless curation can deliberately opt in.
- Required mitigations before mutable API exposure:
  - every mutable multi-document entry point must expose mutation policy explicitly, with protected-safe default and clearly named curation opt-in;
  - save policy must remain separate from edit policy, and protected dirty documents require explicit save/export intent;
  - API responses must expose document ID, source path, source kind, protected/read-only status, dirty status, protected-mutated status, and save eligibility/result;
  - duplicate same-kind symbols and ambiguous references remain hard blockers for composition/materialization;
  - new-symbol creation must require target document selection when multiple candidate documents exist;
  - Phase 6 API docs/tests should state that consumer/final-product frontend workflows use protected-safe defaults, while curation/editor-agent workflows may opt into protected mutation deliberately.
- Watch items: prefer internal `protected` wording over `read-only`, make protected-mutated state easy to surface before save, and keep one-document workflows unchanged unless a document is explicitly opened as protected.

#### TDD / invariant trace

Affected invariants: None directly. This is a planning/consultation gate.

Existing tests to preserve/run: Phase 1 through Phase 5 targeted suites.

New tests: None.

### Phase 6: Agent/headless and frontend-neutral read-only workspace APIs

#### Intent

Expose workspace compile/surface operations through shared services and agent/headless APIs before adding broad frontend mutation workflows.

Candidate operations:

- open/load a workspace from explicit document paths or a folder/manifest selection;
- list workspace documents with source class/read-only/dirty facts;
- list workspace symbols and references with source ownership;
- validate workspace and selected scenario;
- materialize/run selected scenario from workspace;
- return structured missing/ambiguous/conflict diagnostics.

#### Testable outcomes

- Agent API can open or build a workspace from multiple documents and validate it.
- Agent API can list documents, symbols, and missing/ambiguous references without parsing YAML or diagnostic text.
- A persisted scenario can be run by ID from the workspace.
- Existing one-file agent content session tools continue to work.

#### TDD / invariant trace

Affected invariants:

- Content editor operations and scenario tooling use shared Content/Core services and structured reports.

Existing tests to preserve/run:

- `AgentContentEditorApiRunsPersistedScenarioById`
- `AgentContentEditorApiCreatesCombinedPersistedScenarioReport`
- `ContentToolDispatcherCreatesBehaviorPlanAndPreviewWithValidationSummary`
- `ContentToolDispatcherRunsScenarioPlayerNarrativeLogById`

New intentionally failing tests before production changes:

- `AgentContentEditorApiValidatesMultiDocumentWorkspace`
- `AgentContentEditorApiListsWorkspaceDocumentsAndSymbols`
- `AgentContentEditorApiRunsWorkspaceScenarioById`

#### Phase 6 implementation log

- 2026-08-11: Added `AgentContentWorkspaceApi` as the first agent/headless multi-document workspace API surface. It accepts explicit workspace documents, validates the composed workspace, lists documents/symbols/references/diagnostics with source/protection/dirty/save eligibility facts, and runs a persisted workspace scenario by ID through shared `ScenarioRunService`. Added `ScenarioRunService.Run(ContentWorkspace, PersistedScenarioRunRequest)` to run workspace-materialized scenarios with a distinct workspace run mode. This phase is read/list/validate/run only; mutable multi-document agent operations remain deferred so Phase 5.5 mitigation requirements can be applied explicitly per mutation entry point.
- Verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~AgentContentEditorApiValidatesMultiDocumentWorkspace|FullyQualifiedName~AgentContentEditorApiListsWorkspaceDocumentsAndSymbols|FullyQualifiedName~AgentContentEditorApiRunsWorkspaceScenarioById|FullyQualifiedName~AgentContentEditorApiRunsPersistedScenarioById|FullyQualifiedName~AgentContentEditorApiCreatesCombinedPersistedScenarioReport|FullyQualifiedName~ContentToolDispatcherCreatesBehaviorPlanAndPreviewWithValidationSummary|FullyQualifiedName~ContentToolDispatcherRunsScenarioPlayerNarrativeLogById|FullyQualifiedName~ContentWorkspaceCompiler"` passed 25 tests.

#### Phase 6 friction log

- 2026-08-11: The initial workspace run test expected a pending `PlayerChoice` observation after zero turns, but zero-turn runs do not advance far enough to produce that runtime observation. Mitigation: revised the test to assert the workspace run mode and final-state/player placement, leaving pending-prompt behavior covered by existing persisted scenario run tests that execute a turn.

### Phase 7: First canonical/debug composition scenario

#### Intent

Delegate user-facing scenario/content assembly to content-editor after the compiler can support it. The first target is a `Debug Room` scenario based on Pocket Bazaar plus the calibration room, referencing promoted/candidate content across multiple files.

This phase should prove composition ergonomics rather than harden final canonical packaging rules.

#### Testable outcomes

- A curated debug workspace includes canonical/candidate creature, object, substrate, room/space, ecology, command-plan, and scenario content documents.
- `Debug Room` validates/materializes/runs from the workspace.
- Promotion/category standards are recorded as content-authoring guidance or advisory diagnostics only, not hard validation blockers.

#### TDD / invariant trace

Affected invariants:

- Built-in content must load and validate, but tests should not pin valid design choices such as balance values, glyphs, positions, or action plan behavior.
- Scenario runs use shared Content/Core services.

Existing tests to preserve/run:

- `PrototypeRegistryValidationPassesForBuiltInContent`
- `ScenarioCatalogLoadsCuratedManifestSectionsAndEntryMetadata`
- `ScenarioCatalogValidationReportsCuratedManifestIssuesAndUnclassifiedCandidates`
- `ScenarioRunServiceRunsPersistedScenarioByIdWithInsertedPlayer`

New intentionally failing tests before production changes:

- `DebugRoomWorkspaceValidatesPromotedContentComposition`
- `DebugRoomWorkspaceScenarioMaterializesAndRuns`

#### Phase 7 implementation log

- 2026-08-11 incremental content-editor start: created the first split promoted/debug workspace content instead of promoting every category at once. Added canonical `debugPlayer` plus `debugPlayerActionPlan` in `src/GameGameGame.Content/Canonical/Creatures/DebugPlayer.yaml`, canonical reusable `debugRoomRoot` in `src/GameGameGame.Content/Canonical/Spaces/DebugRoomRoot.yaml`, and the initial `debug-room` scenario in `src/GameGameGame.Content/Debug/DebugRoom.yaml`. The initial scenario contains only the room and player; future promotions should add one piece of content at a time and test it by placing it in this debug room workspace.
- Verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~DebugRoomWorkspace|FullyQualifiedName~ContentWorkspaceCompiler|FullyQualifiedName~AgentContentEditorApiValidatesMultiDocumentWorkspace|FullyQualifiedName~AgentContentEditorApiRunsWorkspaceScenarioById"` passed 22 tests.
- 2026-08-11 scenario-root content rule refactor: updated `debugRoomRoot` to act as the scenario/world container, added child `debugStartRoom`, moved placed `debugPlayer` into `debugStartRoom` with `controller: Player`, and removed legacy player insertion fields from `debug-room`. Added `Content-Authoring-Manual.md` guidance that new composed/exhibit scenarios should place players and ordinary interactables inside room-like child entities instead of directly in the scenario root. Verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~DebugRoomWorkspace|FullyQualifiedName~ContentWorkspaceCompiler|FullyQualifiedName~WorkspaceScenarioCatalog"` passed 25 tests.
- 2026-08-11 substrate promotion: added canonical `scrap` substrate in `src/GameGameGame.Content/Canonical/Substrates/Scrap.yaml` using semantic `item.coin` / `item.coin.default` presentation with glyph/color fallback. Added five `scrap` instances to `debugStartRoom` and included the substrate document in the default workspace scenario catalog. Verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~DebugRoomWorkspace|FullyQualifiedName~ContentWorkspaceCompiler|FullyQualifiedName~WorkspaceScenarioCatalog"` passed 25 tests.
- 2026-08-11 object promotion: added canonical `chest` in `src/GameGameGame.Content/Canonical/Objects/Chest.yaml` using semantic `item.box` / `item.box.default` and canonical `bag` in `src/GameGameGame.Content/Canonical/Objects/Bag.yaml` using semantic `item.bag` / `item.bag.default`. Placed `debugChest` and `debugBag` in `debugStartRoom`; chest is intentionally not portable by the debug player (`bulk` exceeds player aperture), while bag is portable by the debug player (`bulk` fits player aperture and player bulk exceeds bag aperture). Included both object documents in the default workspace scenario catalog. Verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~DebugRoomWorkspace|FullyQualifiedName~ContentWorkspaceCompiler|FullyQualifiedName~WorkspaceScenarioCatalog"` passed 25 tests.
- 2026-08-11 push-block promotion: added canonical `pushBlock` in `src/GameGameGame.Content/Canonical/Objects/PushBlock.yaml` using semantic `object.pushBlock` / `object.pushBlock.default`, with local catalog/palette fallback. Placed `debugPushBlock` in `debugStartRoom`; it is pushable-sized for the debug player (`bulk` fits player aperture) but not portable as inventory (`player bulk` does not fit push-block aperture). Included the object document in the default workspace scenario catalog. Verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~DebugRoomWorkspace|FullyQualifiedName~ContentWorkspaceCompiler|FullyQualifiedName~WorkspaceScenarioCatalog"` passed 25 tests.
- 2026-08-11 debug-player action surface: expanded canonical `debugPlayerActionPlan` to expose frontend action menu coverage for Pickup (`PickupTarget`), Drop (`DropFacing`), Give (`GiveTarget`), Take (`TakeTarget`), Enter (`EnterTarget`), and Exit (`ExitFacing`) while preserving existing movement/backstep setup. Restored `debugPushBlock` to the Debug Room test-expected placement at `(4,4)`. Verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~DebugRoomWorkspace|FullyQualifiedName~ContentWorkspaceCompiler|FullyQualifiedName~WorkspaceScenarioCatalog"` passed 25 tests.
- 2026-08-11 canonical action cleanup: changed canonical `debugPlayerActionPlan` to use canon-promoted steps for frontend action coverage: `Move`, `TransformAdjacentToInventory`, `TransformInventoryToAdjacent`, two directional `Transfer` steps (`ActorToTarget` and `TargetToActor`), `EnterTarget`, and `ExitFacing`. Added a stopgap canonical-content action allowlist/forbidden chart to `docs/Source of Truth/Content-Authoring-Manual.md` so canonical content avoids compatibility aliases (`PickupTarget`, `DropFacing`, `GiveTarget`, `TakeTarget`, etc.) until old actions are removed. Verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~DebugRoomWorkspace|FullyQualifiedName~ContentWorkspaceCompiler|FullyQualifiedName~WorkspaceScenarioCatalog"` passed 25 tests.

#### Phase 7 friction log

- 2026-08-11: Targeted test build was blocked by a stale `GameGameGame.Content.Tools` process holding Content/Core DLLs. Mitigation used: stopped process ID `4356` and reran targeted tests successfully.
- 2026-08-11: The headless run final-state summary reports the scenario root's immediate contents and did not include nested `debugPlayer` details after moving the player into `debugStartRoom`. Mitigation: updated the test to assert nested player placement through the materialized world and keep the run assertion focused on root immediate contents/run mode.

## Expected diagnostics vocabulary

Reserve structured diagnostic codes/categories for at least:

- duplicate symbol declaration;
- missing symbol reference;
- ambiguous symbol reference;
- wrong-kind reference, if detected separately from missing;
- read-only mutation attempt;
- no target writable document for creation;
- unsupported implicit override/shadowing attempt;
- workspace load/source failure.

Diagnostics should include document ID, source path when available, symbol key, reference key where applicable, and related declarations/candidates for conflicts and ambiguity.

## Explicit future follow-ups

- Explicit package/import/dependency syntax after workspace composition proves useful.
- Explicit canonical promotion/curation workflow and content index standards in `Content-Authoring-Manual.md`.
- Explicit override/extension semantics for user content, if selected; no implicit override should be backfilled.
- Source-span/line-column mapping for source jumps.
- Broader SadConsole editor/browser workflows over multi-document workspace surfaces.
- Read-only shipped content packaging/distribution policy.
