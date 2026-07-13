# Frontend UX Decision Log

Status: Living decision log for frontend UX and mode-model decisions.

Read when:

- reviewing why a frontend UX standard exists;
- planning a SadConsole Simulation or Editor slice;
- deciding whether a new UI affordance conflicts with a prior decision.

Related documents:

- `docs/Source of Truth/Frontend-UX-Standards.md` records the current UI-bible standards.
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
- **Established patterns so far:** componentized screen models over direct shell drawing; child `Console` overlays for modal/popup layers; `SadConsoleRect.FromSize(...)` for height-based overlay sizing; theme-owned border/color/glyph styling; renderer-owned CP437 glyph index `219` for color swatches; fixed-position inventory grid cells with cursor highlight as presentation state; text/int/choice/confirm overlays as reusable field editors; persistent footer/context controls for current focus/submode.
- **Status:** Active.

### FED-017: Look up SadConsole docs before inventing unproven feature patterns

- **Decision:** If a requested frontend behavior involves SadConsole layout, rendering, input, controls, surfaces, fonts/glyphs, animation/effects, mouse interaction, scrolling, or layering and it does not cleanly match an established project pattern, consult official SadConsole documentation or reference material before implementing.
- **Reasoning:** SadConsole has built-in concepts that are easy to misuse or unnecessarily reimplement. Checking the docs first helps the project benefit from the breadth of the framework while still promoting only accepted patterns into the component gallery and decisions trace.
- **Implications:** Prefer adopted gallery patterns when they exist. When no adopted pattern exists, research SadConsole docs, implement the smallest useful experiment, add a gallery example if accepted, and record any durable UX/API decision here.
- **Status:** Active.
