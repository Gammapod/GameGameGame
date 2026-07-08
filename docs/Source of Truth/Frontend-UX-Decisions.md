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

- **Decision:** SadConsole is the current canonical debug/browser frontend direction. Console remains fallback/minimal tooling.
- **Reasoning:** The first frontend sprint validated that SadConsole can launch scenario catalogs, play through shared controlled-command services, and render entity-panel debug information.
- **Implications:** Rich frontend UX work should prefer SadConsole or shared frontend-neutral services unless Console fallback work is explicitly selected.
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
- **Implications:** Future mutations should use the same invalidation policy unless a shared service contract proves a preview remains valid. Mutation/save controls should remain absent or clearly disabled until a mutation design is promoted.
- **Status:** Active / Phase 5A hardening implemented.
