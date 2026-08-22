---
id: source.frontend-ux-invariants
title: Frontend UX Invariants
kind: source-of-truth
subkind: frontend-ux-invariants
status: active
owners: [frontend-owner]
audience: [frontend-owner, core-owner]
lane: frontend-ux-invariants
truth_rank: 20
truth_domains: [frontend-boundary, test-trace]
read_when:
  - planning SadConsole or future frontend work
  - deciding whether behavior belongs in Core Content Headless Editor services or in a frontend
  - shaping shared session action target log or panel contracts for frontend consumption
related:
  - source.entity-panel-ux-spec
  - source.frontend-ux-standards
  - source.testing-charter
  - source.invariants
  - source.engine-editor-capabilities
---
# Frontend UX Invariants

Status: Source of truth for frontend-facing UX constraints and layer boundaries.

Read when:

- planning SadConsole or future frontend work;
- deciding whether behavior belongs in Core/Content/Headless/Editor services or in a frontend;
- shaping shared session, action, target, log, or panel contracts for frontend consumption.

Related documents:

- `docs/Source of Truth/Entity-Panel-UX-Spec.md` records the canonical entity-panel, breadcrumb, and log UX model.
- `docs/Source of Truth/Frontend-UX-Standards.md` records frontend UI-bible presentation standards that guide implementation but are not Core behavior invariants.
- `docs/Source of Truth/testing-charter.md` records frontend testing workflow expectations and the Core/frontend TDD split.
- `docs/Plans/Frontend-SadConsole-Rolling-Board.md` records active frontend implementation work.
- `docs/Archived/Frontend-Testing-Strategy-Proposal.md` records the approved initial frontend testing strategy that established the current test project and charter guidance.
- `docs/Archived/SadConsole-Spike-Findings.md` records prototype evidence behind these constraints.
- `docs/Source of Truth/invariants.md` records stable Core behavior contracts and test traces.
- `docs/Source of Truth/Engine-Editor-Capabilities.md` records maintainer-facing capability support.

## Relationship to Frontend UX Standards

Keep this document separate from `Frontend-UX-Standards.md` for now. This document records boundary rules, stable constraints, shared contract names, ownership splits, and test traces that determine whether behavior belongs in frontend code or in shared Core/Content/Headless/Editor services. `Frontend-UX-Standards.md` records the UI bible: visual grammar, presentation preferences, interaction patterns, and deferred design guidance that can guide implementation without always being testable invariants.

There is intentional overlap where a rule has both a boundary aspect and a presentation aspect. In those cases, keep the enforceable ownership/test-trace statement here and keep the concrete UI treatment in Standards.

## Stable frontend constraints

