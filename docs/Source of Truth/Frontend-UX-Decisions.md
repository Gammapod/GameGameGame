---
id: source.frontend-ux-decisions
title: Frontend UX Decision Log
kind: source-of-truth
subkind: frontend-ux-decisions
status: active
owners: [frontend-owner]
audience: [frontend-owner, core-owner]
lane: frontend-ux-decisions
truth_rank: 40
truth_domains: [frontend-rationale, frontend-presentation]
read_when:
  - reviewing why a frontend UX standard exists
  - planning a SadConsole Simulation or Editor slice
  - deciding whether a new UI affordance conflicts with a prior decision
related:
  - source.frontend-ux-standards
  - source.frontend-play-visual-language
  - plan.sadconsole-frontend-roadmap
---
# Frontend UX Decision Log

Status: Living decision log for frontend UX and mode-model decisions.

Read when:

- reviewing why a frontend UX standard exists;
- planning a SadConsole Simulation or Editor slice;
- deciding whether a new UI affordance conflicts with a prior decision.

Related documents:

- `docs/Source of Truth/Frontend-UX-Standards.md` records the current UI-bible standards.
- `docs/Source of Truth/Frontend-Play-Visual-Language.md` records player-facing Play-mode visual semantics.
- `docs/Source of Truth/Frontend-Editor-Simulation-Flow.mmd` diagrams the current Editor/Simulation context model.
- `docs/Plans/SadConsole-Frontend-Roadmap.md` records staged implementation work and backlog items.

## Format

Each decision should include:

- **Decision:** the chosen UX/model rule;
- **Reasoning:** why it exists;
- **Implications:** what future work should preserve or avoid;
- **Status:** active, provisional, superseded, or deferred.

## Decisions

### FED-001: SadConsole is the canonical debug/browser direction for now

- **Decision:** SadConsole is the current canonical debug/browser frontend direction. The former Console frontend has been removed.
- **Reasoning:** The first frontend sprint validated that SadConsole can launch scenario catalogs, play through shared controlled-command services, and render entity-panel debug information.
- **Implications:** Frontend UX work should prefer SadConsole or shared frontend-neutral services.
- **Status:** Active.

### FED-002: Entity panels are the shared visual grammar

- **Decision:** Simulation and Editor modes should use entity-panel/card vocabulary where sensible.
- **Reasoning:** The prototype spike and first SadConsole sprint both showed that spaces, containers, inventories, inspected entities, and authored templates can be understood through panel/card structures.
- **Implications:** Avoid introducing a separate special map/player/editor widget vocabulary unless a later design explicitly promotes it.
- **Status:** Active.

### FED-003: Breadcrumbs should evolve toward panel chains

- **Decision:** Breadcrumbs are not just text labels; future Simulation work should plan around breadcrumb nodes rendered as entity panels/cards, including collapsed cards for long chains.
- **Reasoning:** The prototype's breadcrumb-as-panel-chain model was promising and fits the entity-panel standard.
- **Implications:** Future layout work should support panel chains, collapsed/expanded panel states, focus/selection state, and reusable geometry before mouse hit-testing.
- **Status:** Active / initial SadConsole implementation. Editor mode now has an explicitly refreshed Preview section that renders compact turn-0 derived runtime facts and diagnostics; richer Simulation-grade panel/grid reuse remains follow-up.

### FED-004: The player entity is not visually special by default

- **Decision:** Runtime/player control may be focused, but the player entity's facts and glyph should be displayed with the same rules as other entities.
- **Reasoning:** The Stage 7A arrow-glyph experiment was useful but violated entity identity consistency.
- **Implications:** Facing, target, focus, control, and selection need adjacent/layered/decorator-style presentation, not identity glyph replacement.
- **Status:** Active.

### FED-005: Glyphs represent identity consistently

- **Decision:** Entity glyphs must be preserved wherever they represent that entity.
- **Reasoning:** Glyph substitution made facing clear but weakened entity identity and created inconsistent presentation between grids and text rows.
- **Implications:** Future animation or decoration policies must apply consistently across grids, panel headers, contents/activity rows, logs, and editor panels.
- **Status:** Active.

### FED-006: Editor launches and receives Simulation

- **Decision:** The preferred near-term cross-mode model is Editor -> scenario preview -> Simulation -> return to Editor. Main-menu scenario play should open enough editor context to return to the backing content document.
- **Reasoning:** This preserves clean content mutation/materialization/runtime boundaries while enabling the edit-preview-play loop.
- **Implications:** Do not build Editor and Simulation as isolated apps with unrelated context stacks. Plan shared content document, scenario selection, and return navigation state.
- **Status:** Active / Phase 1 shell implemented in SadConsole. The current shell preserves backing content/scenario identity across catalog launch and Simulation return; richer Editor browsing, preview panels, and source jumps remain pending.

### FED-007: Scenario preview belongs in Editor mode

- **Decision:** Editor mode should eventually show a turn-0 materialized scenario preview before launching full Simulation.
- **Reasoning:** Turn-0 preview gives immediate authored-content feedback and bridges content editing with runtime play/debug inspection.
- **Implications:** Preview panels must clearly distinguish authored source from derived materialized state; manual refresh is the safest first refresh policy.
- **Status:** Active / pending implementation.

### FED-008: Simulation may jump to authored source, but does not edit runtime as content

