---
id: plan.sadconsole-linked-containment-play-mode-sprint
title: SadConsole Linked Containment Play-Mode Sprint Plan
kind: plan
status: archived
truth_rank: 50
truth_domains: [planning-priority, frontend-presentation]
owners: [frontend-owner]
audience: [frontend-owner]
lane: frontend-ux
read_when:
  - implementing the next consumer Play-mode UI sprint
  - planning linked inventory-space node layout connector overlays or Play-mode inspection replacement
related:
  - source.sadconsole-ui-specification
  - plan.sadconsole-frontend-roadmap
  - source.frontend-ux-invariants
  - source.frontend-ux-standards
  - source.frontend-ux-decisions
---

# SadConsole Linked Containment Play-Mode Sprint Plan

Status: Completed focused sprint plan. The proof of concept is accepted as successful: consumer Play mode can render linked inventory-space nodes with a smooth connector, fallback geometry, and developer diagnostics without introducing frontend-owned simulation semantics.

## Goal

Implement the first sprint-sized step toward the selected consumer Play-mode UI direction: Play mode presents containment as linked inventory-space nodes. This sprint should move from the current single centered current-space grid to a reusable, test-backed linked-space presentation for the smallest useful case: current place plus one inspected contained entity's inventory space, with connector geometry and overlays layered predictably.

## Source direction

This plan executes the first four proposed implementation slices from `docs/Source of Truth/SadConsole-UI-Specification.md`:

1. connector-line presentation contract;
2. pure linked-space layout for current place plus one linked inspected space;
3. consumer Play-mode replacement of separate inspection popup/panel behavior with the linked two-space presentation;
4. action prompt and debug overlays that continue to float above the linked-space canvas.

## Non-goals

- Do not change Core action legality, Action Choice semantics, turn advancement, materialization, containment, POV, or log projection rules.
- Do not implement arbitrary whole-scenario tree display.
- Do not implement shared-root branching, long breadcrumb-chain browsing, or pin/collapse state beyond what is needed to keep the two-space layout stable.
- Do not add mouse click-to-act, click-to-focus, click-to-expand, or click-to-inspect behavior. Geometry and hit-test facts may be exposed for later work.
- Do not implement user-customizable layouts, fully responsive window resizing, or pixel-perfect leftover-monitor-pixel centering.
- Do not add new editor workflows or revive Console-specific workflows.

## Current baseline

- `ConsumerPlayModeScreen` currently projects the controlled actor/current place and exposes one centered `InventorySpaceComponent` through `CurrentSpaceGridComponent(...)` / `Components(...)`.
- `ConsumerPlayModeConsole.Redraw()` draws the current-space grid, optional `F12` debug text, prompt overlay, and one-tile border buffer.
- `ConsumerPlayModeLayout` owns fullscreen-derived logical dimensions and drawable bounds only; it does not yet expose named Play subregions.
- `InventorySpaceViewModel` / `InventorySpaceComponent` already provide renderer-neutral inventory-space cells, required size, render profiles, and stable coordinate-to-cell bounds.
- `ComponentGalleryScreen` contains a connector-line spike and the inventory-space gallery example, but connector lines are not yet a reusable contract.
- Completed mock-layout work (`GameplayMockLayout`) is the reference for pure named/layered layout regions, topmost hit-testing, and debug diagnostics.

## Sprint slices

### Slice 1: Promote connector-line spike into a reusable presentation contract

Implementation status: Complete. Added a frontend-owned connector-line presentation model/component, updated the component gallery from spike wording to an accepted connector-line pattern, and added focused connector/gallery tests.

Friction encountered: `PresentationColor` lives in `GameGameGame.Content`, while the new model was added under SadConsole frontend components. The first targeted SadConsole test run failed on missing namespace imports.

Mitigation used: kept connector lines as frontend presentation models but explicitly imported the existing shared presentation-color DTO rather than introducing a duplicate frontend color enum. The second targeted SadConsole test run passed.

Create a small frontend-owned connector presentation model and gallery-backed renderer contract. The contract should describe endpoints, segments, layer/z-order intent, redraw timing assumptions, and a fallback tile-line strategy if smooth pixel lines are unavailable or deferred.

Acceptance notes:

- Connector endpoints are presentation geometry derived from resolved component/cell bounds, not simulation facts.
- The contract is usable without launching SadConsole.
- The component gallery replaces the loose spike wording with an accepted connector-line example.
- Connector lines are part of the linked-space canvas, not action prompt overlays.