1. **Frontends do not invent simulation semantics.** Gameplay legality, action resolution, turn advancement, traces, target selection rules, containment, inventory, and materialization semantics belong in Core, Content, Headless, or Editor services.
2. **Frontend state is presentation state.** Frontends may own layout, focus, cursor, prompt mode, hover state, collapse/expand state, scrolling, selected visible panel, styling, animation, and key/mouse/controller bindings. They must not own independent simulation state or duplicate action legality rules.
3. **Player action should converge on shared action contracts.** The current direct-player-command path is a compatibility bridge. Future work should shape it toward Core-owned runtime control-source state plus Action Choice-style request/submission/resolution concepts rather than preserving a permanently special player-command layer. In the first action-step selection pathway, movement controls may remain direct, while `Enter` opens an authored action-step list, then target/source and destination lists derived from Core Action Choice facts. A future target-first pathway should reuse the same Core facts and submission services rather than adding separate legality rules.
4. **Action evaluation remains authoritative.** Target/affordance hints may help prompts and highlighting, but final action resolution must still be performed by shared Core-aware services.
5. **Logs derive from structured outcomes, not parsed display strings.** Frontends may choose wording and emphasis, but chronological logs, local logs, and inspectable log entries should be projected from structured action/turn outcomes with entity/location anchors.
6. **Scenario launch is frontend-neutral.** Scenario discovery, materialization, diagnostics, player insertion, registry/presentation lookup, and capability-gap classification should be reusable by SadConsole, future shareable frontends, tests, and editor previews.
7. **Editor-like workflows consume editor/content APIs.** A future integrated game/editor frontend may present editing through rich panels, but it must not bypass canonical editor services, mutate YAML ad hoc, or introduce editor-only concepts that Core/Content cannot consume.
8. **SadConsole is the preferred debug/editor browser direction, not a final engine lock.** It is the current canonical direction for debug/browser UX work, while final engine selection remains informed by mouse interaction, browser/download packaging, animation, editor UX, and maintenance cost.
9. **Menus and ordinary editor controls have a directional primary path.** Every menu, picker, selectable list, and ordinary editor control should be operable through directional navigation plus Select and Cancel. Directional navigation should move focus spatially from the user's point of view: Left/Right move to selectable items shown left/right in the current layout, and Up/Down move to selectable items shown above/below or previous/next within the current list. Shortcuts such as Tab, shoulder buttons, letter keys, mouse clicks, or direct hotkeys may exist as convenience paths. Explicit dense-editor submodes such as inventory grid editing or action-plan sequence editing may make visible hotkeys the primary path for high-frequency actions like place/delete/move/insert, but those exceptions must be contextual, documented on screen, cancelable with Esc, and backed by shared editor services.
10. **Contextual controls are visible for the current focus.** Frontends should reserve screen space for contextual controls/status that describes the currently focused selection or mode. The displayed controls may later change labels for keyboard, mouse, controller, or other input devices, but the UI should not rely on undocumented hotkeys for normal editor workflows.
11. **Typing is an explicit submode of selection-driven UI.** Text entry should be entered from a selectable/editable field or an explicit `Enter new` style option where the backed service supports free text or creation. Text-entry mode should provide confirm/cancel behavior and should not blur into menu navigation semantics.
12. **Editor navigation uses semantic focus targets.** Directional movement in editor screens should jump between meaningful authored-content controls, cards, rows, fields, or grid cells rather than arbitrary screen coordinates. Visual focus targets should correspond to user-understandable editor concepts such as template name, glyph, color, metadata fields, default action plan, targeting-rule label/target/range, brush, or authored inventory cell. This is an editor presentation/input invariant only: activating a focused target must still route authored mutations through shared editor/content services.
13. **Editor dirty state and preview-stale state have one user-facing recovery action.** In the componentized SadConsole editor, authored mutations mark the editor dirty and imply that preview facts may be stale. Saving through the shared editor service is the primary user-facing action that clears the dirty/stale condition and refreshes preview state. Future richer preview systems may add diagnostics or progress indication, but should not reintroduce separate stale states without a clear shared-service reason.
14. **Play-mode Select/Cancel prompts behave as a stack.** Action selection and nested target/source/destination prompts may be frontend-owned focus state, but Select and Cancel should move predictably through the active prompt stack. Select advances or submits the currently focused valid choice through shared action services; Cancel returns to the previous prompt layer or closes the action selector without submitting. Cancel must not mutate simulation state by itself.

## Shared contract boundary names

The frontend roadmap uses these first shared contract boundaries:

| Boundary | Owner | Purpose |
|---|---|---|
| Playable scenario session launch | Content/Headless-aware services | Launch scenarios through a reusable result containing scenario identity, world, registry/presentation lookup, action plans, player entity, active plane/container, diagnostics, runtime failures, and capability gaps. |
| Controlled actor command / Action Choice compatibility result | Core-aware services | Execute current direct-control actions through one authoritative path now; future Action Choice should derive requests from runtime control source and authored action plans, then return success/failure, turn-consumption, trace, target/source/destination anchors, and resulting turn report. |
| Action target / affordance query | Core-aware services | Enumerate valid and invalid movement, pickup, drop, enter, exit, and related choices for prompts/highlighting without replacing authoritative resolution. |
| Structured action outcome / log projection | Core/Headless-aware services | Project traces and turn reports into inspectable chronological and local log rows with actor, target, context, result, and failure reason anchors. |
| Entity panel projection | Content/Headless-aware services | Provide frontend-neutral data for visible entity panels while keeping layout, scrolling, focus, and rendering frontend-owned. |