- **Decision:** Simulation should eventually support navigation from runtime entity/log/diagnostic facts to authored templates/action plans/scenario source when provenance exists.
- **Reasoning:** This is the useful part of “editing within Simulation” without blurring runtime state mutation and content authoring.
- **Implications:** Need runtime-to-source binding visibility. Runtime debug mutation remains separate and deferred.
- **Status:** Active / first SadConsole seed implemented for Preview runtime entity -> authored entity template jumps when registry provenance exists; broader Simulation/log/action-plan source jumps still need provenance follow-up.

### FED-009: Live hot-editing and runtime debug mutation are deferred debugger capabilities

- **Decision:** Live content editing while Simulation continues, and direct runtime debug mutation inside Simulation, are deferred.
- **Reasoning:** Both weaken the simple materialized-runtime boundary and likely require Core/Content coordination.
- **Implications:** Reassess after Editor -> Preview -> Simulation is established. A possible future path is debug-only actions/primitives with traceable Core-aware outcomes.
- **Status:** Deferred.

### FED-010: Cached Editor snapshots refresh explicitly and stale Preview is cleared

- **Decision:** SadConsole Editor mode treats its read-only authored-content browser as a cached snapshot. `R` refreshes/revalidates through shared editor services and clears/marks Preview stale; `P` explicitly rematerializes turn-0 Preview.
- **Reasoning:** This keeps redraws responsive and preserves the authored-content/materialized-runtime boundary before mutation UI exists.
- **Implications:** Superseded for the componentized editor by FED-013. Keep this as historical context for the legacy read-only browser only; do not reintroduce separate `R`/`P` preview-stale controls unless a future richer preview surface proves that Save cannot be the primary refresh boundary.
- **Status:** Superseded by FED-013.

### FED-011: Editor navigation uses semantic focus targets

- **Decision:** Durable Editor-mode screens should move focus between semantic authored-content controls, fields, rows, cards, or grid cells through directional navigation, with Select activating the focused target and Cancel leaving or cancelling the current submode.
- **Reasoning:** The integrated editor should support controller-friendly workflows and avoid relying on hidden letter hotkeys or arbitrary screen-coordinate cursoring for normal editing. The entity-template editing mockup uses visible editable-field regions as the intended navigation targets.
- **Implications:** Current hotkey-heavy SadConsole editor mutation flows are prototype debt. Upcoming entity-template editing should use a dedicated field-focused layout for template identity, presentation, metadata, default action plan, targeting fields, and eventually inventory cells. Activating fields must still call shared editor/content services rather than introducing frontend-owned authoring semantics.
- **Status:** Active.

### FED-012: Existing SadConsole shell is legacy reference; new work uses componentized screen models

- **Decision:** The existing monolithic SadConsole shell/list-detail implementation is deprecated as legacy reference. New SadConsole exploration work should build reusable, testable screen/component models first, then attach SadConsole rendering and input adapters.
- **Reasoning:** The current implementation proved catalog launch, editor-service-backed mutation, preview/materialization, Simulation launch/return, and runtime play/debug paths, but it stayed too close to Console-inspired row-list rendering and accumulated too much shell-owned drawing/input behavior.
- **Implications:** Keep only the minimum legacy Simulation Play stopgap needed while replacing it with clean architecture slices. Durable screens should model selection, focused components, contextual controls, and authored/runtime data boundaries explicitly. Do not bypass shared editor/content/runtime services while rebuilding. Do not add new editor features to the legacy shell.
- **Status:** Active / implemented. The componentized SadConsole editor is now the default launch path. The former user-launchable `--beta-editor` path has been removed; legacy shell usage is internal stopgap only.

### FED-013: Save is the editor refresh boundary for authored preview state

- **Decision:** In the componentized SadConsole Editor, dirty authored content and stale preview state are treated as one user-facing condition. Saving clears dirty state and refreshes the current scenario preview boundary.
- **Reasoning:** The separate legacy `R` refresh / `P` preview-rematerialize model was useful while browsing was read-only, but became unnecessary friction once service-backed mutation and save affordances were present. Authors need one obvious recovery action for “my editor view/preview is stale.”
- **Implications:** The Scenario Edit save-status panel, `S` hotkey, and unsaved-exit modal are the canonical first Save/Preview UX. Future richer preview rendering may refine what is refreshed, but should preserve Save as the primary user-facing stale-state resolution unless performance or shared-service constraints force a split.
- **Status:** Active.

### FED-014: Dense spatial and sequence editors may use visible hotkey-first controls

- **Decision:** The normal directional + Select/Cancel model remains preferred, but dense spatial or ordered-sequence authoring modes may expose high-frequency actions through visible hotkeys when Enter-only operation would be clumsy. Current approved examples are inventory grid editing and action-plan sequence editing.
- **Reasoning:** Large-grid editing and ordered step editing need fast place/delete/move/insert operations. Forcing every operation through a Submit-only menu slows authoring and makes the UI less usable.
- **Implications:** Hotkey-first modes must clearly show contextual controls, keep Esc as cancel/back, and route mutations through shared editor services. These exceptions should be explicit and local to the submode; do not use them as permission to hide ordinary durable workflows behind undocumented keys.
- **Status:** Active.

### FED-015: Content-management actions use pinned create rows plus per-item action modals

- **Decision:** Scenario Edit lists that manage authored definitions use a pinned Create row and per-existing-item action modal. Entity templates use `2.3 Create New Template` plus `2.3.1 Edit/Duplicate/Delete`; action plans use `2.4 Create New Action Plan` plus `2.4.1 Edit/Duplicate/Delete`.
- **Reasoning:** This keeps creation discoverable without requiring a global command palette, and prevents selection of an existing definition from immediately committing to edit when duplicate/delete may be intended.
- **Implications:** Duplicate should request a new name before creating and then open the duplicate. Delete should use a confirmation modal before calling shared editor services. Future lists of authored definitions should prefer the same pattern unless a more scalable management surface is designed.
- **Status:** Active.