### Slice 2: Add pure linked-space layout for current place plus one linked inspected space

Implementation status: Complete. Added a pure linked inventory-space layout model for single-node fallback and current-place-plus-linked-inspected-space layout, connector endpoint generation, clipping/omission status, and future mouse-inspection hit regions. Added `InventorySpaceComponent.CellBounds(...)` so connector anchors reuse inventory-space geometry instead of duplicating cell math.

Mouse-click note: click-to-choose-inspected-entity remains out of this sprint slice, but the layout now exposes node and parent-cell hit regions that a later UI-N05 mouse-input slice can route to the same inspection selection state used by keyboard/controller workflows.

Friction encountered: the first compile after adding the layout exposed ordinary C# namespace/nullability/scope issues: `PresentationColor` needed the Content namespace, test code needed the SadConsole app namespace for `SadConsoleRect`, and repeated local variable names in separate branches conflicted with C# local scope rules.

Mitigation used: kept fixes mechanical and boundary-preserving: imported existing shared DTO namespaces, renamed branch locals, and explicitly narrowed nullable child/coord inputs after the single-node fallback branch. The targeted SadConsole test run passed after those fixes.

Add a pure, test-backed layout model that receives drawable bounds plus two inventory-space requirements and resolves:

- current-space node bounds;
- linked-inspected-space node bounds;
- the parent entity's cell anchor in the current-space node;
- connector endpoints/segments;
- optional stable hit-test facts for node bounds and inventory cells.

Acceptance notes:

- The resolver should be deterministic and independent of SadConsole host state.
- The first layout may use simple relative placement and min-size rules; do not introduce a broad layout DSL.
- Both nodes must stay inside the provided drawable bounds or report an explicit clipped/omitted presentation state.
- Parent entity cell geometry should use `InventorySpaceViewModel.CellBoundsFor(...)` or the existing stable inventory-space geometry rather than duplicating cell math.

### Slice 3: Replace separate Play-mode inspection presentation with linked two-space view

Implementation status: Complete for first-pass automatic linked-inspected-space selection. Consumer Play mode now builds a linked-space presentation from the current-place inventory space plus the first non-controlled local entity with a usable inventory grid, renders both inventory-space nodes, and draws the connector fallback through the SadConsole component renderer. If the linked inspected space is unavailable or cannot fit, the layout falls back to a single current-space node.

Cleanup status: Before checkpoint, shared MonoGame connector drawing was extracted so the component gallery and consumer Play mode use the same draw-call helper. The implementation terminology was also broadened from `InspectedChild` to `LinkedInspectedSpace` where practical, because future selection may come from clicking, keyboard/controller focus, active targets, nearby entities, or POV-derived automatic policy rather than only child containment.

Follow-up adjustment from frontend review: Play mode now prefers MonoGame `DrawCallCustom` smooth connector lines instead of tile fallback lines for the normal linked-space connector. Connector endpoints support sub-cell anchors so the first accepted shape can draw directly from the parent entity cell center to the inspected inventory node's left edge. Tile fallback remains available as the degraded rendering strategy and for pure geometry tests.

Friction encountered: the prototype session already had an inspectable child, so an initial fallback test that assumed no inspectable child was present failed.

Mitigation used: changed the fallback test to exercise the actual fallback condition owned by this slice: constrained drawable bounds that cause child omission/clipping. Kept child selection automatic and presentation-only; explicit keyboard/mouse selection remains a later interaction slice over the hit regions added in Slice 2.

Use the new linked-space layout in consumer Play mode so an inspected contained entity with a usable inventory space appears as a second inventory-space node beside the current place. Draw a connector from the entity's rendered cell in the parent node to the child node.

Acceptance notes:

- Normal Play mode remains player-facing and uncluttered: no title/frame/debug rows unless an accepted render profile requires them.
- Existing movement and Select/Cancel prompt behavior must keep working.
- If no linked inspected space exists, Play mode should retain the current single current-space behavior.
- The linked child space is presentation over existing Content/Core projection facts; no frontend ancestry guessing should become gameplay semantics.

### Slice 4: Keep prompts and debug overlays floating above the linked-space canvas