## Frontend-owned responsibilities

Frontends own:

- rendering panels, cells, breadcrumbs, logs, borders, colors, animation, and visual themes;
- panel layout, collapse/expand presentation, scrolling, clipping, screen-space geometry, and hit-testing;
- keyboard, mouse, and controller bindings;
- focus, cursor, selection, hover, and prompt state;
- menu presentation, distribution/packaging experiments, and player-facing wording derived from shared data.

## Not frontend-owned

Frontends must not become the durable owner of:

- action legality, failure policy, or failed-action turn-consumption rules;
- movement, inventory, aperture, containment, target-selection, or turn-advancement semantics;
- scenario materialization or player insertion rules;
- diagnostics classification for scenario launch;
- action/turn event facts that are needed by more than one frontend;
- editor/content mutation semantics.

## Current UX risks and decision pressure

These are known risks to evaluate before treating any frontend engine or interaction model as final:

- panel-chain readability with many panels, large grids, dense spaces, long names, or deep containment;
- keyboard focus/cursor complexity, especially when player movement and inspection focus diverge;
- mouse hit-testing, hover, and click affordance comfort in SadConsole;
- compact local logs that do not hide necessary context;
- clear facing/target indicators without grid clutter;
- browser/HTML5 or itch.io-friendly packaging versus downloadable desktop delivery;
- future editor UX comfort compared with a richer UI/game framework.

## Frontend invariant test trace

Frontend invariant traces may point to Core/Content/Editor tests when the invariant is enforced by a shared service rather than by frontend code. Frontend-owned traces should remain focused on presentation, layout, prompt, and view-model behavior.

Prefer tracing frontend invariants to focused component/helper suites rather than every individual frontend test. If a single invariant needs a disproportionately large number of frontend tests, review whether the tests are too detailed, the component needs a clearer suite boundary, the invariant should be split, or the behavior belongs in a shared semantic layer.

