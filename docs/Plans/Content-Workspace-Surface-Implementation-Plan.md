---
id: plan.content-workspace-surface-implementation
title: Content Workspace Surface Implementation Plan
kind: plan
status: active
truth_rank: 45
truth_domains: [planning-priority, implementation-navigation, test-trace]
owners: [core-owner]
audience: [core-owner, content-editor, frontend-owner]
read_when:
  - implementing type-first content browsing or editor surfaces over compiler facts
  - adding Content workspace/surface projections before multi-file imports
  - deciding which frontend/editor backlog items to pull into the compiler-backed surface
related:
  - source.invariants
  - source.testing-charter
  - source.engine-editor-capabilities
  - source.vertical-slice-map
  - plan.sadconsole-frontend-roadmap
  - plan.high-level-roadmap
  - plan.content-validation-compiler-migration-sprint
---

# Content Workspace Surface Implementation Plan

Status: Active planning document for the next content/editor surface work after the completed compiler migration sprint.

The completed migration sprint is archived at `docs/Archived/Content-Validation-Compiler-Migration-Sprint-Plan.md`.

## Goal

Build the first compiler-backed content surface that lets shared services and future frontends reason about content by type and dependency graph rather than by YAML file layout, while still staying inside the current one-document content model.

This plan should turn the completed compiler runway into a usable surface for editor/frontend work:

1. expose a stable content surface/workspace projection over the compiler result;
2. group scenarios, entity templates, action plans, presentations, merged layers, diagnostics, symbols, and references by type;
3. support scenario-centric selection: selected scenario details plus shared content panes for entities/action plans/references;
4. surface dependency warnings and provenance/source facts for unresolved references;
5. prepare SadConsole or a future frontend to browse content without owning YAML semantics;
6. defer actual multi-file imports/includes/package merge semantics until this surface is useful in one-document form.

## Non-goals

- Do **not** implement multi-file imports, includes, packages, workspace merge/conflict resolution, shipped-vs-user override policy, or cross-document reference resolution in this plan.
- Do **not** redesign all SadConsole editor screens at once.
- Do **not** add editor-only semantic fields that Core/Content cannot compile or validate.
- Do **not** revive the retired Avalonia editor or Console UI paths.
- Do **not** make source file layout the primary editor organization model; source paths are provenance facts only.

## Backlog scrub: frontend/editor items to pull in now

The compiler-backed surface is a good time to pull in a narrow subset of old frontend/editor backlog ideas because they align with type-first browsing and dependency projection.

Pull into this plan:

- **Debug/editor browser foundations** from `SadConsole-Frontend-Roadmap` Stage 8:
  - browse scenario/catalog metadata;
  - browse loaded entity templates, action plans, and references;
  - show action-plan/default-plan summaries in entity/template panels;
  - add action-plan preview panels through shared Content/editor services;
  - add validation diagnostics panels.
- **Componentized Editor -> Preview -> Simulation groundwork** from the roadmap:
  - keep scenario preview and simulation launch facts connected to the selected scenario;
  - add provenance-backed source-jump facts at the shared DTO level first, even if the first frontend only displays the source path and symbol ID.
- **Scenario curation/grouping metadata display** where it is already available through scenario catalog/manifest services. Treat this as read-only browsing metadata, not new content package semantics.
- **Behavior/action-plan usage display** in a small read-only form, using the compiler reference index before planning reusable behavior templates or save-as-template workflows.
- **Frontend test ergonomics** when SadConsole consumption starts: shared fixture builders for editor snapshots/playable sessions and lightweight boundary tests that verify the UI consumes shared DTOs rather than YAML or simulation semantics directly.

Defer for later plans:

- scenario root/player-start mutation polish beyond displaying current scenario facts;
- per-carried-instance initial facing/state authoring;
- full generic typed action-step parameter editing;
- mouse hit-testing, packaging, runlog steppers, visual export, and distribution;
- behavior/action-plan templates and save-as-template workflows;
- shipped/user content separation and cross-file imports.

## Recommended implementation order

### Phase 1: Stabilize compiler surface naming and file boundaries

#### Intent

Keep the completed compiler behavior but make the new API easier to maintain before more consumers depend on it.

Likely work:

