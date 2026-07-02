# Frontend UX Standards

Status: Source of truth for frontend-facing presentation standards, design principles, and UI-bible guidance.

Read when:

- designing Console, SadConsole, or future frontend presentation;
- deciding whether a UI treatment is consistent with the entity-panel/debug-browser model;
- evaluating Stage 7+ information-density, log, glyph, cursor, highlighting, or panel changes;
- converting playtest/frontend feedback into UI standards or backlog items.

Related documents:

- `docs/Source of Truth/Frontend-UX-Invariants.md` records frontend layer boundaries and behavior constraints.
- `docs/Source of Truth/Frontend-UX-Decisions.md` records the chronological UX decision log behind these standards.
- `docs/Source of Truth/Frontend-Editor-Simulation-Flow.mmd` diagrams the current Editor/Simulation context model.
- `docs/Source of Truth/Entity-Panel-UX-Spec.md` records the entity-panel, breadcrumb, and log UX model.
- `docs/Plans/SadConsole-Frontend-Roadmap.md` records staged implementation work.

## Purpose

This document is the frontend/UI bible. It records design standards that may not have direct automated tests but should guide frontend implementation and review.

These standards are intentionally distinct from Core invariants. They should not create simulation semantics. They should shape how shared frontend-neutral facts are displayed, prioritized, and interacted with.

## Entity-neutral presentation

1. **Do not treat the player entity as visually special by default.**
   - If the UI shows a property for one entity, it should show that property for all relevant entities in the same way.
   - The entity that currently represents the player may be selected, focused, controlled, or centered by presentation state, but its runtime facts should not be displayed through a unique player-only model.
   - Player-focused convenience should be expressed as frontend focus/selection, not as special entity semantics.

2. **Entity panels remain entity panels.**
   - The current playspace, inspected object, carried inventory, and nested spaces should all be represented through the same entity-panel vocabulary where possible.
   - Avoid separate “map-only” or “player-only” widgets that duplicate panel facts unless a later design explicitly promotes them.

3. **Controls may be player-centric; facts should not be.**
   - Keyboard shortcuts may submit commands for the currently controlled entity.
   - The display of facing, target, logs, action state, inventory, and initiative should remain reusable for any entity.

## Glyph standards

1. **Entity glyphs must identify the entity consistently.**
   - In inventory spaces, panel headers, contents/initiative rows, logs, and future browser panels, an entity's glyph should represent that entity.
   - Do not replace an entity glyph with an unrelated state marker such as `<`, `>`, `^`, or `v` to represent facing.

2. **A glyph should appear the same way everywhere it represents the same entity.**
   - If a glyph is animated, recolored, decorated, or otherwise styled as an entity identity treatment, that treatment should apply consistently everywhere the glyph is shown.
   - Local emphasis may use surrounding UI treatment, such as borders, background, adjacent icons, decorators, labels, or overlays, but should not silently change the identity glyph in only one location.

3. **State indicators should be adjacent or layered, not identity replacements.**
   - Facing, target, controlled/focused state, valid actions, selection, and warnings should be shown with consistent markers, background/highlight treatments, arrows beside a glyph, labels, or separate columns.
   - If a future renderer supports true layered/decorator rendering, the same decoration policy should apply across panel headers, grids, contents rows, and logs.

## Contents, initiative, and local logs

1. **Contents and local log should converge into one local activity list.**
   - A panel should avoid separate disconnected “Contents” and “Local log” sections when the useful mental model is “what did each local entity do?”
   - Preferred shape: a ranked local list by turn/initiative order. Each row/group contains turn order, glyph, entity name, role/classification, and recent local outcomes/traces for that entity.

2. **Local activity should prioritize the previous round for local entities.**
   - Show successes and failures for entities local to the panel's inventory/space.
   - Include fuller trace/failure context locally than the global log does.
   - Keep local context anchored to the panel's entity, contained entities, inventory plane, and affected entities.

3. **Example local activity shape:**

```text
| 0 | s : Slime1 |
  ├─Slime tried moving East but was blocked
  └─Slime picked up rock
| 1 | @ : Player |
  └─Player moved West
```