| Invariant | Automated trace | Manual or deferred trace |
|---|---|---|
| 1. Frontends do not invent simulation semantics. | Core/shared semantic trace remains in `docs/Source of Truth/invariants.md`: `ControlledActorCommandMoveReturnsStructuredSuccessAndAdvancesTurn`, `ControlledActorCommandFailedMoveRecordsFailureWithoutAdvancingTurn`, `ControlledActorCommandPickupReportsTargetAndDestinationAnchors`, `ControlledActorAffordanceQueryReportsValidAndBlockedMovementDirections`, `ControlledActorAffordanceQueryReportsPickupSourcesAndDestinations`, `ControlledActorAffordanceQueryReportsDropSourcesAndBlockedDropDestinations`, `ControlledActorAffordanceQueryReportsEnterTargetsAndExitDirections`, `PointOfViewUsesContainmentBreadcrumbsAndSelectsNearestContainerAsCurrentPlace`, and `EntityPanelProjectionIncludesPointOfViewFactsForProjectedEntity`. | Frontend review should reject tests or UI code that redefines legality, failure policy, movement, inventory, containment, point-of-view current-place selection, or turn advancement. |
| 2. Frontend state is presentation state. | `SadConsoleSessionLayoutTests`, `SadConsolePanelChainViewTests`, `SadConsoleSessionViewBuilderTests`, `PromptChoiceCyclerTests`, `LocalActivityViewBuilderTests`, `SadConsoleUiComponentLibraryTests`, `SadConsoleComponentGalleryTests`, and editor screen-model tests cover layout, collapse, selection, focus/border state, undo availability presentation, candidate cycling, and local presentation as pure frontend state. | Future hover, scrolling, mouse hit-testing, and animation behavior should add SadConsole tests once stable enough to pin. |
| Inventory-space component remains presentation-only and layer-stable. | `InventorySpaceViewModelTests`, `ConsumerPlayModeScreenTests`, and `SadConsoleComponentGalleryTests` cover the renderer-neutral view model, viewport/cell geometry, backdrop/entity/decorator separation, render options/profiles, current-space Play-mode consumption, required-size behavior, and executable gallery reference. | Future true transparency, accent layers, semi-transparent information layers, scaling, scrolling, and mouse hit-testing should add focused tests before becoming durable patterns. |
| Consumer Play mode confines gameplay UI to drawable bounds. | `ConsumerPlayModeScreenTests` covers pixel/cell layout resolution, the one-tile glyph-181 border buffer contract, `F12` debug color state, centered bare-grid normal mode, debug-label overlay mode, and component placement inside the resolved drawable bounds. | Manual frontend smoke confirmed fullscreen and border visibility on multiple resolutions. Future display work should add evidence for pixel-perfect leftover-monitor-pixel centering if promoted from deferred UX spec note. |
| 3. Player action should converge on shared action contracts. | Core/shared bridge trace remains in `docs/Source of Truth/invariants.md`: `ControlledActorCommandMoveReturnsStructuredSuccessAndAdvancesTurn`, `ControlledActorCommandFailedMoveRecordsFailureWithoutAdvancingTurn`, `ControlledActorCommandPickupReportsTargetAndDestinationAnchors`, Action Choice request/submission tests for Move/Pickup/Drop/Push. Frontend action-step-first prompt trace: `EnterOpensActionStepFirstMenuBeforeChoosingPickupTargetAndDestination`, `DropChoiceUsesTargetThenDestinationListsAndHistorySubmission`, and consumer Play Push traces `ConsumerPlayModeSizeCalibrationCanPushNestedBagThroughContextPrompt` / `ConsumerPlayModeCanonicalPushShowcaseCanPushPlayerBlock`. | Future target-first action menu should reuse the same Core Action Choice facts and add prompt tests when selected. |
| 4. Action evaluation remains authoritative. | Core/shared affordance and execution trace remains in `docs/Source of Truth/invariants.md`: `ControlledActorAffordanceQueryReportsValidAndBlockedMovementDirections`, `ControlledActorAffordanceQueryReportsPickupSourcesAndDestinations`, `ControlledActorAffordanceQueryReportsDropSourcesAndBlockedDropDestinations`, `ControlledActorAffordanceQueryReportsEnterTargetsAndExitDirections`, plus controlled-command execution tests. `PromptChoiceCyclerTests` cover only frontend filtering/cycling of already-computed candidates. | Manual UI review should confirm highlighted targets are presented as hints and that command submission still flows through shared execution services. |
| 5. Logs derive from structured outcomes, not parsed display strings. | Core/shared log trace remains in `docs/Source of Truth/invariants.md`: `ActionOutcomeProjectionRendersSuccessfulPickupFromStructuredCommandResult`, `ActionOutcomeProjectionRendersFailedMoveWithoutParsingDisplaySummary`, `ActionOutcomeProjectionExposesSuccessfulPickupApertureDegree`, `ActionOutcomeProjectionExposesFailedEnterApertureDegree`, `ActionLogProjectionFiltersOutcomesByEntityAndPlaneAnchors`, and history-backed autonomous actor outcome tests. `LocalActivityViewBuilderTests` cover presentation of local action snippets, including autonomous actor successes/failures, and honest empty text. | Richer target/source/destination anchors for autonomous outcomes remain Core follow-up before local logs can reliably show every affected non-actor entity. |
| 6. Scenario launch is frontend-neutral. | Content trace remains in `docs/Source of Truth/invariants.md`: `PlayableScenarioLauncherBuildsFrontendNeutralSessionFromPersistedScenario`, `PlayableScenarioLauncherBuildsFreshSessionFromCatalogEntry`, `PlayableScenarioLauncherBuildsSessionFromWorkspaceScenario`, `DebugRoomWorkspaceScenarioMaterializesAndRuns`, and `ScenarioCatalogScanServiceDiscoversFolderAndWritesManifest`. New `GameGameGame.Frontend.SadConsole` trace should live in `tests/GameGameGame.Frontend.SadConsole.Tests` with browser/request-mapping tests such as `ScenarioBrowserShowsWorkspaceDebugRoomCatalogEntry`, `ScenarioBrowserSelectionCreatesLaunchRequestWithoutLaunchingSemantics`, and `ScenarioBrowserPreservesDiagnosticsAsDisplayData`. | Future SadConsole scenario menu/session view tests should assert presentation over shared Content workspace/catalog/launcher results, not new launch semantics. |
| 7. Editor-like workflows consume editor/content APIs. | Editor/content trace remains in `docs/Source of Truth/invariants.md`: `ContentEditorServiceUpdatesEntityPresetAndPresentation`, `ContentEditorServicePlacesAndMovesCarriedEntityInInventoryLayout`, `ContentEditorServiceAddsReordersAndRemovesActionPlanSteps`, `ContentEditorServiceValidatesCurrentDocumentAfterEdits`, `FrontendEditorServiceTests`, `FrontendEditorServiceAndAgentApiShareContentEditorSessionAsParallelSurfaces`, `AgentContentEditorApiAuthorsCanonicalEnterExitBehavior`. SadConsole-owned editor shell/read-only browser/preview trace: `SadConsoleEditorContextTests`. | Future frontend Editor mode tests should assert UI/view-model behavior over editor services, not direct YAML mutation or runtime-state editing. |
| 8. SadConsole is the preferred debug/editor browser direction, not a final engine lock. | `tests/GameGameGame.Frontend.SadConsole.Tests` is the new automated frontend test home for the clean SadConsole surface. `tests/GameGameGame.SadConsole.Tests` remains the legacy/reference SadConsole test home while that project remains buildable and mined. | Engine-selection, packaging, mouse comfort, animation, and final UI-framework decisions remain manual/research-backed until promoted into stable contracts. |
| New Frontend.SadConsole component tests preserve presentation-only boundaries. | New stable tests belong in `tests/GameGameGame.Frontend.SadConsole.Tests`, beginning with suites for settings defaults, display/drawable-bounds resolution, scenario browser selection/request mapping, component-gallery screen models, and Play-surface layout over shared session/projection DTOs. Representative planned tests: `FrontendSadConsoleSettingsLoadsDefaultsWhenNoPersistenceExists`, `FrontendSadConsoleDisplayShellResolvesDrawableBoundsInsideChrome`, `ScenarioBrowserShowsWorkspaceDebugRoomCatalogEntry`, `ScenarioBrowserSelectionCreatesLaunchRequestWithoutLaunchingSemantics`, `PlaySurfaceBuildsFromPlayableScenarioSession`, and `PlaySurfacePlacesComponentsInsideDrawableBounds`. | Tests in the new project must not assert duplicate materialization rules, player insertion rules, action legality, turn advancement, YAML mutation semantics, command outcomes, or diagnostics classification. Manual smoke remains required for real SadConsole host rendering, fullscreen fallback, mouse feel, and gamepad comfort. |
| 9. Menus and editor controls have a directional primary path. | `SadConsoleScenarioSelectionScreenTests` covers the new scenario-selection Play/Edit sub-panel and Escape-as-Cancel/Back behavior; component-level selection/focus routing is covered by `SadConsoleUiComponentLibraryTests` and `SadConsoleComponentGalleryTests`. Future SadConsole command-menu/picker tests should cover directional focus movement and Select/Cancel as the primary path for stable editor controls. | Existing hotkey-heavy editor mutation slices are legacy reference debt; cleanup should migrate durable workflows toward this invariant before adding many more controls. |
| 10. Contextual controls are visible for the current focus. | Future footer/context-control view tests should cover that focused editor modes expose current valid actions without launching the real window. | Manual review should reject hidden-only control schemes for normal editor workflows. |
| 11. Typing is an explicit submode of selection-driven UI. | Existing SadConsole template presentation/targeting edit tests cover confirm/cancel text-edit submodes for early fields. Future text-entry widgets should add focused tests. | Controller/on-screen-keyboard text entry remains deferred; current keyboard text entry is acceptable as long as it is entered/cancelled explicitly. |
| 12. Editor navigation uses semantic focus targets. | Future SadConsole entity-template editor tests should cover focus movement between semantic fields/rows/grid cells and activation through Select/Cancel without depending on letter hotkeys. | Current list/detail template editing is prototype debt; planned dedicated editor screens should use semantic focus targets before being treated as durable controller-friendly workflows. |
| 13. Editor dirty state and preview-stale state have one user-facing recovery action. | `SadConsoleScenarioEditScreenTests` covers saved/dirty status, unsaved-exit modal behavior, and save-state presentation. | Future materialized preview UI should add tests that Save refreshes/rebuilds the visible preview facts once the preview surface is richer than the current authored summary. |