- move symbol/reference index types and builder out of `ContentCompiler.cs` into focused files such as `ContentSymbols.cs` and `ContentReferenceIndex.cs`;
- decide whether `ContentReferenceKind.BehaviorStepPlan` should be renamed or split for primitive fallback vs behavior-step plan references;
- preserve public API compatibility unless a rename is intentionally selected and test-backed immediately.

#### Testable outcomes

- Existing compiler symbol/reference tests pass unchanged or are intentionally revised for selected naming changes.
- No validation, materialization, or editor behavior changes.

#### TDD / invariant trace

Affected invariants from `docs/Source of Truth/invariants.md`: behavior-preserving refactor of Content Pipeline and Action Plan Data support.

Existing tests to preserve/run:

- `ContentCompilerBuildsSymbolsGroupedByType`
- `ContentCompilerIndexesTemplateActionPlanAndScenarioReferences`
- `ContentCompilerIndexesBehaviorStepTemplatePlanAndCostReferences`
- `ContentCompilerMarksMissingReferencesWithoutResolvingAcrossFiles`
- `ContentCompilerReturnsRegistryValidationDiagnostics`
- `ContentEditorServiceValidateUsesContentCompiler`

New tests: None expected unless naming or public DTO shape changes.

### Phase 2: Add a frontend-neutral `ContentWorkspaceSurface` projection

#### Intent

Create the first shared type-first DTO over `ContentCompileResult`. It should be usable by agent APIs, SadConsole, tests, and future frontends without exposing YAML object graphs directly.

Candidate shape:

- `ContentWorkspaceSurfaceService.Build(EditableContentDocument document, ContentCompileOptions? options = null)`;
- `ContentWorkspaceSurface` with grouped collections:
  - `Scenarios`;
  - `EntityTemplates`;
  - `ActionPlans`;
  - `Presentations`;
  - `MergedLayers`;
  - `Diagnostics`;
  - `References`;
  - `Symbols`;
  - `SourceSummary`.

This is still a one-document surface. The name may use `Workspace` as a future-facing abstraction, but the implementation must not imply imports or cross-file resolution.

#### Testable outcomes

- Surface groups content by type independent of YAML declaration order.
- Surface carries compiler diagnostics and source attribution.
- Surface carries unresolved references as warnings/errors with source/target symbol facts.
- Surface does not expose mutable DTO internals as the primary frontend contract.

#### TDD / invariant trace

Affected invariants:

- Content Pipeline:
  - `YAML content loads from strings and files into registries that can be validated.`
  - `Content editor operations preserve declared IDs... and validation results.`
  - `Frontend editor snapshots... expose authored scenarios, entity templates... action-plan summaries... validation diagnostic... through shared content/editor services...`
- Action Plan Data:
  - descriptors preserve structured built-in inputs and materialize executable plans.

Existing tests to preserve/run:

- compiler tests from Phase 1;
- `FrontendEditorServiceTests`;
- `ContentEditorServiceValidationReportsCurrentDocumentErrors`;
- `ActionPlanPreviewSummarizesBehaviorStepCosts`;
- `ActionPlanPreviewSummarizesTargetPathMoveFields`.

New intentionally failing tests before production changes:

- `ContentWorkspaceSurfaceGroupsContentByType`
- `ContentWorkspaceSurfaceCarriesCompilerDiagnosticsAndSource`
- `ContentWorkspaceSurfaceProjectsMissingReferences`

### Phase 2.5: Content-editor consultation checkpoint

#### Intent

Stop after the read-only workspace surface exists and get content-editor sign-off before exposing the surface through authoring/tool APIs or using it to drive editor workflows.

Consultation outcome already received before implementation began:

- content-editor signs off on Phase 1 when it remains a behavior-preserving refactor/naming cleanup with no validation, materialization, YAML, save, or editor behavior changes;
- content-editor signs off on Phase 2 when it remains a read-only, frontend-neutral projection over the existing one-document compiler result;
- content-editor wants another consultation after Phase 2 before authoring/tool API exposure or editor workflow consumption.

#### Constraints to preserve during Phase 1 and Phase 2

