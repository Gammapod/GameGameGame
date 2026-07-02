# SadConsole Spike Findings

Status: Findings document for ending the SadConsole prototype spike. Superseded for next-work selection by `docs/Plans/SadConsole-Frontend-Roadmap.md`; retain this document as evidence/reference for the roadmap.

Read when:

- planning the next shareable/frontend architecture pass;
- deciding what belongs in Core, Content, Headless, Editor, or a frontend application;
- comparing SadConsole with Godot, Unity, or another frontend candidate;
- extracting reusable services from the SadConsole prototype experience.

Related source of truth:

- `docs/Source of Truth/Frontend-UX-Invariants.md` records broad frontend UX invariants, including the rule that frontends must not invent frontend-only simulation semantics.
- `docs/Source of Truth/Entity-Panel-UX-Spec.md` records the entity-panel, breadcrumb, and turn-log UX model.
- `docs/Archived/SadConsole-Entity-Panel-Prototype.md` records the prototype implementation turns.
- `docs/Archived/SadConsole-Prototype-Assessment.md` records the immediate UX assessment after keyboard-first action controls.

## Executive summary

The SadConsole prototype validated the entity-panel direction, but the spike should end before the prototype becomes an accidental second gameplay/application layer.

The prototype proved that a grid/panel frontend can sit over existing scenario materialization, inspection, containment paths, Core actions, turn advancement, and turn reports without new simulation semantics. It also exposed the next architectural requirement: future frontend work needs cleaner shared session, command, target, and log-projection services so Console, SadConsole, future shareable frontends, and eventual integrated editor surfaces consume the same capabilities.

The SadConsole project should be treated as evidence and reference code, not as the owner of durable gameplay rules.

## What the prototype proved

- SadConsole can host a standalone frontend over `GameGameGame.Core`, `GameGameGame.Content`, and shared inspection/materialization services.
- Existing `EntityInspectionPanel` data maps naturally to entity panels with status fields, glyph/color presentation, inventory/space grids, facing, target, and containment paths.
- The player's current playspace works as an entity panel instead of requiring a separate map concept.
- Containment/breadcrumb paths can be rendered as a left-to-right panel chain.
- Expanded/collapsed panel states make longer containment paths more readable, at least for prototype-size layouts.
- Keyboard focus and per-panel cursors are enough to inspect entities inside nested inventory/space panels.
- Existing Core action intents can drive movement, pickup, drop, enter, and exit from a richer frontend.
- `LastTurnReport` and local turn-order data are usable seeds for a universal turn-log display.

## What remains unproven

- Whether SadConsole is the right long-term frontend engine for mouse-heavy interaction, animation, polished status windows, or editor-like widgets.
- Whether the current SadConsole/MonoGame DesktopGL stack can support browser/HTML5 distribution for itch.io-style sharing.
- Whether panel chains remain readable with large grids, long names, dense spaces, and many containment levels.
- Whether mouse hit-testing and hover/click affordances are comfortable in SadConsole.
- Whether per-panel local logs can remain compact without hiding necessary context.
- Whether action prompts can become player-friendly without adding too much custom UI state.
- Whether integrated editor workflows would be pleasant in SadConsole compared with a richer UI/game framework.

## Architectural concern discovered by the spike

The prototype did not seriously violate engine/frontend boundaries, but it began to accumulate a frontend-local “game session controller.” That pressure is valuable evidence.

Current prototype responsibilities that should not harden inside a SadConsole-specific project:

- deciding how a controlled actor action is evaluated and submitted to the turn service;
- deciding which failed actions consume or do not consume a turn;
- finding valid pickup, drop, enter, exit, and movement targets;
- creating player-facing success/failure summaries from action traces;
- projecting universal turn reports into local panel logs;
- loading a playable scenario session and classifying launch diagnostics;
- refreshing current-container and inspected-panel state after every action.

Some of this can remain frontend orchestration in the short term, but durable gameplay/application semantics should move into frontend-agnostic services before another frontend spike or shareable build becomes production-ish.

## Recommended layer split

### Core-owned services

Core should own simulation truth and reusable command semantics.

Candidate services or service responsibilities:

1. **Controlled actor command service**
   - Input: world/session state, actor ID, action intent or command request.
   - Responsibility: evaluate the action, run it through turn advancement when appropriate, record traces/reports, refresh Core-owned derived action state, and return a structured command result.
   - Frontend benefit: Console, SadConsole, tests, and future frontends can all submit player actions through the same path.

2. **Action target query service**
   - Input: world state, actor ID, action kind, optional source context.
   - Responsibility: enumerate or evaluate valid movement directions, pickup targets, carried items, drop destinations, enter targets, and exit directions.
   - Frontend benefit: frontends can highlight valid cells and constrain prompts without duplicating rule checks.

3. **Structured action/turn outcome model**
   - Input/output around existing traces and `LastTurnReport`.
   - Responsibility: expose actor, target, affected entities, containing space, success/failure, failure reason, turn consumption, and report anchors in a stable shape.
   - Frontend benefit: logs, panel summaries, and editor previews can link back to inspectable entities without parsing display strings.

Core should not own rendering, key bindings, panel layout, mouse geometry, or visual prompt wording.

### Content/Headless-owned services

Content and Headless should own launch/session and report-adapter concerns that are not SadConsole-specific.

Candidate responsibilities:

1. **Playable scenario session creation**
   - Reuse the existing scenario materializer and catalog/manifest concepts.
   - Return a frontend-neutral session object or launch result with world, registry, action plans, player entity, diagnostics, and capability gaps.
   - Preserve direct launch for developer testing while enabling scenario menus to avoid duplicating content discovery rules.

2. **Diagnostics classification for frontend launch**
   - Distinguish blocking materialization failures from non-blocking validation diagnostics and capability gaps.
   - Let frontends decide how much to show, but keep classification shared.

3. **Report/log projection helpers**
   - Build on turn reports, scenario recorder outputs, local turn order, and traces.
   - Provide frontend-neutral projections for full chronological logs and local context summaries.

Headless should remain frontend-agnostic: no SadConsole cell geometry, key concepts, or visual-only state.

### Editor/API-owned services

Future integrated editor work should consume existing editor/content APIs and typed operations.

Candidate responsibilities:

- template/scenario editing operations;
- validation and diagnostics in context;
- scenario launch/replay hooks for preview;
- typed changes to spaces, inventories, behavior chains, and authored action plans.

The frontend may present editing through entity panels, but it must not duplicate YAML manipulation or invent editor-only semantics that Core/Content cannot consume.

### Frontend-owned responsibilities

The frontend should own presentation and input mechanics.

SadConsole, a future Godot frontend, or another UI should own:

- rendering panels, cells, breadcrumbs, logs, borders, colors, animation, and visual themes;
- panel layout, collapse/expand presentation, scrolling, clipping, and screen-space geometry;
- keyboard/mouse/controller bindings;
- focus, cursor, selection, and transient prompt UI state;
- menu presentation and packaging/distribution details;
- player-facing wording, filtering, and visual emphasis, as long as it is derived from shared data.

Frontend state may include selected panel, visible panels, collapsed panels, cursor location, hover state, and current prompt. It should not include independent simulation state or separate event semantics.

## Specific extraction opportunities

### 1. Controlled actor command execution

The prototype currently wraps player actions with local methods for movement, pickup, drop, enter, and exit. Those methods evaluate actions, run turns, format messages, and refresh panels.

Future plan:

- introduce a shared command result shape before making a shareable frontend production-like;
- ensure failed-action behavior is explicit and tested;
- expose success/failure and trace details without requiring each frontend to format Core traces from scratch.

### 2. Valid target discovery

The keyboard prompt pass showed that player-friendly action controls need valid-target highlighting and skipping.

Future plan:

- promote target discovery into Core rather than teaching each frontend pickup/drop/enter/exit legality;
- let frontends render valid/invalid states and explain blocked choices;
- keep action evaluation authoritative even when target hints exist.

### 3. Universal and local turn-log projections

Panel-specific logs remain a needed next frontend capability, but they should be projections of the same universal trace/report data.

Future plan:

- build a shared turn-log projection model with inspectable anchors;
- support both full chronological logs and local panel-context summaries;
- avoid creating SadConsole-only action events to make logs look better.

### 4. Scenario/session launch reuse

The prototype direct-launch path was useful, but a shareable frontend needs a menu backed by existing catalog/manifest concepts.

Future plan:

- reuse or extract the Console catalog/menu content-discovery path;
- keep command-line direct launch as a developer path;
- classify diagnostics consistently across Console, SadConsole, and future frontends.

### 5. Entity-panel view adapters

The prototype's frontend-owned view model was a useful step: rendering no longer had to directly query every service. However, some projections may be useful beyond SadConsole.

Future plan:

- keep SadConsole-specific drawing and layout in the frontend;
- consider shared, frontend-neutral panel/log projection models only when a second frontend or editor preview needs them;
- keep panel UI state such as collapsed/focused/hovered frontend-owned unless persistence or user preferences promote it.

## Frontend plan implications

Before choosing a long-term frontend engine or trying another shareable frontend spike, plan around these questions:

1. What minimal shared controlled-action API does a frontend need to play a scenario without duplicating gameplay rules?
2. What target-query API is needed for valid action prompts and mouse highlighting?
3. What structured turn-log/report data is needed for global logs, per-panel local logs, and inspectable log entries?
4. What scenario catalog/session launch API should every frontend use?
5. Which entity-panel projections are stable enough to share, and which are still frontend experiment state?
6. What distribution target matters most for external feedback: browser, downloadable desktop, or both?
7. How much future editor UX is required before selecting a frontend engine?

## Recommended next planning move

Do not continue adding SadConsole features as the default next step from this findings document alone. Instead:

1. Record this spike as completed research.
2. Create a frontend architecture plan that first extracts or specifies the shared session/action/target/log contracts above.
3. Follow `docs/Plans/SadConsole-Frontend-Roadmap.md`: pave shared services first, then resume SadConsole as the canonical debug/browser direction while deferring final engine comparison.
4. Treat browser delivery and future editor widgets as first-class engine-selection criteria, not late packaging details.

## Decision status

SadConsole remains a useful prototype and reference implementation. Later planning selected it as the preferred canonical debug/browser direction, while final frontend-engine choice remains deferred.

Current decision: **end the spike, carry findings forward, and use the SadConsole frontend roadmap for next work**.

Future decision should happen only after shared frontend-facing service contracts are planned and the next frontend candidate is assessed against play, inspection, logs, scenario selection, packaging, mouse interaction, and future editor needs.