## Stage 0 debt inventory

The Stage 0 code/documentation audit found these current responsibility concentrations:

- `PlayableScenarioLauncher` in Content now owns frontend-neutral playable session launch over scenario materialization and catalog entries. The former Console compatibility shim has been removed.
- `ControlledActorCommandService` in Core now owns direct-control command execution policy for move, pickup, drop, enter, and exit. Failed direct commands record traces without advancing the turn; successful commands consume and advance the turn.
- `ControlledActorAffordanceService` in Core now exposes direct-control prompt/query hints for movement, pickup, drop, enter, and exit. Frontends may use these hints for highlighting/skipping/explanations, but `ControlledActorCommandService` remains authoritative for execution.
- `ActionOutcomeProjection` and `ActionLogProjection` in Core now expose structured log rows and local entity/plane filtering for controlled-command results plus conservative autonomous actor outcomes from history intervals.
- `EntityPanelProjectionService` in Content now exposes a first-pass frontend-neutral entity panel model combining inspection facts, breadcrumbs, point-of-view current-place/ratio/diagnostic facts, action state, inventory grid, local contents, and structured local logs. This contract is intentionally expected to evolve after SadConsole consumption.
- Console-specific menu, play-loop, input, and inspection formatting code has been removed. Equivalent durable behavior should live in SadConsole or shared Content/Core services.