- Do **not** change YAML shape/schema, save behavior, validation semantics, or mutation APIs.
- Do **not** rewrite or canonicalize authored files.
- Keep `Workspace` vocabulary explicitly one-document-only in DTOs, docs, and tests.
- Keep mutation workflows unchanged: open/create session, inspect/list, semantic edits, validate/canonical validate, review diff, save deliberately.
- Keep new APIs additive and read-only until the surface stabilizes.
- Do **not** expose mutable `EditableContentDocument` DTOs or registry internals as the primary surface contract.
- Preserve declaration IDs exactly; type-first grouping must not imply content renaming or YAML reordering.
- Keep diagnostics structured so tools/frontends do not parse diagnostic text.
- Expose source/provenance as document/source path/symbol facts only; line/column source jumps remain deferred unless separately planned and test-backed.
- Do not overstate reference resolution; only mark references resolved when the compiler can prove resolution.

#### Friction log

Record any implementation friction here with mitigation before continuing if it affects authoring semantics, tooling shape, or the consultation constraints above.

- 2026-08-11 Phase 1/2 verification friction: targeted `dotnet test` was blocked by a running `GameGameGame.Content.Tools` process holding `GameGameGame.Content.dll` in `src/GameGameGame.Content.Tools/bin/Debug/net10.0`. Mitigation: stop the stale local tool process or rerun tests with an isolated output path before treating verification as complete. This is a build-environment lock only; it does not create pressure to change YAML/schema, save behavior, validation semantics, or mutation APIs.
- 2026-08-11 Phase 2 content-editor review friction: the new surface exposes compiler reference resolution facts, and existing compiler indexing marked `PresentationId` and `PaletteId` references as `Resolved` whenever the field was present, even when registry validation can report unknown presentation/palette IDs. Mitigation completed: added `ContentCompilerMarksUnknownPresentationAndPaletteReferencesMissing` and updated the one-document reference index so presentation/palette reference resolution uses built-in plus document-authored presentation/palette catalogs, projecting unknown IDs as missing rather than overstating resolution. content-editor confirmed this resolves the concern. This is a reference-fact correctness fix only; it does not require YAML/schema, save behavior, validation semantics, or mutation API changes.

#### Testable outcomes

- Content-editor sign-off is captured in this plan before implementation starts.
- Any Phase 1/2 friction that creates pressure to change YAML/schema, save behavior, validation semantics, or mutation APIs is recorded here and triggers immediate content-editor consultation before production changes continue.

#### TDD / invariant trace

Affected invariants: None directly. This phase is a planning/consultation gate, not a production behavior change.

Existing tests to preserve/run: Phase 1 and Phase 2 suites.

New tests: None.

### Phase 3: Add scenario-centric surface projection

#### Intent

Support the desired editor mode: select a scenario, then show scenario-specific facts plus shared content facts by type.

Candidate shape:

- `ContentScenarioSurfaceService.Build(document, scenarioId, options)`;
- selected scenario summary, scenario root/player facts, current materialization/preview readiness, diagnostics for the selected scenario, and shared content groups from the workspace surface;
- scenario dependency closure as read-only facts: root template, player template, carried entity templates under the root where available, action plans referenced by those templates.

#### Testable outcomes

- Selecting a scenario does not hide shared entity templates/action plans that live outside the scenario root dependency closure.
- Selected scenario diagnostics are grouped separately from global diagnostics.
- Missing scenario root/player references appear both in diagnostics and reference facts.
- The surface can provide current scenario preview inputs without duplicating `ScenarioMaterializer` policy.

#### TDD / invariant trace

Affected invariants:

- Content Pipeline persisted scenario materialization diagnostics.
- Scenario Tooling scenario reports/materialization use shared Content/Core services.
- Frontend editor snapshots expose authored scenarios, entity templates, action plans, diagnostics, and preview facts through shared services.

Existing tests to preserve/run:

- `ScenarioMaterializerReportsAuthoringDiagnostics`
- `ScenarioMaterializerValidatesPersistedAlphaScenarioDefinitions`
- `PlayableScenarioLauncherBuildsFrontendNeutralSessionFromPersistedScenario`
- `FrontendEditorServiceTests`

New intentionally failing tests before production changes:

- `ContentScenarioSurfaceShowsSelectedScenarioAndSharedContentByType`
- `ContentScenarioSurfaceGroupsSelectedScenarioDiagnostics`
- `ContentScenarioSurfaceCarriesDependencyClosureWithoutFilteringSharedContent`

