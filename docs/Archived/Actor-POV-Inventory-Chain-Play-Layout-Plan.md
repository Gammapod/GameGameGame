---
id: plan.actor-pov-inventory-chain-play-layout
title: Actor POV Inventory-Chain Play Layout Plan
kind: plan
subkind: frontend-play-layout-plan
status: archived
owners: [frontend-owner]
audience: [frontend-owner, core-owner]
lane: frontend-ux
truth_rank: 55
truth_domains: [frontend-presentation]
read_when:
  - planning the componentized consumer Play mode containment/inventory-chain layout
  - deciding how actor POV, parent locations, peer inspection, and actor inventory inspection should compose on screen
  - replacing the actor-POV layout spike with clean architecture in main
related:
  - plan.actor-pov-play-layout-foundation-sprint
  - source.frontend-ux-invariants
  - source.frontend-ux-standards
  - source.frontend-ux-decisions
  - source.entity-panel-ux-spec
  - source.sadconsole-ui-specification
  - plan.sadconsole-frontend-roadmap
---

# Actor POV Inventory-Chain Play Layout Plan

Status: Active plan and findings record. This document records the user-facing semantics proven by the actor-POV inventory-chain spike and the recommended cleaner architecture for rebuilding the UX in main.

Completed focused implementation sprint: `docs/Archived/Actor-POV-Play-Layout-Foundation-Sprint-Plan.md` records the first componentized layout sprint over the MVP `ActorPovPlayProjection` layer.

## Decision

Do not merge the spike implementation as the durable Play-mode architecture. Use the spike as evidence, record its semantics here, and rebuild the UX in main through componentized, test-backed slices.

The spike successfully proved the visual grammar:

```text
┌─────────────────────────────────────────────────────────────┐
│ parent/location chain │ current actor POV │ world inspection │
│        left           │ centered square   │ chain right      │
├─────────────────────────────────────────────────────────────┤
│ controlled actor inventory chain -> inspected carried item   │
└─────────────────────────────────────────────────────────────┘
```

## User-facing semantics proven by the spike

1. **Actor POV is the screen anchor.**
   - Each draw centers the upper world area on the current actor's point-of-view current place.
   - The current place is visually primary and occupies a centered square region.

2. **Parent locations extend left.**
   - When the actor enters a nested space, breadcrumb parent locations render in the left region at normal scale.
   - The scenario root or nearest visible ancestor appears left of the current POV region.
   - Connector lines run from the parent cell containing the child to the next visible child/current-place node.

3. **World/peer inspection extends right.**
   - A selected/inspected peer space renders to the right of the current POV region.
   - Connector lines run from the inspected entity's cell in the current/parent place to the inspected space.
   - The spike used a provisional automatic policy: first inspectable non-actor local entity with an inventory grid.

4. **Controlled actor inventory has a dedicated bottom chain.**
   - The bottom region is reserved for the controlled actor's own inventory.
   - An inspected carried item/inventory can chain to the right from the actor inventory.
   - The spike used a provisional automatic policy: first carried entity with an inventory grid.

5. **Region chrome helps orientation.**
   - Visible separators between parent/current/inspection and inventory regions made the experimental layout easier to parse.
   - Durable chrome should remain a component/theme concern rather than hardcoded drawing in the screen shell.

6. **Partial visibility is acceptable if explicit.**
   - Deep chains and large spaces cannot all fit at once.
   - The layout can still be understandable if overflow, clipping, panning, collapsed nodes, or hidden-count markers are explicit.

## Spike compromises not to preserve

- The spike concentrated layout, projection choice, viewport fitting, connector combination, and node orchestration in `ConsumerPlayModeScreen`.
- Parent chain, world inspection chain, and actor inventory chain were combined through one broad presentation method.
- Inspection selection was automatic and hardcoded rather than user-controlled.
- Current-location zoom was approximated with inventory-space cell metrics, not a clear render-profile/component scaling model.
- Viewport fitting was a temporary auto-crop around the controlled actor, not durable per-panel panning state.
- Region chrome was rendered directly by the console rather than as a reusable component/layer.
- The spike used enough direct geometry to prove the UX, but it should not become the long-term layout engine.

## Target architecture

Rebuild as componentized Play-mode presentation over shared Core/Content facts.

Recommended first shape:

```text
ActorPovPlayScreenModel
  ActorPovPlayLayoutResolver
  ActorPovPlayRegionChromeComponent
  ParentLocationChainComponent
  CurrentPovLocationComponent
  WorldInspectionChainComponent
  ActorInventoryChainComponent
  ConnectorLayerComponent / ConnectorOverlayModel
```

Recommended reusable presentation models:

- `ActorPovPlayLayout`: pure top-level region resolver over drawable bounds.
- `InventoryChainNodeViewModel`: projected entity/space node, role, scale profile, viewport, bounds, focus/selection state.
- `InventoryChainConnectorViewModel`: connector endpoints derived from rendered cell geometry.
- `InventorySpaceViewportState`: frontend-owned pan/crop state per visible inventory-space node.
- `InspectionSelectionState`: frontend-owned selected peer entity and selected carried entity, backed by shared inspectable facts when needed.
- `InventorySpaceRenderProfile`: named display profiles such as compact chain node, current POV large node, actor inventory node, debug/labeled node.

## Ownership boundaries

Frontend owns:

- region layout, component bounds, scaling profiles, panning, clipping, collapse/expand state, visible chain selection, focus, hover, and connector rendering;
- user-facing wording, labels, hidden-count markers, and region chrome;
- keyboard/mouse/controller inspection navigation as presentation/input state.

Shared Core/Content/Headless services should own or expose:

- containment/breadcrumb paths and cycle-safe diagnostics;
- actor point-of-view current place and current-place diagnostics;
- entity panel/inventory projections;
- future frontend-neutral inspectable candidate facts if inspection selection must respect POV, visibility, knowledge, or other shared rules.

The frontend must not invent containment, action legality, materialization, turn advancement, or durable visibility semantics.

## Required follow-up capabilities

### 1. First-class region components

Treat each major area as a component with its own bounds, render profile, local empty text, focus treatment, and diagnostics:

- parent/location chain;
- current actor POV location;
- world/peer inspection chain;
- controlled actor inventory chain;
- chrome/separator layer.

This enables separate scaling, text info, labels, panning, and future mouse hit-testing.

### 2. Per-node viewport and panning

Each visible inventory-space node needs frontend-owned viewport state. The current POV node should usually track the controlled actor, but the player/debugger should be able to pan when a region cannot show the full space.

The panning model should be local to presentation and should not mutate runtime location or containment facts.

### 3. Explicit inspection selection

Replace the spike's automatic "first inspectable" policy with explicit selection:

- one active world/peer inspection outside the controlled actor inventory;
- one active carried-item inspection inside the controlled actor inventory.

Frontend can own current selected IDs and focus cycling, but if valid inspectable candidates depend on more than visible projected facts, coordinate with Core/Content for a frontend-neutral query/projection.

### 4. Connector layer and hit-test facts

Connector endpoints should derive from rendered inventory cell geometry after layout and viewport resolution. The connector layer should be separate enough to draw below overlays and above/between node surfaces consistently.

Future mouse support should hit-test nodes and cells, not rely on connector lines as primary targets unless a later explicit connector hit-test model is designed.

### 5. Overflow/collapse policy

Deep breadcrumb chains, large current spaces, and long inspected chains need visible overflow handling:

- clipped/hidden counts;
- collapsed ancestor/descendant cards;
- panning controls;
- debug diagnostics for omitted nodes;
- deterministic priority rules for which nodes remain visible.

## Suggested reconstruction slices

1. **Pure layout resolver and chrome component.**
   - Recreate the actor-POV top-level regions in main with tests.
   - Render non-semantic chrome as a component/layer.

2. **Current POV location component.**
   - Render only the actor POV current place in the centered square using a named render profile and viewport state.

3. **Controlled actor inventory component.**
   - Render the bottom inventory root with its own render profile and viewport state.

4. **Parent location chain component.**
   - Consume the current-place breadcrumb and render visible ancestors left of the current POV node.
   - Add connector endpoints derived from parent cell geometry.

5. **World inspection chain component.**
   - Start with explicit frontend selection over currently visible inspectable peers.
   - Render selected peer chain to the right.

6. **Actor inventory inspection chain component.**
   - Add explicit carried-item inspection selection and render selected carried inventory chain to the right of actor inventory.

7. **Panning and overflow controls.**
   - Add per-node viewport movement, hidden-count markers, and collapse/omission diagnostics.

8. **Mouse/focus polish.**
   - Add hit-test maps and click-to-focus/inspect convenience once keyboard selection is coherent.

## Promotion criteria

Before treating this UX as durable, main should have:

- pure tests for top-level region resolution and overflow edge cases;
- component tests for each chain region;
- tests proving current POV, parent chain, world inspection, and actor inventory inspection can coexist without crossing drawable bounds;
- tests proving inspection selection is frontend state over shared facts, not action legality;
- component-gallery examples for accepted region/chrome/connector/inventory-chain patterns;
- updated Frontend UX Standards/Decisions if the actor-POV anchored layout becomes canonical rather than provisional.

## Open questions

- Should actor inventory be pinned lower-left, lower-center, or lower-right in the final consumer layout?
- Should the current POV node show full large cells, a camera viewport, or a hybrid with mini-map/summary for very large spaces?
- What is the default keyboard model for switching focus between current POV, world inspection, and actor inventory inspection?
- Does inspection selection need a shared inspectable-candidate service, or are projected visible facts enough for the first durable slice?
- How should collapsed ancestors and off-screen inspected descendants be represented without cluttering the main play surface?