Known first-pass panel projection limitations:

- action-plan summary is presence/type-level only, not a rich behavior-chain preview;
- structured previous-action snippets can use controlled-command outcomes and conservative autonomous actor outcomes; richer target/source/destination autonomous anchors are still follow-up;
- local log inclusion currently uses entity and inventory-plane anchors and may need richer containment/affected-entity semantics;
- layout, scrolling, collapse, focus, filtering, and visual prioritization are intentionally not represented.
- Removed Console key-mapping and inspection-formatting helpers should not be revived as shared semantics; use Core controlled commands and Content panel projections instead.
- Only the SadConsole spike findings were carried forward to main. The old `src/GameGameGame.SadConsolePrototype` prototype source is intentionally not a current codebase artifact. Current SadConsole implementation lives in the fresh `src/GameGameGame.SadConsole` project and should continue consuming shared contracts rather than reviving prototype-local semantics.

## Contradictions or clarification needed

No semantic contradiction was found in the core UX direction: the documents consistently say to stop adding SadConsole-only features until shared contracts are paved and keep simulation semantics out of frontends.

Repository-state clarification:

- Several historical docs describe the completed prototype and old run commands. Those commands are archival only. Main should carry forward the findings, not the prototype project. Current SadConsole work should continue in `src/GameGameGame.SadConsole` and keep frontend-specific behavior behind testable presentation/view-model seams where practical.