### Phase 4: Add dependency/provenance operations for editor consumers

#### Intent

Make compiler references useful to editing tools before adding multi-file imports.

Candidate operations:

- list references from a symbol;
- list references to a symbol;
- list missing references;
- summarize “used by” relationships for templates/action plans/scenarios;
- expose source path/document ID/symbol ID for provenance/source-jump UI.

#### Testable outcomes

- Layer/entity owner, carried template, action-plan, scenario, and behavior-step references are queryable by source and target.
- Missing references can be projected into editor warnings without reparsing diagnostics text.
- Provenance facts are present even when the first frontend cannot jump to line/column yet.

#### TDD / invariant trace

Affected invariants:

- Content Pipeline validation/results preservation.
- Entity And Space merged-layer/source-cell link authoring diagnostics.
- Action Plan Data descriptor/reference preservation.

Existing tests to preserve/run:

- `ContentCompilerIndexesTemplateActionPlanAndScenarioReferences`
- `ContentCompilerMarksMissingReferencesWithoutResolvingAcrossFiles`
- `ContentValidationRejectsMergedLayerJoinDirectionalConflict`
- `PrototypeRegistryValidationReportsMissingCalledPlan`
- `PrototypeRegistryValidationReportsMissingCreateEntityTemplateReference`

New intentionally failing tests before production changes:

- `ContentReferenceQueryListsReferencesFromSymbol`
- `ContentReferenceQueryListsReferencesToSymbol`
- `ContentReferenceQueryListsMissingReferencesWithProvenance`

### Phase 4.5: Content-editor consultation checkpoint before API/tool exposure

#### Intent

Stop after scenario-centric projections and reference/provenance query operations exist, then get content-editor sign-off before exposing them through shared editor services, agent/tool APIs, or frontend workflows.

This checkpoint is required because Phase 5 turns read-only DTO shape into a broader authoring/tool contract. Content-editor should review:

- scenario-surface wording and DTO names for accidental mutation/workflow implications;
- reference-query semantics, especially which references are proven resolved, missing, or intentionally unknown/deferred;
- provenance/source facts for author usefulness without implying unsupported line/column source jumps;
- immutability expectations before public tool/API exposure;
- continued absence of YAML/schema, save behavior, validation semantics, and mutation API changes.

#### Friction log

Record Phase 3/4 implementation friction here if it affects authoring semantics, tooling shape, or Phase 4.5 sign-off constraints.

- 2026-08-11 Phase 3 verification friction: targeted `dotnet test` was again blocked by a running `GameGameGame.Content.Tools` process holding `GameGameGame.Content.dll` in `src/GameGameGame.Content.Tools/bin/Debug/net10.0`. Mitigation: stop the stale local tool process before rerunning tests, or use an isolated test output path if this recurs. This remains a build-environment lock only; it does not affect authoring semantics, tooling shape, YAML/schema, save behavior, validation semantics, or mutation APIs.
- 2026-08-11 Phase 4.5 content-editor review friction: `ContentScenarioSurface.PlayerStart` exposed `EditableContentDocument.GridCoordDto?`, which is mutable and could leak a document-owned editable DTO through an otherwise read-only surface. Mitigation: replace it with a surface-owned immutable coordinate DTO before Phase 5 exposure, and keep Phase 5 APIs explicitly read-only.

#### Testable outcomes

- Phase 3/4 implementation is reviewed before Phase 5 API/tool exposure begins.
- Any pressure to change YAML/schema, save behavior, validation semantics, or mutation APIs triggers immediate content-editor consultation before production changes continue.

#### TDD / invariant trace

Affected invariants: None directly. This phase is a planning/consultation gate, not a production behavior change.

Existing tests to preserve/run: Phase 3 and Phase 4 suites.

New tests: None.

### Phase 5: Add shared editor/agent API surface operations

#### Intent

Expose the workspace/scenario surface through existing shared services so frontend and agent consumers do not instantiate compiler internals directly.

Likely consumers:

- `ContentEditorService` read-only methods;
- `AgentContentEditorApi` or tool-host commands;
- `FrontendEditorService` snapshots, initially as additive DTO fields or new query methods to avoid destabilizing existing UI.

#### Testable outcomes