Implementation status: Complete. Consumer Play `F12` diagnostics now include linked-layout developer state: drawable bounds, layout status, node count, connector availability, connector render mode, linked-inspected-space identity/location/grid, node bounds/clipping, parent-cell bounds, connector endpoint anchors, and hit-region summaries. Prompt overlays remain separate from linked inventory-space nodes, and the smooth MonoGame connector is suppressed while a prompt overlay is active so prompts remain visually topmost; the diagnostic rows explicitly report that suppression state.

Friction encountered: opening a deterministic prompt in tests required using a size-calibration interaction path rather than assuming `SubmitDefaultAction()` always opens a prompt from turn zero.

Mitigation used: drove the test through existing Play-mode helper movement toward an interactable item, then opened a prompt only after the shared Action Choice state exposed one. This preserved the rule that frontend tests should not fabricate action legality or prompt state.

Route prompt and `F12` debug overlay placement through the same layer policy used by the linked-space canvas. Prompts must remain topmost over nodes/connectors, while debug overlay remains developer-only and non-mutating.

Acceptance notes:

- `PromptComponent(...)` remains an overlay and is not laid out as a child inventory-space node.
- `F12` diagnostics include enough linked-layout facts to diagnose node bounds, connector endpoints, and any clipping/omission.
- Overlays must not move the underlying inventory cell geometry.
- Existing prompt-stack tests continue to pass; add focused tests only for overlay layering/placement changes.

## Affected component trace

| Area | Existing component/file | Expected effect |
|---|---|---|
| Play screen model | `src/GameGameGame.SadConsole/Ui/Screens/ConsumerPlayModeScreen.cs` | Add linked-space view-model production, optional linked-inspected-space node selection, linked component list, and debug rows for linked layout. Preserve action submission and prompt-stack ownership. |
| Play layout shell | `src/GameGameGame.SadConsole/Ui/Screens/ConsumerPlayModeLayout.cs` | May remain display-shell-only, or gain references to resolved named/layered Play regions if needed. Do not put containment semantics here. |
| Play renderer | `src/GameGameGame.SadConsole/Ui/Rendering/ConsumerPlayModeConsole.cs` | Draw linked-space canvas, connector lines, debug overlay, prompt overlay, and border buffer in deterministic z-order. |
| Display/fullscreen boundary | `src/GameGameGame.SadConsole/Ui/Rendering/ConsumerPlayModeDisplay.cs` | No expected change except preserving existing fullscreen/drawable-bounds behavior. |
| Inventory-space component | `src/GameGameGame.SadConsole/Ui/Components/InventorySpaceViewModel.cs` | Reuse stable required-size/cell-bounds geometry; add helpers only if linked layout exposes a real missing presentation primitive. |
| Component renderer | `src/GameGameGame.SadConsole/Ui/Rendering/SadConsoleComponentRenderer.cs` | Likely affected for reusable connector drawing or overlay/layer ordering if renderer-owned. |
| Component gallery | `src/GameGameGame.SadConsole/Ui/Screens/ComponentGalleryScreen.cs` and `Ui/Rendering/ComponentGalleryConsole.cs` | Promote connector-line example from spike to accepted reusable pattern; keep inventory-space example current. |
| Mock layout reference | `src/GameGameGame.SadConsole/Ui/Screens/GameplayMockLayout.cs` and `Ui/Rendering/GameplayMockConsole.cs` | Reference only unless shared layout primitives are extracted. Do not regress existing mock diagnostics. |
| Shared services consumed | `EntityPanelProjectionService`, `EntityContainmentPathService`, `PlayableScenarioSession`, Action Choice services | Consume existing facts only. If needed child-space facts are unavailable, log/escalate to owning layer rather than inventing frontend semantics. |

## Expected new components

Names are suggestions; implementation may choose equivalent names if the same boundaries are preserved.