### FED-016: Component gallery is the executable SadConsole pattern reference

- **Decision:** The SadConsole component gallery should expand with each reusable component or adopted SadConsole pattern, with working examples that are interactive through the frontend whenever practical and isolated enough to inspect without entering a full editor workflow.
- **Reasoning:** The gallery gives future frontend work a live, code-backed reference for accepted implementation patterns such as panels, lists, editable fields, overlays, fixed-cell grids, color swatches, and future mouse/scrolling/layout patterns. This is more reliable than re-deriving SadConsole API usage from memory, especially in fresh implementation sessions.
- **Implications:** When a reusable component or SadConsole feature pattern is accepted, add or update a gallery example and keep the example simple enough to copy from. Do not create a separate SadConsole-pattern manual by default; record the reason and UX decision here, then point implementers to gallery/code examples and official SadConsole docs when no project pattern exists.
- **Established patterns so far:** componentized screen models over direct shell drawing; child `Console` overlays for modal/popup layers; `SadConsoleRect.FromSize(...)` for height-based overlay sizing; theme-owned border/color/glyph styling; renderer-owned CP437 glyph index `219` for color swatches; fixed-position inventory grid cells with cursor highlight as presentation state; text/int/choice/confirm overlays as reusable field editors; persistent footer/context controls for current focus/submode; inventory-space component with renderer-neutral viewport/cell geometry, backdrop/entity/decorator layer separation, executable gallery example, mixed-scale display profiles, shared pixel presentation geometry, explicit root-cell pixel metrics, and facing decorators as SadConsole `CellDecorator` overlays.
- **Status:** Active.

### FED-017: Look up SadConsole docs before inventing unproven feature patterns

- **Decision:** If a requested frontend behavior involves SadConsole layout, rendering, input, controls, surfaces, fonts/glyphs, animation/effects, mouse interaction, scrolling, or layering and it does not cleanly match an established project pattern, consult official SadConsole documentation or reference material before implementing.
- **Reasoning:** SadConsole has built-in concepts that are easy to misuse or unnecessarily reimplement. Checking the docs first helps the project benefit from the breadth of the framework while still promoting only accepted patterns into the component gallery and decisions trace.
- **Implications:** Prefer adopted gallery patterns when they exist. When no adopted pattern exists, research SadConsole docs, implement the smallest useful experiment, add a gallery example if accepted, and record any durable UX/API decision here.
- **Status:** Active.

### FED-018: Play-mode action prompts use numbered components and a Select/Cancel stack

- **Decision:** The componentized play-mode UX uses `0` for the play screen, `0.1` for HUD, `0.2` for current place, `0.2.1` for the action selector, and `0.3` for inspection/player-inventory panels. Enter/Select advances through action -> target/source -> destination, while Esc/Cancel returns one prompt layer or closes the selector without submitting.
- **Reasoning:** This names the player-facing workflow consistently with other componentized screens and keeps action choices out of the HUD. Spatial choices happen on spatial panels, and inventory choices happen through the player entity's inspection panel.
- **Implications:** Future pickup/drop/target prompts should consume Core Action Choice facts for selectable/highlighted choices and should not reintroduce HUD-only option rows as the primary action selector. Mouse selection can be added later over the same component/choice model.
- **Status:** Active / initial Play UX mock implementation.

### FED-019: SadConsole uses a square-tile rendering baseline

- **Decision:** SadConsole frontend presentation should move toward a square-cell tile font as the baseline graphics paradigm, initially targeting 8x8 cells. Text remains part of the UI, but new player-facing gameplay facts should not default to terminal-style text dumps when a square-tile visual treatment is needed for understanding.
- **Reasoning:** The current UI exposes the correct player-facing information but often does so textually and with IBM/DOS-like assumptions. Establishing the square-tile baseline early lets text, entity glyphs, decorators, borders, menus, and future sprites share one rendering paradigm and reduces regression risk as new canonical actions are added.
- **Implications:** Each new canonical action vertical slice should identify exposed player-facing facts, choose existing graphical treatments or prototype new ones in the component gallery, and record accepted reusable treatments in standards/decisions. Existing components may migrate gradually; this decision does not require converting the entire play surface in one pass.
- **Status:** Active.

### FED-020: Frontend refactors stop at shared-service ownership boundaries

- **Decision:** When SadConsole refactoring reveals missing shared action/session/editor capability, the frontend should stop at the boundary, call the owning layer, and only continue after the shared capability is provided or a follow-up is logged. Frontend code should not paper over missing Core/Content behavior with direct semantic execution, direct YAML/content mutation, or frontend-only legality rules.
- **Reasoning:** The frontend refactor/consolidation sprint found direct SadConsole action-step execution only because play-mode prompt/session responsibilities were separated. Calling Core-owner produced a narrow shared `AuthoredStep` Action Choice/session path, after which SadConsole could remove `ActionPlanInterpreter`, direct trace recording, and direct turn advancement. This was safer than preserving or expanding the frontend workaround.
- **Implications:** Future frontend cleanup should prefer explicit seams, targeted ownership escalation, and grep/test checks for disallowed semantic calls. Good practice: extract one frontend state machine at a time, keep service-backed mutation/application in an existing façade until ownership is clear, and run targeted tests before the full suite. Bad practice: turning frontend helper classes into semantic layers, hiding broad direct Core execution behind “convenience” wrappers, or allowing brittle test fixture data to masquerade as product regressions.
- **Status:** Active.