- Editor service can return workspace and scenario surfaces.
- Agent API/tooling can request the surface and missing-reference summary.
- Existing mutation workflows remain unchanged.

#### TDD / invariant trace

Affected invariants:

- Content editor operations preserve IDs, layouts, action plans, validation results.
- Frontend editor snapshots/mutations expose shared service facts rather than Avalonia/YAML-specific behavior.

Existing tests to preserve/run:

- `FrontendEditorServiceAndAgentApiShareContentEditorSessionAsParallelSurfaces`
- `ContentToolDispatcherKeepsSessionAcrossSemanticEditCalls`
- `ContentEditorServiceValidationReportsCurrentDocumentErrors`

New intentionally failing tests before production changes:

- `ContentEditorServiceBuildsWorkspaceSurface`
- `AgentContentEditorApiReturnsWorkspaceSurface`
- `FrontendEditorServiceCanQueryScenarioSurfaceWithoutMutatingDocument`

### Phase 6: First SadConsole/type-first browser consumption

#### Intent

Use the new shared surface in a narrow frontend/browser slice. Start read-only unless a separate editor-mutation plan is selected.

Recommended first UI slice:

1. scenario list/detail using scenario surface;
2. entity-template and action-plan lists grouped by type;
3. validation/missing-reference panel;
4. action-plan preview panel reuse;
5. source/provenance display as path + symbol ID, with true source jump deferred until line/column mapping exists.

#### Testable outcomes

- Frontend/browser consumes shared surface DTOs rather than YAML dictionaries.
- Selecting a scenario updates scenario-specific detail but keeps shared entity/action-plan lists visible.
- Missing references and validation diagnostics are displayed from shared diagnostics/reference facts.
- Existing componentized editor/play mode still works.

#### TDD / invariant trace

Affected invariants:

- Frontend editor snapshots/shared service facts from Content Pipeline.
- Frontend UX invariants should be traced by frontend-owner before UI implementation.

Existing tests to preserve/run:

- relevant SadConsole component/editor tests;
- `FrontendEditorServiceTests`;
- `ContentEditorServiceBuildsWorkspaceSurface` once added.

New tests before production changes:

- frontend-owner should add light screen-model/view-builder tests for grouping, selection, diagnostic display, and source/provenance rows.

#### Phase 6 narrow implementation note

Because a larger SadConsole/editor rewrite is planned, the first frontend consumption slice should remain deliberately small. The accepted minimal slice is to let the existing componentized Scenario Edit screen consume `FrontendEditorService.BuildScenarioSurface(...)` as an additive read-only projection, without changing mutation flows, snapshot shape, renderer architecture, or introducing new durable editor controls.

Implemented minimal display facts:

- scenario preview panel shows type-first workspace counts and selected scenario reference/dependency counts from `ContentScenarioSurface`;
- player-start panel uses surface-owned root/player-start facts when available;
- diagnostics panel includes selected-scenario missing-reference rows from surface references;
- existing entity/action-plan lists remain visible from the current editor snapshot, preserving the current UI shape while proving shared-surface consumption.

Deferred until the broader editor rewrite or a follow-up UI slice:

- full source/provenance rows and source jump beyond existing preview-row source jump;
- action-plan preview panel redesign;
- replacing snapshot-backed lists with surface-backed view models everywhere;
- new scrolling/layout/panel patterns.

#### Phase 6 friction log

- 2026-08-11 full SadConsole suite friction: the targeted Scenario Edit screen tests passed, but the full `GameGameGame.SadConsole.Tests` suite timed out after many unrelated `GameplayMockScreenTests` failures involving missing runtime entity IDs such as `scenarioRoot`/`mockCrate` and prompt-state expectations. Mitigation: treat the narrow Scenario Edit tests as the Phase 6 verification for this slice, keep the full-suite output for follow-up triage in the existing play-mode/mock-screen area, and avoid broad play-mode changes in this content-surface UI slice.

## Explicit future follow-up after this plan

Only after the one-document surface proves useful should a future plan consider `ContentWorkspace` multi-document composition. That later plan should define:

- document identity and load order;
- duplicate symbol/conflict policy;
- explicit import/include or package semantics;
- shipped-vs-user source classes;
- save ownership and source-jump/source-span behavior;
- cross-document reference resolution and ambiguous-reference diagnostics.
