# Gamma Editor MVP Plan

Status: Archived backlog reference. Promote again only if a future roadmap selection chooses Gamma Editor MVP work.

Read when:

- selecting Gamma release work;
- deciding whether a frontend/editor request is Must Have or Could Have for Editor MVP;
- grouping SadConsole, shared-service, authoring, validation, preview, and simulation-return work around one release checkpoint.

Related source of truth:

- `docs/Source of Truth/Frontend-UX-Standards.md` records the Editor/Simulation mode model.
- `docs/Source of Truth/Frontend-UX-Decisions.md` records active frontend/editor UX decisions.
- `docs/Source of Truth/Frontend-Editor-Simulation-Flow.mmd` diagrams the preferred context flow.
- `docs/Source of Truth/Frontend-UX-Invariants.md` records frontend layer boundaries and test traces.
- `docs/Source of Truth/Engine-Editor-Capabilities.md` records implemented Core/Editor/frontend-facing support tiers.
- `docs/Source of Truth/Content-Authoring-Manual.md` records what authors can safely create today.
- `docs/Plans/SadConsole-Frontend-Roadmap.md` records the broader SadConsole/debug-browser roadmap.
- `docs/Source of Truth/SadConsole-UI-Specification.md` records reusable UI layout/layering/resizing/mouse/render-style specifications that should be consumed instead of duplicated here.

## Reset note

This plan preserves the previous Gamma Editor MVP release bar and implementation sequence. It is not currently an active implementation commitment after the major refactor/code cleanup and Delta selection; use it as backlog/context until roadmap priorities promote it again.

## Gamma release target

Gamma should prove that SadConsole can become the integrated game/editor frontend by delivering a useful **Editor -> Preview -> Simulation -> Return** loop over existing shared services.

The release is not trying to ship a polished final editor, a full replacement for all authoring workflows, or a live hot-reload debugger. It should instead make the happy path concrete enough that future work can be grouped around the editor surface rather than around separate Console, report, recorder, or mechanics-only tracks.

Target statement:

- A user can open authored content in SadConsole Editor mode.
- A user can browse, understand, and perform core service-backed edits for scenarios' templates, inventory layouts, and action plans without opening raw YAML first.
- A user can materialize a selected scenario as a turn-0 preview, launch Simulation mode from that preview, play/debug through shared runtime services, and return to the same editor context.
- Editor-like workflows consume shared content/editor services; Simulation consumes shared session/action/affordance/log/panel services.
- Runtime mutation, live hot-editing, and broad new gameplay mechanics remain deferred unless explicitly promoted by an Editor MVP blocker.

## Must Have requirements

These define the Editor MVP release bar.

### Shell and context

1. SadConsole has a clear top-level path to **Open Content File** and **Play Scenario from Catalog**.
2. Opening either path establishes an editor context backed by a content file/session where possible.
3. Scenario catalog launch preserves enough backing-content context to return to Editor mode after Simulation.
4. Editor mode and Simulation mode are visually and behaviorally distinct.
5. The UI labels authored source panels versus materialized runtime panels clearly.

### Read-only editor browser baseline

1. Browse scenarios in the opened content context.
2. Browse entity templates and authored carried inventory layouts at a useful summary level.
3. Browse action plans at a useful summary level, including canonical behavior-chain/action-step names where available.
4. Show references that are already available through shared content/editor services when practical, especially scenario -> root/player/template/action-plan relationships.
5. Use the componentized SadConsole editor as the default authoring surface for supported service-backed edits: template presentation/default-plan/targeting/inventory, action-plan step sequence edits, and template/action-plan create/duplicate/delete.

### Validation and authoring feedback

1. Run content validation through shared editor/content services.
2. Present validation diagnostics in Editor mode with severity, message, and relevant authored object context when available.
3. Distinguish validation/materialization errors from runtime observations and capability gaps.
4. Provide a clear refresh/revalidate action after file or session changes.

### YAML and diff visibility

1. Show generated/canonical YAML preview for the opened content/session when the underlying service supports it.
2. Show a diff/dirty-state surface sufficient to understand pending authored-content changes; current componentized editor baseline includes prominent dirty/saved status, `S` save, and unsaved-exit confirmation.
3. Treat YAML/diff views as inspection/confirmation surfaces, not as the primary mutation model for MVP.

### Scenario preview

1. Materialize a selected scenario at turn 0 from Editor mode.
2. Render the preview using the same entity-panel/grid/glyph standards as Simulation where practical.
3. Show materialization diagnostics and capability gaps in context.
4. Make it clear that preview state is derived runtime state, not the authored source itself.
5. Treat Save as the first preview-refresh boundary: authored mutations make content dirty/stale, and saving clears that state and refreshes preview facts. A richer preview surface may refine this later.

### Simulation handoff and return

1. Launch Simulation mode from the selected scenario/preview.
2. Simulation uses shared playable-session launch, controlled-command, affordance, history/log, and entity-panel projection services.
3. The user can return from Simulation to the prior Editor context.
4. Simulation state is not silently written back to authored content.

### Source navigation seed

1. Support at least one useful source-navigation path from preview or Simulation back to authored source when provenance exists, such as scenario diagnostic -> scenario entry or runtime entity -> authored template.
2. When provenance is missing, the UI should say so rather than guessing.

### Testing and documentation

1. Add frontend-owned tests for mode/context/view-model behavior where stable enough to pin.
2. Rely on existing Core/Content/Editor invariant traces for shared semantics; do not duplicate simulation rules in frontend tests.
3. Update source-of-truth docs when the support tier changes from planned/read-only to implemented.

## Could Have requirements

These are valuable, but should not block Editor MVP unless user testing shows they are essential.