### FED-021: Mock play-mode layout resolves through named layered regions

- **Decision:** Mock play mode uses `GameplayMockLayout.Resolve(width, height)` as the accepted first layout resolver seam. It emits named layered regions for `0`, `0.1`, `0.2`, `0.3`, `0.2.1`, and `0.diagnostics`, keeps compatibility bounds on `GameplayMockFrame`, and uses narrow ratio/gap/min-size split helpers rather than a broad layout DSL.
- **Reasoning:** This proves the UI-M01/UI-M02 shape with minimal behavior churn in `GameplayMockScreen` / `GameplayMockConsole`. Named regions, explicit layers, pure tests, and compatibility bounds let future componentized play-mode work migrate renderer responsibilities gradually.
- **Implications:** Future play/debug layout work should prefer pure resolver output over inline `BuildFrame` arithmetic. Overlay placement should flow through named regions where practical. `F12` layout debug, live topmost-region hover diagnostics, and `F11` logical-console re-resolve are accepted debug seams, but `F11` does not imply dynamic OS-window resizing. Mouse diagnostics must remain non-mutating unless a later feature explicitly routes clicks through shared action/editor services.
- **Status:** Active.

### FED-022: Consumer Play mode starts as an isolated SadConsole mode

- **Decision:** The first consumer-facing Play mode should start as a new isolated componentized mode inside `src/GameGameGame.SadConsole`, launched from a new `Play` scenario option. The existing debug Simulation/play path remains available but is relabeled `Debug`; `Edit` remains the editor route.
- **Reasoning:** This creates a clean final-frontend growth path without duplicating SadConsole bootstrap, catalog/session wiring, component-gallery patterns, or shared-service consumption in a separate project too early. It also avoids extending the legacy debug play surface as the player-facing UX.
- **Implications:** The MVP Play route renders only the controlled actor's current inventory space through a reusable layered inventory-space component in normal mode. Debug/editor workflows stay accessible but should not define the consumer Play UX. A separate frontend project can be reconsidered later if packaging, asset pipeline, final-engine choice, or product separation requires it.
- **Status:** Active / implemented by `docs/Archived/New-Play-Mode-MVP-Sprint-Plan.md`.

### FED-023: Canonical Push uses target-then-direction prompts in Consumer Play mode

- **Decision:** Consumer Play mode presents canonical Push through the existing action-selection stack: choose Push/target from Core `ActionChoiceKind.Push`, then choose a valid target-relative direction from `ActionChoice.PushDirections(targetId)`. If only one valid target and direction exist, the existing candidate auto-submit behavior may submit it directly.
- **Reasoning:** Push is the first promoted canonical action that needs both an entity target and a target-relative direction while keeping the actor stationary. Reusing the prompt stack preserves Select/Cancel behavior and keeps legality in Core.
- **Implications:** Future target-plus-parameter actions should copy this pattern only when their parameter facts are exposed by shared Action Choice contracts. Frontend prompts may filter or focus valid options but must not invent legality. Direction keys in a Push direction prompt select the push direction, not actor movement.
- **Automated trace:** `ConsumerPlayModeSizeCalibrationCanPushNestedBagThroughContextPrompt` and `ConsumerPlayModeCanonicalPushShowcaseCanPushPlayerBlock`.
- **Status:** Active.

### FED-023: Consumer Play mode owns fullscreen display chrome and drawable bounds

- **Decision:** Entering the new consumer `Play` route switches fullscreen through SadConsole host APIs, resizes the SadConsole render output with the host, resolves logical play cells from available display pixels divided by the active scaled Candii tile size, and reserves a fixed one-tile outside border buffer. Gameplay/content components receive only the inner drawable bounds. The border buffer uses Candii glyph `181`; normal mode draws it black-on-black and `F12` toggles debug mode by changing the border foreground to red and drawing topmost debug glyph/text aids.
- **Reasoning:** The player-facing mode needs a stable screen bootstrap boundary before adding more gameplay UX. Treating fullscreen metrics, the solid border buffer, and `F12` debug visibility as Play-mode presentation state keeps gameplay/content semantics out of the display shell and makes drawable-area assumptions testable.
- **Implications:** New Play-mode components must be placed inside the resolved drawable bounds and must not use the outer border for gameplay. Normal Play mode should remain visually clean and render only player-facing content; scaffold/status/display facts belong behind `F12`. Pixel-perfect centering of the final tile surface inside leftover monitor pixels and semi-transparent debug overlays remain deferred display/render-layer work.
- **Status:** Active.

### FED-024: Play-mode gameplay capture is frontend-owned debug tooling

- **Decision:** Consumer Play mode may expose `F10` as a debug recording toggle. Starting capture saves the current player-control frame; while recording, each successful player-control submission queues another frontend screenshot after redraw; stopping capture keeps a single still or writes `gameplay.gif` when multiple frames were captured.
- **Reasoning:** Screenshot/GIF capture records presentation output and does not create gameplay, authoring, materialization, or action semantics. Capturing only player-control frames matches the current user need without requiring Core to expose intermediate autonomous-turn render snapshots.
- **Implications:** Capture belongs in SadConsole/MonoGame rendering code and should stay debug-labeled. If future UX needs every initiative/autonomous step, coordinate with Core/Content for shared history/render snapshot support rather than inferring hidden turns in the frontend.
- **Status:** Active / initial implementation.