4. **The global log should be concise.**
   - The global log should show successful outcomes for all simulated entities, not only the controlled/player entity.
   - Failures and deeper traces are better surfaced in local activity rows, expandable detail, or debug drilldown.
   - Until autonomous turn outcomes are projected by shared services, frontends should label limited logs honestly, such as “controlled-command log,” rather than implying complete global simulation history.

## Action highlighting and selection standards

1. **Valid action highlighting is valuable but should not imply authoritative resolution.**
   - Frontend highlights are affordance hints.
   - Shared command execution remains authoritative.

2. **Selection should favor valid choices.**
   - When choosing a target/cell for an action, cycling through valid choices is preferred over moving an unrestricted cursor across mostly invalid cells.
   - Arbitrary-cell selection may remain available for debug/explanation modes, but the default player-facing interaction should minimize invalid cursor work.

3. **Invalid options should be explainable.**
   - If invalid/blocked options are shown, their reason should be discoverable in prompt text, local detail, or future hover/focus UI.

4. **Cursor/focus state should be animated or otherwise distinct.**
    - A selection cursor should be visually distinct from static valid-target highlights.
    - Desired follow-up: blink the active selection cursor gold.
    - Desired follow-up: when no action target is being selected and a move/action is available, blink or otherwise emphasize the currently controlled entity without changing its identity glyph.

5. **Known dead-end prompt modes should be avoided when shared affordance data is sufficient.**
   - If the current direct-control affordance query reports no valid targets for an action, the frontend should prefer explaining that immediately rather than entering an empty selection mode.
   - This is a presentation/input shortcut only. Shared command execution remains authoritative if a command is submitted through another path or the world changes.

6. **Inspection selection can use valid-target style affordances, but it is navigation rather than action legality.**
   - Inspect mode may highlight and cycle visible inspectable entities/cells to reduce cursor work.
   - Inspection targeting should be derived from visible projected/runtime facts and should not be confused with Core action-target legality.

## Editor and Simulation mode model

SadConsole should grow toward two top-level frontend modes that share visual vocabulary but keep authored content and runtime simulation concepts distinct:

- **Editor mode**: opens and edits authored content documents, templates, scenarios, action plans, carried inventory layouts, validation diagnostics, YAML/diff previews, and scenario previews through shared content/editor services.
- **Simulation mode**: plays, inspects, and debugs a materialized runtime scenario session through shared session/action/affordance/log/panel services.

### Shared navigation model

The top-level shell should allow the user to choose either:

1. a content file to edit; or
2. a scenario from a manifest/catalog to play.

Scenario launch should still preserve editor context where possible. A scenario selected from the main menu has a backing content file; launching it should be equivalent to opening that content in an editor context, starting a simulation session for the selected scenario, and returning to that editor context when simulation ends.

### Preferred Editor -> Simulation flow

The preferred near-term model is:

```text
Main Menu
 ├─ Open Content File -> Editor Mode
 │    ├─ Scenario Preview Panel (turn 0 materialization)
 │    └─ Launch Simulation -> Simulation Mode -> Return to Editor
 └─ Play Scenario from Manifest
      └─ Open backing content file in Editor context
           └─ Launch Simulation -> Simulation Mode -> Return to Editor
```

This model prioritizes:

- a clear materialization boundary: Simulation is a runtime session produced from authored content;
- a clear mutation boundary: Editor mode mutates authored content, Simulation mode submits runtime actions;
- a useful feedback loop: edit content, preview turn 0, launch simulation, return to the same editor context.

### Scenario preview in Editor mode

Editor mode should support a scenario preview surface that materializes the selected scenario at turn 0 without entering full Simulation mode.

The preview should:

- use the same entity-panel/grid/glyph standards as Simulation where practical;
- show materialization diagnostics and validation problems in context;
- make it clear that preview state is derived from authored content and is not itself the authored source;
- eventually support a direct “Launch Simulation” action from the preview.

Refresh policy is a design choice for later implementation. Manual refresh is safest first; auto-refresh can be considered once performance and dirty-document semantics are understood.

