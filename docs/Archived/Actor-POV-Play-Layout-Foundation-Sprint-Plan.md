---
id: plan.actor-pov-play-layout-foundation-sprint
title: Actor POV Play Layout Foundation Sprint Plan
kind: plan
subkind: frontend-sprint-plan
status: completed
owners: [frontend-owner]
audience: [frontend-owner, core-owner]
lane: frontend-ux
truth_rank: 60
truth_domains: [frontend-presentation, frontend-planning]
read_when:
  - reviewing the completed componentized Actor POV consumer Play layout foundation sprint
  - investigating why Actor POV Play mode has separate region/layout/screen-model seams
  - planning follow-up parent inventory nodes connectors per-node viewport or chrome work
related:
  - plan.actor-pov-inventory-chain-play-layout
  - plan.sadconsole-ui-specification
  - source.frontend-ux-invariants
  - source.frontend-ux-standards
  - source.frontend-ux-decisions
  - source.entity-panel-ux-spec
---

# Actor POV Play Layout Foundation Sprint Plan

Status: Completed focused frontend sprint plan. This sprint began the durable rebuild described by `docs/Plans/Actor-POV-Inventory-Chain-Play-Layout-Plan.md` by creating a componentized Actor POV layout foundation over the MVP `ActorPovPlayProjection` layer.

## Goal

Replace the current consumer Play layout direction with a componentized Actor POV layout foundation that:

1. reserves stable screen regions for the parent chain, current actor POV, world inspection, controlled actor inventory, carried-item inspection, chrome, connectors, and diagnostics;
2. consumes `ActorPovPlayProjection` instead of rebuilding containment or inspection facts in SadConsole;
3. renders current POV and controlled actor inventory as independent components;
4. scaffolds parent/world/carried chain regions without overbuilding final interaction polish;
5. stays presentation-only and test-backed.

## Starting dependency

Core/content now provides an MVP projection layer in commit `c26df9484c3de574c867d0c9d0156140266681a9` (`MVP POV projection layer`). The first sprint should consume this rather than adding frontend-owned containment traversal.

Available projection shape:

- `ActorPovPlayProjection.ControlledActor`
- `ActorPovPlayProjection.CurrentPlace`
- `ActorPovPlayProjection.ParentChain`
- `ActorPovPlayProjection.WorldInspectionCandidates`
- `ActorPovPlayProjection.ActorInventory`
- `ActorPovPlayProjection.CarriedInspectionCandidates`
- `ActorPovPlayProjection.Diagnostics`

The projection is sufficient for the first layout sprint. Coordinate with `core-owner` only when a needed fact crosses out of frontend-owned presentation state.

## Non-goals

Do not try to finish these in this sprint:

- full recursive containment-tree browsing;
- final region chrome styling;
- mouse interaction;
- final panning and overflow UX;
- final inspection-selection controls;
- whole-scenario tree layout;
- semantic inspectability, visibility, knowledge, action-legality, materialization, or containment rules.

## Ownership boundaries

Frontend owns:

- region layout and layering;
- component bounds and chrome;
- selected visible region/candidate state;
- viewport/crop state;
- focus and prompt presentation state;
- empty text, labels, hidden counts, and diagnostics presentation.

Core/Content/Headless owns or exposes:

- current point-of-view place facts;
- containment breadcrumbs and diagnostics;
- entity panel/inventory projections;
- future frontend-neutral inspectable-candidate rules if inspectability becomes semantic.

The sprint must not introduce SadConsole-owned gameplay semantics, containment traversal, action legality, materialization rules, provenance rules, or log facts.

## Sprint slices

### 1. Pure Actor POV layout resolver

Add a pure tested resolver, with names similar to:

```text
ActorPovPlayLayout
ActorPovPlayLayoutResolver
ActorPovPlayRegion
ActorPovPlayRegionId
```

Initial named regions:

```text
0.actor-pov-root
0.actor-pov.parent-chain
0.actor-pov.current-place
0.actor-pov.world-inspection
0.actor-pov.actor-inventory
0.actor-pov.actor-inventory-inspection
0.actor-pov.chrome
0.actor-pov.connectors
0.actor-pov.diagnostics
```

Initial layout grammar:

```text
┌─────────────────────────────────────────────────────────────┐
│ parent chain │ current POV square │ world inspection chain  │
├─────────────────────────────────────────────────────────────┤
│ actor inventory │ actor carried-item inspection chain       │
└─────────────────────────────────────────────────────────────┘
```

Tests should prove:

- all concrete regions stay inside drawable bounds;
- the current POV region is centered in the upper world area;
- the bottom actor-inventory band is reserved;
- small screens return safe omitted/diagnostic regions instead of invalid bounds;
- chrome, connector, and diagnostic regions/layers are separate from content regions.

### 2. Projection-backed screen-model seam

Introduce an Actor POV Play screen-model layer, with names similar to:

```text
ActorPovPlayScreenModel
ActorPovPlayScreenModelBuilder
```

It should call `ActorPovPlayProjectionService.Project(...)` and expose only frontend presentation state, such as:

- selected world inspection candidate ID;
- selected carried inspection candidate ID;
- viewport state keyed by visible entity/region;
- selected/focused region;
- projection and layout diagnostic rows.

Initial candidate selection may default to the first projected candidate, but this must remain explicitly frontend-owned presentation state.

Tests should prove:

- the model includes controlled actor, current place, and actor inventory from the projection;
- point-of-view diagnostics pass through without frontend guessing;
- selected candidates are selected from projection candidates;
- missing current place produces an empty/diagnostic model instead of a crash.

### 3. Current POV location component

Render `projection.CurrentPlace` into the current-place region using existing inventory-space component patterns.

Scope:

- use an accepted inventory-space render profile;
- center or crop through frontend-owned viewport state;
- preserve entity glyph identity;
- avoid new action legality or visibility logic.

Tests should prove:

- the current-place component appears when the projection has a current place;
- honest empty text appears when current place is unavailable;
- the component stays inside its assigned region;
- viewport state does not mutate runtime facts.

### 4. Controlled actor inventory component

Render `projection.ActorInventory` into the bottom actor-inventory region.

Scope:

- keep it independent from the current-place component;
- use its own viewport/render profile;
- show carried-candidate count or placeholder text if the carried-item inspection chain is not rendered yet.

Tests should prove:

- actor inventory renders when the controlled actor has an inventory grid;
- missing actor inventory is handled cleanly;
- carried candidates are not treated as gameplay actions.

### 5. Parent chain scaffold

Consume `projection.ParentChain`.

Initial behavior:

- render compact parent nodes/cards left of current POV;
- include omitted/hidden marker if the chain does not fit;
- preserve child coordinate facts for later connector endpoints;
- do not over-polish connectors in this slice.

Projection fields to preserve:

- `ActorPovChainNodeProjection.Entity`
- `ActorPovChainNodeProjection.ChildEntityId`
- `ActorPovChainNodeProjection.ChildCoordinateInEntityInventory`

Tests should prove:

- parent chain order follows projection order;
- parent child coordinate is preserved for connector endpoint construction;
- overflow or too-small regions omit deterministically with visible diagnostics.

### 6. Inspection chain placeholders or first selected nodes

Consume:

- `projection.WorldInspectionCandidates`
- `projection.CarriedInspectionCandidates`

Preferred first behavior if time allows:

- render one selected world candidate in the right region;
- render one selected carried candidate in the lower-right region.

Acceptable fallback:

- render placeholders with candidate counts and selected IDs.

Tests should prove:

- world candidate selection comes from `WorldInspectionCandidates`;
- carried candidate selection comes from `CarriedInspectionCandidates`;
- no-candidate cases show honest empty text;
- selection remains frontend state over shared facts.

### 7. Chrome and diagnostics

Add a first reusable chrome/diagnostic treatment:

- separators;
- optional debug region labels;
- hidden/omitted counts;
- projection diagnostics;
- layout diagnostics.

Chrome should be component/theme-owned rather than hardcoded shell drawing where practical. If the treatment becomes a reusable pattern, update the component gallery and decision/source docs as appropriate.

## Core-owner coordination triggers

Coordinate with `core-owner` if any of these appear during implementation:

1. **Inspectable candidate semantics exceed projected visible facts.**
   - If inspectability depends on visibility, knowledge, aperture, POV policy, permissions, or other shared rules, request a frontend-neutral query/projection instead of hardcoding it in SadConsole.
2. **Connector facts are insufficient.**
   - The MVP parent chain exposes `ChildCoordinateInEntityInventory`, which is enough for first parent/current connectors. Shared-root branches, ambiguous containment, or cycle/incomplete cases should use shared facts rather than frontend ancestry guesses.
3. **Diagnostics need richer severity/classification.**
   - Pass through current `PointOfViewDiagnostic` facts for now. If user-facing severity/category is needed, coordinate on projection shape.
4. **Projection performance or batching becomes a real issue.**
   - The MVP projects several entity panels and is acceptable for the sprint. Defer batching unless tests or visible performance show a problem.

## Acceptance criteria

The sprint is complete when:

- consumer Play can build an Actor POV screen model from `ActorPovPlayProjection`;
- current place and controlled actor inventory render in distinct stable regions;
- parent, world-inspection, carried-inspection, chrome, connector, and diagnostic regions exist and handle empty/overflow cases;
- focused SadConsole tests cover layout and screen-model behavior;
- normal/debug presentation keeps all gameplay content inside drawable bounds;
- no frontend-owned simulation, containment, action-legality, materialization, or inspectability semantics are introduced.

## Recommended PR order

1. Add `ActorPovPlayLayoutResolver` and focused tests.
2. Add `ActorPovPlayScreenModel` over `ActorPovPlayProjection` and focused tests.
3. Integrate current-place rendering.
4. Integrate controlled actor inventory rendering.
5. Add parent/inspection placeholders and diagnostics.
6. Add chrome/gallery/docs updates for promoted reusable patterns.

## Friction log

- Slice 2 test setup needed projection-rich runtime data while the content-layer projection fixture is private to `tests/GameGameGame.Tests`. Mitigation used: keep a small local SadConsole test fixture that constructs only the facts needed for frontend presentation tests. If this duplication grows during later slices, promote a shared fixture builder instead of copying more world setup.

## Sprint completion note

The sprint is accepted as an MVP foundation, not a final Actor POV layout. The implementation now proves the screen-model and region/component seams needed for finalization work:

- **Connecting lines between spaces in different regions:** feasible through the existing `ConnectorLineViewModel` / `ConnectorLineDrawCallRenderer` path and the dedicated Actor POV connector layer. Next work should derive endpoints from rendered inventory-cell geometry after region/node layout.
- **Individually scalable and pannable inventory spaces:** feasible through per-region component construction plus `ActorPovPlayViewportState`. Current rendering still uses default cell metrics and full viewports; next work should apply named render profiles and persisted viewport state per visible node.
- **Different inventory spaces within each region:** feasible through `ActorPovPlayComponentFactory` and region-local component composition. Current parent chain is a compact text scaffold; next work should replace it with vertically stacked compact inventory-space nodes where useful.
- **Dividing borders between each region:** feasible through the dedicated chrome layer and existing theme-owned panel borders. Current regions mostly rely on component borders; next work should add reusable region separator/chrome components rather than shell-owned drawing.
- **Contextual labels/metadata per region:** feasible through component titles, panel rows/status, and diagnostics chrome. Next work should standardize player-facing labels separately from debug-only diagnostics.

No frontend-owned containment, action-legality, materialization, visibility, or inspectability semantics were introduced. Remaining work is presentation/input refinement over shared projection facts unless future inspectability rules require core-owner coordination.