### FED-025: Mixed-scale inventory spaces use shared presentation geometry and decorator overlays

- **Decision:** Consumer Play-mode inventory spaces may render at relationship-tier-specific pixel zooms, but every mixed-scale space must resolve through shared presentation geometry with explicit root-cell pixel metrics. Facing is rendered as a yellow Candii arrow `CellDecorator` with SadConsole `Mirror` flags over the entity glyph, not as identity glyph replacement.
- **Reasoning:** The mixed-scale sprint showed that applying child surfaces directly to Play mode without a geometry/layer/performance foundation caused connector, tooltip, overlay, and redraw problems. A geometry-first path lets rendering, connectors, hit-testing, and occlusion agree about where entities actually appear. SadConsole's `CellDecorator` and `Mirror` APIs provide the needed facing treatment while preserving entity identity.
- **Implications:** Future mixed-scale work should pass active root-cell pixel metrics into sizing/geometry, avoid hidden 16px assumptions, and treat the current per-cell child-console renderer as accepted-for-now rather than the final rendering architecture. `Micro4` remains a non-Candii summary renderer; it may carry a desire for state presentation, but needs a separate micro-state marker policy before claiming glyph-facing support.
- **Status:** Active.

### FED-026: New SadConsole frontend project supersedes old project for active player-facing work

- **Decision:** New active frontend work starts in `src/GameGameGame.Frontend.SadConsole`, with tests in `tests/GameGameGame.Frontend.SadConsole.Tests`. The existing `src/GameGameGame.SadConsole` project remains buildable/reference-only until useful components, tests, patterns, glyph decisions, and display lessons have been mined, but the new project must not reference it.
- **Reasoning:** The multi-document content/workspace refactor intentionally broke assumptions in the old frontend. Starting a clean project avoids preserving legacy Debug/Edit shell architecture while still allowing SadConsole research and component patterns to be cannibalized deliberately.
- **Implications:** The first checkpoint is a workspace-backed scenario browser that shows `debug-room`; Play mode is rebuilt after that checkpoint. Debug mode is abandoned as a first-class route. Editor mode is deferred and should be reinvented around shared workspace/editor services. Reusable components promoted into the new frontend should receive readable gallery examples and focused tests once stable.
- **Status:** Active / spike sprint planned by `docs/Plans/Frontend-SadConsole-Workspace-Browser-Sprint-Plan.md`.

### FED-027: Frontend.SadConsole consumes workspace scenario services rather than an API v2

- **Decision:** Do not create a parallel API v2 for the new frontend. Instead, add or evolve a Content-owned, frontend-neutral workspace scenario catalog/launch facade over `ContentWorkspace`, scenario catalog projections, and `PlayableScenarioLauncher.CreateFromWorkspace(...)`.
- **Reasoning:** The changed requirement is workspace composition and multi-file scenario launch, not a SadConsole-specific API compatibility break. A shared service keeps scenario discovery/materialization frontend-neutral and avoids freezing a premature version boundary.
- **Implications:** `DebugRoom.yaml` must be discovered/launched through workspace-aware services because it depends on canonical content files. File-local launch helpers may remain compatibility paths, but the new frontend should not depend on them for multi-file content. Frontend tests may assert request mapping and display of returned diagnostics, not catalog/materialization semantics.
- **Status:** Active.

### FED-028: Candii is treated as a size-parity tileset family

- **Decision:** The new `GameGameGame.Frontend.SadConsole` treats Candii assets as one tileset family with size variants. A glyph index in `Candii` 8x8 and the same glyph index in `Candii16` 16x16 represent the same semantic tile. Presentation mappings and role definitions from the Candii tileset manifest are size-independent unless a future manifest explicitly overrides a role for a size.
- **Reasoning:** The frontend already depends on manifest-owned glyph facts such as the Candii blank tile and panel-border roles. Making glyph parity explicit lets renderers switch between 8x8, 16x16, and future sizes without duplicating presentation mappings or inventing renderer-local glyph tables.
- **Implications:** Frontend renderers should resolve text blank, entity presentation glyphs, panel borders, grid/backdrop roles, and future sprite roles through the tileset family manifest instead of hardcoding ASCII/CP437 assumptions. The manifest should remain flexible enough to add size variants, optional accent layers, and animations later. Do not split Candii into many category sheets or implement accent/animation rendering until asset pressure or Play-mode requirements justify it.
- **Status:** Active / first applied to the new scenario browser modal border and blank glyph handling.

### FED-029: Play visual semantics belong in a separate visual-language source

- **Decision:** Keep `Frontend-UX-Decisions.md` as the frontend-owner rationale and implementation-memory log, and use `Frontend-Play-Visual-Language.md` as the canonical player-facing visual semantics document for the new `GameGameGame.Frontend.SadConsole` Play mode.
- **Reasoning:** The project needs two different documentation products: durable memory of why code and UX choices were made, and a clean rulebook for what players should infer from highlights, focus, overlays, action rows, and future indicators. Mixing them would make the visual-language spec too implementation-heavy and make the decision log too normative for player-facing semantics.
- **Implications:** When implementing a feature, record rationale, alternatives, code anchors, tests, and follow-up risks here. When promoting a visible Play concept into stable player-facing language, add or update the visual-language document. Old frontend UX specs remain reference/audit material and should not define new prompt/menu assumptions for `GameGameGame.Frontend.SadConsole`.
- **Status:** Active.

### FED-030: Play highlights communicate expected action, not generic interest