### Simulation -> Editor source jumps

Simulation mode should eventually support source navigation for runtime facts that can be traced back to authored content.

Preferred examples:

- runtime entity -> authored entity template;
- runtime entity default plan -> authored action plan;
- simulation/materialization diagnostic -> relevant scenario/template/action-plan editor panel;
- log or local activity row -> relevant runtime entity, then optionally its authored source.

This is not runtime-state editing. It is cross-mode navigation from materialized state back to authored source.

### Authored versus runtime identity

Editor and Simulation may share entity-panel presentation, but each panel must identify whether it represents authored content or runtime state.

| Concept | Simulation mode | Editor mode |
|---|---|---|
| Entity panel | Runtime entity | Authored entity template or authored carried entity definition |
| Location | Runtime plane coordinate | Authored carried layout/default placement |
| Inventory grid | Runtime contents | Authored carried entities/templates |
| Action state | Runtime facing/target | Authored defaults/targeting rules |
| Logs/details | Runtime outcomes/traces | Validation, preview, diff, authoring diagnostics |
| Actions | Controlled commands/runtime debug actions | Content mutations through shared editor/content services |

The frontend should not blur these categories. Editing authored content should happen through shared editor/content services. Simulation actions should happen through runtime/session/action services.

### Deferred high-risk debugger ideas

The following ideas are intentionally deferred and should be reassessed only after the Editor -> Preview -> Simulation loop is established:

1. **Live content editing while Simulation continues running.**
   - Risk: weakens the materialization boundary and requires explicit Core/Content hot-reload semantics.
   - Safer first step: edit content while simulation is paused or in a side panel, then explicitly re-materialize/restart simulation.
   - Possible future pathway: shared debug-only actions or primitives that apply controlled changes with traceable outcomes.

2. **Runtime debug mutation inside Simulation.**
   - Risk: runtime state mutation is not authored content editing and should not be presented as ordinary Editor mode.
   - Useful future shape: a clearly labeled debug mutation mode or debug-only Action Step/primitive set for moving entities, changing action state, forcing containment, or setting targets during a paused simulation.
   - Any such primitive/debug action should be Core-aware, traceable, and separated from normal content-authoring semantics.

## Known standards contradicted by the current first pass

The Stage 7A SadConsole shell intentionally explored useful ideas quickly. The following current behaviors are exploratory and should be revised before treating the UI as canonical:

- The Stage 7A shell initially changed the player glyph to direction arrows for facing. That violated the glyph identity standard and was corrected in Stage 7B by preserving entity glyphs and moving facing/target into text indicators. Future visual facing treatments should remain adjacent/layered/decorator-style rather than identity replacements.
- Local logs currently show controlled-command outcomes and can appear broadly wherever the player/current plane is anchored. They are not yet true local per-entity activity rows.
- The global log currently records controlled-command outcomes only. It is not yet a complete successful-outcome log for all simulated entities.
- Selection currently allows cursoring across arbitrary cells in several modes. Future action prompts should prefer cycling/constraining to valid choices.

## Backlog candidates promoted by these standards

Use these as planning seeds for Stage 7 follow-up slices:

1. Replace player-facing glyph substitution with consistent facing/target decorators.
2. Consolidate contents and local logs into initiative-ranked local activity rows.
3. Add shared/autonomous turn outcome projection if Core does not yet expose enough structured data for all-entity global/local logs.
4. Restrict or cycle action selection through valid affordance choices by default.
5. Add blinking/animated cursor and controlled-entity emphasis without changing identity glyphs.
6. Define a reusable glyph/decorator style policy before adding animation.
7. Plan the Editor -> scenario preview -> Simulation loop before implementing broad Editor mode UI.
8. Backlog live hot-editing and runtime debug mutation as deferred debugger capabilities, potentially through debug-only action primitives.
9. Add valid inspection target highlighting/cycling as a Stage 7 navigation polish item.
10. Suppress known-dead-end direct-control prompt modes when current shared affordance data reports no valid targets.