| Proposed component | Owner/layer | Purpose | Testing trace |
|---|---|---|---|
| `ConnectorLineViewModel` / `ConnectorLineSegment` | SadConsole frontend presentation model | Describe connector endpoints, segment kind, z-order role, and fallback tile rendering data independent of SadConsole host APIs. | New `ConnectorLineViewModelTests` or added `SadConsoleComponentGalleryTests` coverage asserting endpoint preservation, deterministic fallback segment output, and no dependency on simulation data. |
| `ConnectorLineComponent` or renderer helper | SadConsole frontend rendering adapter | Render connector line model through accepted SadConsole pattern; gallery demonstrates the pattern. | `SadConsoleComponentGalleryTests` asserts accepted connector example exists and exposes the model; renderer behavior may be smoke-tested through component rows or focused renderer helper tests if pure. |
| `LinkedInventorySpaceLayout` | SadConsole frontend pure layout model | Resolve two inventory-space node bounds, parent-cell anchor, child-node anchor, connector model, layer IDs, and optional clipped/omitted state from drawable bounds and required sizes. | New `LinkedInventorySpaceLayoutTests` covering normal viewport, narrow/small viewport, deterministic rounding, inside-drawable constraints, and connector endpoints derived from parent cell geometry. |
| `LinkedInventorySpaceNode` / `LinkedInventorySpaceViewModel` | SadConsole frontend screen/view model | Carry current-place and linked-inspected-space inventory components plus relationship/anchor metadata for Play rendering. | `ConsumerPlayModeScreenTests` asserting single-node fallback, two-node linked view when linked inspected inventory exists, node IDs/bounds/options, and connector anchor facts. |
| `LinkedInventorySpaceCanvasComponent` or equivalent component list | SadConsole frontend component composition | Present nodes/connectors as one canvas so prompts/debug overlays can layer above it. | `ConsumerPlayModeScreenTests` and/or new component tests asserting canvas bounds remain inside drawable area and normal components do not include prompt/debug overlays. |
| Linked-layout debug rows | SadConsole frontend diagnostics | Report node bounds, connector endpoints, layer IDs, and clipped/omitted state under `F12`. | `ConsumerPlayModeScreenTests` asserting debug rows include linked-layout diagnostics without requiring the SadConsole window. |

## Testing trace by slice

| Slice | Required automated trace | Manual/deferred trace |
|---|---|---|
| 1. Connector contract | Add focused connector model tests and update `SadConsoleComponentGalleryTests` from “spike exists” to “accepted connector-line pattern exists”. If rendering behavior is pure enough, test fallback tile-line segment generation. | Manual gallery review for smooth/pixel connector appearance and acceptable fallback visuals. |
| 2. Linked two-space layout | Add `LinkedInventorySpaceLayoutTests` for default large drawable, constrained drawable, minimum sizes/clipping, stable layer ordering, and connector endpoint derivation from inventory cell bounds. | Manual review only for aesthetic placement; geometry must be automated. |
| 3. Consumer Play linked view | Extend `ConsumerPlayModeScreenTests` to cover single-space fallback, two-space linked presentation from a fixture/session with an inspectable child inventory, components inside drawable bounds, and no regression to movement/action prompt submissions. | Manual Play smoke in normal mode to confirm the uncluttered linked two-space view reads correctly. |
| 4. Overlay layering/debug | Extend `ConsumerPlayModeScreenTests` and/or renderer helper tests to assert prompts remain separate overlays, debug rows include linked-layout facts, and debug labels/overlays do not change underlying node/cell geometry. Existing prompt-stack tests remain the behavior trace. | Manual `F12` smoke to confirm topmost diagnostics and prompt overlay visibility over connectors/nodes. |

## Definition of done

- Consumer Play mode can show current space plus one linked inspected inventory space connected from parent entity cell to the linked node.
- Single current-space Play mode still works when there is no child space to inspect.
- Action prompts remain Select/Cancel stack overlays and movement/action submissions still route through shared services.
- `F12` debug mode reports linked-layout diagnostics without mutating simulation state.
- Component gallery contains the accepted connector-line pattern and still contains the inventory-space pattern.
- New or updated tests cover connector model, linked layout geometry, consumer Play screen composition, and overlay/debug separation.
- No frontend code introduces simulation/content semantics; any missing shared facts are recorded as follow-up rather than patched in SadConsole.

## Follow-ups intentionally left after this sprint

- Connector appearance controls: color/style/thickness, endpoint selection such as center/corner/edge anchors, multiple connector lines, and profile/theme-driven connector treatments.
- Explicit inspected-space selection: keyboard/controller cycling and mouse click-to-inspect/focus/select over node and cell hit-test facts, reusing frontend presentation state and shared projection facts.
- Automatic inspected-space policy: choose which spaces are shown/hidden from current actor context, root-relative location, nearby/adjacent entities, active targets, and POV-service facts rather than hardcoded first-local-entity selection.
- Breadcrumb-chain layout using `EntityContainmentPathService` / POV breadcrumbs.
- Shared-root branching and partial tree navigation.
- Collapsed/expanded/pinned branch state.
- Generalized recursive relative layout resolver.
- Responsive window-pixel-to-cell recalculation and pixel-perfect leftover-margin centering.