1. Rich action-plan parameter/check/effect editing after a typed frontend projection/mutation contract is designed.
2. Rich action-plan preview panels with expanded step details, slot reads/writes, fallback summaries, and legacy/canonical shape classification.
3. Semantic reference browser: list all references to a template/action plan and jump between them.
4. Saved runlogs or history playback integrated into Editor mode.
5. SadConsole-rendered visual export replacing legacy PNG/GIF recorder workflows.
6. Apply UI-N05 from `SadConsole-UI-Specification.md` when adding mouse hit-testing for editor panels, source jumps, scenario selection, and action target selection.
7. Apply UI-M01/UI-M02/UI-N06 from `SadConsole-UI-Specification.md` when adding collapsible/pinned multi-panel editor layouts with reusable geometry.
8. Rich YAML path diagnostics for every validation issue.
9. Strict/canonical validation toggle and schema/catalog export surfaced in the UI.
10. Scenario curation UI for manifest ordering, descriptions, visibility, or deprecated/headless-only markers.
11. Batch/dry-run mutation workflows with ordered semantic diffs.
12. Runtime control-source / Action Choice promotion if direct-control compatibility becomes the main editor/simulation blocker.
13. Runtime debug mutation mode, clearly separated from authored-content editing.
14. Live hot-edit/re-materialize while Simulation is paused.
15. Packaging/distribution polish for external tester builds.
16. Scenario root/player-start editing if shared editor APIs expose safe mutations and validation.
17. Per-carried-instance initial facing/state in inventory authoring, once shared content/editor/materialization support exists.

## Explicit non-goals for Gamma Editor MVP

- Broad new gameplay mechanics by default, including template spawning, reactions, scheduler/speed, combat, or generalized runtime indexing.
- Retired Avalonia GUI parity.
- Direct YAML editing as the primary editor interaction.
- Frontend-only authoring concepts that cannot be represented by Core/Content/Editor services.
- Live hot-editing while Simulation continues running.
- Runtime state mutation presented as normal authored-content editing.
- Final frontend engine decision; SadConsole remains the current canonical debug/editor browser direction while evidence is gathered.

## Multi-phase implementation plan

### Phase 0: Alignment and inventory

Goal: make the Gamma Editor MVP checkpoint explicit and identify service gaps before UI work expands.

Scope:

1. Update active planning docs to point Gamma at this Editor MVP plan.
2. Inventory existing SadConsole shell, content/editor services, preview/materialization services, and tests against the Must Have list.
3. Record missing shared-service gaps in the roadmap or capability docs, not as SadConsole-only semantics.

Exit criteria:

- Must Have requirements are accepted as the Gamma release bar.
- Each Must Have has an owner lane: frontend-owned UI/view-model, content/editor service, Core/shared runtime service, or documentation/testing.

Current readiness note:

- Initial content/editor service readiness exists through `FrontendEditorService`, which exposes SadConsole-consumable read-only snapshots and turn-0 scenario previews over `ContentEditorSession`/`ContentEditorService` while preserving `AgentContentEditorApi` as a parallel editing surface.

### Phase 1: Editor context shell

Goal: establish the durable top-level mode/context model.

Scope:

1. Add or refine SadConsole navigation for Open Content File and Play Scenario from Catalog.
2. Create an Editor context abstraction over the existing content/editor session services.
3. Preserve backing content context when launching a catalog scenario.
4. Add return-to-editor navigation from Simulation.

Exit criteria:

- A scenario can be selected, launched into Simulation, and return to an Editor context without losing the selected content/scenario identity.

### Phase 2: Browser, diagnostics, and core authoring parity

Goal: make Editor mode useful for browsing and core service-backed authoring before full Simulation handoff.

Scope:

1. Browse scenarios, templates, carried layouts, and action plans.
2. Show validation diagnostics grouped by authored object where possible.
3. Show YAML preview, dirty state, and available diff surface.
4. Support the completed core authoring set through shared editor services: template presentation/default-plan/targeting/inventory, action-plan step sequence edits, template/action-plan create/duplicate/delete, save/dirty/unsaved-exit.

Exit criteria:

- A user can answer “what content is in this file and what is invalid?” and perform core service-backed template/action-plan/inventory edits from SadConsole without opening YAML.

### Phase 3: Scenario preview

Goal: bridge authored content and runtime inspection without entering full play.

Scope:

1. Materialize the selected scenario at turn 0 from Editor mode.
2. Render preview entity panels/grids using shared projection data where possible.
3. Show materialization diagnostics and capability gaps next to the preview.
4. Keep Save as the primary preview refresh boundary for authored mutations; add extra manual refresh/revalidate/rematerialize controls only if a richer preview surface proves they are still needed.

Exit criteria:

- A user can inspect the selected scenario's starting runtime shape and diagnostics before launching Simulation.

### Phase 4: Simulation handoff, history, and source jump seed

Goal: complete the editor-preview-play feedback loop.

Scope:

1. Launch Simulation from the preview/current scenario.
2. Preserve shared history/log/entity-panel behavior in Simulation.
3. Return to the same Editor context.
4. Implement one or more provenance-backed source jumps.

Exit criteria:

- A user can edit/browse context, preview, play/debug, and return to the same context with at least one traceable path from runtime/diagnostic facts back to authored source.

### Phase 5: MVP hardening and release decision

Goal: decide whether Gamma Editor MVP is ready for tester/developer feedback.

Scope:

1. Fix high-friction navigation, diagnostics, and preview readability issues.
2. Update source-of-truth capability and authoring docs to match implemented support.
3. Run targeted frontend tests plus relevant Core/Content/Editor tests for shared-service contracts.
4. Decide which Could Have item, if any, should become the first post-MVP slice.

Exit criteria:

- The Editor -> Preview -> Simulation -> Return loop is reliable enough to become the organizing checkpoint for subsequent Gamma work.