- **Decision:** In new Play mode, grid highlights communicate what confirming/selecting the highlighted cell will do. Current implemented variants are movement preview and entity-target inspection/action focus.
- **Reasoning:** The same adjacent cell can imply different outcomes: moving to an empty destination versus focusing actions for an occupied entity. A single generic highlight would hide the actual consequence of confirmation. Distinct highlights preserve player intent and prevent accidental movement/action confusion.
- **Implementation anchors:** `PlayHighlightState`, `CellHighlightKind.MovePreview`, `CellHighlightKind.EntityTarget`, `CellHighlightPresentation`, `TilesetRoles.MoveHighlight`, `TilesetRoles.EntityHighlight`, and `PlayModeConsole.ResolveHighlight(...)`.
- **Implications:** Future action-specific highlights may be introduced when exactly one action is available, but only after that action's UX semantics are designed. Highlights should remain overlays/decorators rather than replacing entity glyph identity, preserving FED-004/FED-005.
- **Status:** Active / initial rules copied into `Frontend-Play-Visual-Language.md`.

### FED-031: Play focus routing locks input to the focused owner

- **Decision:** Play mode routes keyboard input by focus owner. When inspection actions have focus, panel input is consumed by `PlayInspectionInputController`; movement aim, movement-key release clearing, and inspected-cell changes do not fall through to the grid.
- **Reasoning:** Focus must be semantic, not just visual. Without explicit routing, panel interaction can accidentally mutate grid aim or selection behind the overlay, making action selection unreliable and undermining future picker/player-panel workflows.
- **Implementation anchors:** `PlayFocusMode`, `PlayInspectionInputController`, `PlayModeConsole.ProcessKeyboard(...)`, `PlayInspectionController.FocusActions()/ReturnToGrid()`, and tests in `PlayInputControllerTests`.
- **Implications:** Future player inventory panels and action pickers must become explicit focus owners before accepting input. `Esc`/Left-style return paths should move focus visibly and semantically back to the grid or previous owner.
- **Status:** Active / checkpoint `e4ba3b6 Harden play inspection focus input`.

### FED-032: New Play action session controller owns execution facts, not selection UX

- **Decision:** `PlayActionSessionController` centralizes Core action-choice submission/session facts for the new frontend, while action-selection workflows remain frontend-designed and action-specific.
- **Reasoning:** The old frontend's session controller had valuable Core orchestration responsibilities, but its prompt/menu conventions are not automatically valid for the new frontend. Separating execution facts from selection semantics lets the new UI use current overlay/focus patterns without duplicating Core `ActionChoiceService` requests or inheriting legacy prompt assumptions.
- **Implementation anchors:** `PlayActionSessionController`, `PlayMovementController`, `PlayModeConsole`, `EntityInspectionPanelModelFactory`, and `InspectionActionChoiceProjector`.
- **Implications:** Movement and inspection action rows should consume the controller-owned current `ActionChoiceRequest`. Do not expose old `PlayModeIntentSeed`, prompt stack, menu ordering, or shortcut assumptions through the session controller. Coordinate with Core/Content if missing submission/session capabilities appear.
- **Status:** Active / checkpoint `ae7e85f Add play action session groundwork`.

### FED-033: Action candidates are inert infrastructure until per-action UX is designed

- **Decision:** New `PlayActionCandidate` and `PlayActionPromptLayer` data can be projected onto inspection/player action rows, but prompt focus, picker overlays, auto-submit, candidate ordering, and action confirmation semantics are deferred until after the player inventory/self-inspection overlay exists and then designed one action at a time.
- **Reasoning:** Building all prompt behavior now would either import old frontend workflow assumptions or prematurely commit to interaction semantics before the inventory overlay exists. Structured candidate data is still useful infrastructure: rows can carry Core-derived action facts without each panel re-reading affordances or relying on text-only “not wired yet” placeholders.
- **Implementation anchors:** `PlayActionCandidateModel.cs`, `EntityInspectionActionRow.Candidate`, `InspectionActionChoiceProjector`, `PlayActionCandidateResolver`, and `PlayActionCandidateModelTests`.
- **Implications:** Candidate outcome terminology should avoid implying visible prompt behavior has happened; use “follow-up needed” rather than “prompt opened” for inert classification. The first concrete action workflow should update the visual-language spec and decision log for that action only, without claiming global prompt semantics.
- **Status:** Active / infrastructure checkpoint only.

### FED-034: Player inventory panel is always visible and `I` toggles focus

- **Decision:** The new Play mode shows the controlled actor's player panel as an always-visible bottom-left overlay. Pressing `I` toggles focus between the grid and the player panel; `Esc`/Left also returns panel focus to the grid. The panel attempts to show the actor's inventory immediately by projecting cells from the actor's registered inventory plane.
- **Reasoning:** The player inventory/status surface is needed before designing action prompt semantics. Making it always visible avoids prematurely deciding open/close lifecycle and lets focus semantics be tested independently from action execution. Reusing entity-inspection overlay/panel/mixed-Candii patterns keeps visual language consistent and avoids inventing another panel framework.
- **Implementation anchors:** `PlayPlayerPanelController`, `PlayInputController.TogglePlayerPanel`, `PlayModeConsole.HandlePlayerPanelKeyboard(...)`, `PlayModeInspectionLayout.PlayerPanelBounds`, `InspectionInventoryProjector`, and `MixedTilesetPlayspaceOverlay` inventory drawing.
- **Implications:** Player-panel action execution remains inert until action-specific workflows are designed. Future layout work may revise always-visible placement, but should preserve explicit focus ownership and avoid grid input fallthrough while the panel is focused.
- **Status:** Active / first implementation.

