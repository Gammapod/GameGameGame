# Entity Panel UX Spec

Status: Source of truth for the entity-panel, breadcrumb, and turn-log UX model.

Read when:

- designing Console, SadConsole, or future frontend inspection surfaces;
- defining frontend-neutral entity panel projection data;
- deciding which panel state is frontend-owned versus shared service data.

Related documents:

- `docs/Source of Truth/Frontend-UX-Invariants.md` records broad frontend UX constraints and layer boundaries.
- `docs/Source of Truth/Frontend-UX-Standards.md` records UI-bible presentation standards for entity-neutral display, glyph consistency, local activity, and action highlighting.
- `docs/Plans/SadConsole-Frontend-Roadmap.md` records staged implementation work.
- `docs/Archived/SadConsole-Spike-Findings.md` records prototype evidence.
- `docs/Source of Truth/Engine-Editor-Capabilities.md` records implemented support tiers.

## UX model

The canonical debug/browser frontend is composed of **entity panels**. A panel presents one entity and, when the entity owns an inventory/space plane, the contents of that plane. The player's current playspace is also an entity panel; it should not require a separate special map concept for debug/browser workflows.

Panels are connected by containment/breadcrumb paths. A frontend may show one panel, two panels, or a chain of panels, but the underlying data should remain entity-centric.

## Panel content

An entity panel should eventually be buildable from one frontend-neutral projection containing:

- identity: entity ID, name, glyph, color, and optional presentation metadata;
- location: current plane/coordinate and containment breadcrumb path;
- physical metadata: bulk, aperture, inventory dimensions, and load/contents summary;
- action state: facing, target slots, actor/inert/player classification where available;
- behavior summary: default/action-plan or behavior-chain preview where available;
- inventory/space grid: dimensions, cells, occupants, occupant presentation, and coordinates;
- contents list: contained entities in deterministic/initiative order where available;
- previous action or local log snippet for relevant contained entities;
- diagnostics or capability gaps when the panel depends on incomplete data.

The shared projection should provide facts. The frontend decides layout, line wrapping, filtering, collapse state, visual emphasis, and exact wording.

## Breadcrumbs

Breadcrumbs are structural containment paths, not gameplay paths. They should use Core containment-path queries so traversal is cycle-safe and can report incomplete statuses.

Expected breadcrumb uses:

- show the player's current containment path;
- show the inspected entity's containment path;
- show root-relative or shared-root paths when comparing two entities;
- support future interactive navigation by selecting ancestors or visible panels.

Current frontend work should keep breadcrumb navigation read-only unless explicitly promoted. Interactive breadcrumbs, collapsible panel chains, and pinned panels are future rich-frontend work.

## Logs

Entity panels need both global and local temporal context:

- **Global chronological log:** full action/turn history for the current session or recent turns.
- **Local panel log:** entries relevant to the panel entity, its inventory/space, visible occupants, or affected anchored entities.
- **Previous-action summary:** compact row-level context for visible contents, especially from local turn-order reports.

Logs should be derived from structured action outcomes or shared turn reports. Frontends should not parse arbitrary trace display strings to discover actor, target, result, or failure facts.

Sentence rendering can be frontend-owned, but the shared shape should support compact patterns such as:

- success: `{entity} {verb}ed {target/recipient}`;
- failure: `{entity} tried to {verb} {target}, but {failure reason}`.

## Input and focus principles

The prototype findings support keyboard-first play with optional mouse convenience. The durable principles are:

- normal player movement should remain easy to perform without understanding every visible panel;
- inspect mode, prompt mode, and play mode must be visibly distinct;
- action prompts should constrain or highlight valid choices using shared target/affordance data once available;
- mouse controls may click entities, select action targets, expand/collapse panels, and support future editor UX, but mouse should be a convenience layer over a coherent keyboard model;
- if play focus and inspection focus diverge, the UI must make both states explicit enough to avoid accidental action targeting.

Keyboard interaction options remain design choices for frontend-owner evaluation, not Core semantics. Candidate patterns from the spike include player-centric default focus with inspect mode, action-first prompts, numbered quick-focus panels, and split play/inspect focus.

## Frontend-owned panel state

Frontend-owned state may include:

- visible panel list/order;
- focused panel and focused cell;
- selected/hovered entity or cell;
- collapsed/expanded panels or sections;
- scroll offsets, clipping, zoom/scale, and minimap choices;
- active prompt/action mode;
- mouse hit-test rectangles and layout geometry;
- visual styling and animation state.

This state must remain presentation/input state. It should not duplicate the world, independently decide action legality, or persist gameplay facts that should live in Core/Content.

## Current source data

Existing shared data that can seed panel projection work:

- `EntityInspectionPanel` for identity, properties, presentation, and inventory grid data;
- `EntityContainmentPathService` for cycle-safe breadcrumbs and path statuses;
- `LocalTurnOrderReport` for local actor/player/inert ordering and previous-action summaries;
- `WorldState.LastTurnReport` and traces for recent turn/action facts;
- scenario materialization outputs for world, registry/presentation lookup, action plans, player entity, and active plane/container.

## Known open UX questions

- How many visible panels remain readable before collapse, pinning, scrolling, or alternate summaries are needed?
- How should large grids be clipped, scrolled, scaled, or summarized?
- How should facing and target indicators appear in-grid without clutter?
- How much local log context is enough for a panel without overwhelming it?
- Which prompt/focus model is most comfortable for keyboard-only play?
- Whether SadConsole hit-testing, mouse hover/click, animation, and editor-like widgets are sufficient for the long-term frontend.