### FED-035: Inspection action highlight resolution has a shared seam

- **Decision:** Inspection-focused entity highlights resolve through `PlayActionHighlightResolver`, which maps the currently selected inspection action row to a highlight kind. The initial refactor only distinguishes selectable rows, which keep generic entity-target language, from greyed-out rows, which use no-action language. Action-specific mappings such as Pickup are intentionally added one at a time.
- **Reasoning:** The player-facing rule is that highlights communicate the action to be taken, but bulk-wiring every action highlight would create visual promises before each action workflow is designed. A shared resolver seam lets us add and test one action highlight at a time without repeatedly changing `PlayModeConsole` or renderer code.
- **Implementation anchors:** `PlayActionHighlightResolver`, `PlayInspectionController.FocusedActionHighlightKind()`, `PlayModeConsole.ResolveEntityTargetHighlightKind()`, `CellHighlightKind.NoAction`, and tileset roles `pickupHighlight`/`dropHighlight`/`enterHighlight`/`exitHighlight`/`transferHighlight`/`noActionHighlight`.
- **Implications:** Next action-specific highlight should be Pickup. Unavailable action rows should consistently resolve to no-action. Other valid action rows should remain generic `EntityTarget` until their semantics are explicitly promoted.
- **Status:** Active / refactor seam implemented.

### FED-036: Pickup is the first action-specific inspection highlight

- **Decision:** A selectable Pickup action row maps the inspected entity highlight to `CellHighlightKind.Pickup` / `pickupHighlight`. Greyed-out Pickup rows continue to map to no-action.
- **Reasoning:** Pickup is the first likely concrete non-move action path and is simple to distinguish from generic entity inspection without committing to all action-specific highlight semantics at once.
- **Implementation anchors:** `PlayActionHighlightResolver`, `CellHighlightPresentation.Pickup(...)`, `TilesetRoles.PickupHighlight`, and `PlayActionCandidateModelTests.ActionHighlightResolverUsesPickupForSelectablePickupRows`.
- **Implications:** Other valid action rows remain generic `EntityTarget` until their UX semantics are intentionally promoted. Pickup execution/destination selection remains deferred.
- **Status:** Active / first action-specific highlight implemented.

### FED-037: Pickup uses focused inventory-cell destination selection

- **Decision:** Confirming a selectable Pickup inspection action enters a focused inventory selection state over the controlled actor's inventory. Empty valid inventory cells use pickup highlight language; occupied or invalid cells use no-action highlight language. Confirming an empty valid cell submits Pickup through `PlayActionSessionController`; occupied/invalid cells cannot be selected.
- **Reasoning:** Pickup requires a destination that is not necessarily adjacent in the world grid. Reusing the always-visible player inventory panel makes the destination explicit and keeps legality in Core `ActionChoiceRequest` destination facts rather than adding frontend placement rules.
- **Implementation anchors:** `PlayInventorySelectionController`, `PlayModeConsole.HandleInventorySelectionKeyboard(...)`, `PlayActionSessionController.SubmitPickup(...)`, `InspectionInventoryProjector` inventory highlights, and `PlayActionCandidateModelTests.InventorySelectionConfirmsPickupIntoSelectedPlayerInventoryCell`.
- **Implications:** This establishes inventory-cell selection as a focus/input concept, but only for Pickup so far. Other actions that need source/destination selection should reuse the focus model only after their visual and input semantics are designed. Future work should generalize the controller beyond Pickup once a second action path needs it.
- **Status:** Active / first functional Pickup slice.

### FED-038: Play input is organized as a selection stack

- **Decision:** New Play mode names and routes interaction through a selection stack: Adjacent selection, Action selection, and Cell selection. Only the top selection frame receives input. Lower frames remain contextual but cannot mutate while a deeper frame is active. Successful action submission clears the stack back to Adjacent selection for the next turn.
- **Reasoning:** Pickup exposed a focus inconsistency: inventory-cell selection could visually coexist with adjacent/action context, but lower selections must not change while the player is choosing a destination cell. The stack vocabulary gives future action workflows a precise design shape and prevents accidental fallthrough between grid aim, action rows, and cell pickers.
- **Implementation anchors:** `PlaySelectionStack`, `PlaySelectionFrameKind`, `PlayModeConsole.ProcessKeyboard(...)`, `PlayModeConsole.ResolveAdjacentSelectionCoord()`, and `PlaySelectionStackTests`.
- **Implications:** Define each action workflow as a stack before implementation. Current stacks: Move = Adjacent empty cell -> submit; Pickup = Adjacent entity -> Inspection action Pickup -> Player inventory cell empty valid destination -> submit. Cancelling cell selection currently pops back toward action/adjacent context; successful Pickup returns to Adjacent selection and never to the inspection panel.
- **Status:** Active.

### FED-039: Release-oriented SadConsole uses DirectX with overlay-safe borderless by default

- **Decision:** `GameGameGame.Frontend.SadConsole` is treated as the release-oriented SadConsole app on Windows and uses the MonoGame WindowsDX backend. Its default player-facing window mode is overlay-safe borderless; F11 toggles between overlay-safe borderless and windowed until a full settings screen exists.
- **Reasoning:** Windows Game Bar and common capture/overlay tools are more reliable with DirectX than DesktopGL/OpenGL. Exact-monitor borderless windows can still be promoted into fullscreen-like presentation paths where overlays receive input but are not visibly composited, so the default borderless mode intentionally avoids exact monitor dimensions.
- **Implementation anchors:** `MonoGame.Framework.WindowsDX`, `FrontendWindowMode.OverlaySafeBorderlessWindowed`, `SadConsoleDisplayHost.ApplyWindowMode(...)`, and `ScenarioBrowserChromeState.ToggleWindowMode()`.
- **Implications:** Keep the legacy/debug `GameGameGame.SadConsole` project separate for now. Future release settings UI should preserve a simple player choice between overlay-safe borderless and windowed unless a tested exclusive/fullscreen mode is explicitly added.
- **Status:** Active / initial DirectX overlay-safe default implemented.

### FED-040: Drop starts from the player inventory action menu

- **Decision:** In new Play mode, Drop is selected from the focused player inventory panel rather than from the inspected-entity action panel. Selecting Drop enters inventory source-cell selection, then adjacent world destination-cell selection, then submits through the action session controller.
- **Reasoning:** Drop is actor/inventory-contextual rather than target-inspection-contextual. The actor-centric Core `ActionChoiceRequest` already exposes Drop source entities and per-source destinations, so the frontend can populate the player inventory action row and selection highlights without inventing portability, adjacency, or empty-destination legality.
- **Implementation anchors:** `PlayActionCandidateProjector.ForPlayerInventory(...)`, `InspectionActionChoiceProjector.ProjectPlayerInventory(...)`, `PlayPlayerPanelController.SelectedActionRow`, `PlayInventorySelectionController.TryBeginDropSource()/ConfirmDropSource()/ConfirmDrop()`, and `PlayActionSessionController.SubmitDrop(...)`.
- **Implications:** Future self/inventory/context actions such as Exit or Transfer should start from actor-level Action Choice facts when they are not naturally inspected-target actions. Player-panel action ordering, wording, and layout remain provisional.
- **Status:** Active / initial functional Drop workflow.

### FED-041: Enter submits inspected target and Exit uses Core direction destinations

- **Decision:** In new Play mode, Enter is selected from an inspected adjacent entity and submits immediately with the selected target entity. It does not prompt for an inventory destination cell because Core Enter choices do not expose that payload. Exit is selected from the player inventory panel and enters direction selection; the highlighted destination is the Core-projected `DirectionOption.Destination`, and submission sends only the selected direction.
- **Reasoning:** Current Core `ActionChoiceKind.Enter` exposes target entities only, while `ActionChoiceKind.Exit` exposes direction options with destination hints. The frontend should preserve those semantics rather than inventing destination-cell selection or parent-relative geometry rules.
- **Implementation anchors:** `PlayActionSessionController.SubmitEnter(...)`, `PlayActionSessionController.SubmitExit(...)`, `PlayInventorySelectionController.TryBeginExitDestination()/ConfirmExit()`, and `PlayActionCandidateProjector.ForPlayerInventory(...)`.
- **Implications:** If future UX wants explicit Enter placement, Core should first expose destination choices and an Enter submission payload that includes the chosen destination. Exit visual selection may feel adjacency-like, but must continue to render Core-projected destinations rather than recomputing topology in the frontend.
- **Status:** Active / initial functional Enter and Exit workflow.

### FED-042: Transfer is one counterparty-first action with Give/Take item labels

- **Decision:** In new Play mode, Transfer starts from an inspected adjacent counterparty and appears as one inspection action even when authored plans expose multiple Transfer steps. Selecting Transfer opens an item picker popup over Core-projected `TransferItems(counterpartyId)`. Items from the actor inventory are labeled as Give, items from the counterparty inventory are labeled as Take, the selected item is highlighted with transfer language in the owning inventory panel, and submission sends only `counterpartyId` plus `movingEntityId` through the action session controller.
- **Reasoning:** Core models Transfer as one action choice rather than separate Give and Take actions. A counterparty-first workflow maps directly to `ActionChoiceKind.Transfer`, keeps adjacency and item legality in Core, and still gives players clear direction labels.
- **Implementation anchors:** `PlayInventorySelectionController.TryBeginTransferItems()/MoveTransferItem()/ConfirmTransfer()`, `PlayActionSessionController.SubmitTransfer(...)`, and `PlayActionCandidateProjector.TransferCandidates(...)`.
- **Implications:** Future Give/Take affordances may be added as filtered shortcuts over the same Transfer choice, but should not become separate frontend legality paths unless Core exposes separate action semantics.
- **Status:** Active / initial functional Transfer workflow.

### FED-043: Push uses canonical target-first Action Choice, not PushFacing

- **Decision:** In new Play mode, Push is selected from an inspected adjacent target and then chooses a Core-projected target-relative push direction. Submission sends the inspected target plus selected direction through the action session controller. The debug-room player action plan uses canonical `Push`, not legacy `PushFacing`, so Core exposes target-first `ActionChoiceKind.Push` rows.
- **Reasoning:** `PushFacing` is legacy facing/blocker behavior and does not mean “push the inspected adjacent target.” Canonical `Push` already exposes valid target entities and valid directions through shared Action Choice facts, which keeps target and direction legality out of the frontend.
- **Implementation anchors:** `PlayActionWorkflowController.TryBeginPushDirection()/ConfirmPush()`, `PlayActionSessionController.SubmitPush(...)`, `PlayActionHighlightResolver`, and `src/GameGameGame.Content/Canonical/Creatures/DebugPlayer.yaml`.
- **Implications:** Do not silently mutate actor Facing/Target to make Push rows appear. If a future bump-push UX is desired, design it separately over an explicit Core/shared capability rather than synthesizing target-first Push from `PushFacing`.
- **Status:** Active / initial functional Push workflow.
