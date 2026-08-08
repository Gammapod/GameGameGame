# Merged Inventory Topology Spike Findings

Status: Active spike findings for merged inventory topology layers, non-Euclidean seams, overlap-enabled layout experiments, and future topology-aware rendering. This document records what the spike is trying to prove and the friction discovered so far; durable behavior and test trace still live in `docs/Source of Truth/invariants.md` and the sprint implementation log is archived at `docs/Archived/Merged-Inventory-Layer-Vertical-Slice-Sprint-Plan.md`.

Read when:

- evaluating whether merged inventory layers should graduate from prototype spike to stable topology capability;
- planning overlap, Möbius, portal, generated-join, or topology-aware rendering work;
- deciding what belongs in Core/Content/editor validation versus frontend visualization.

Related:

- `docs/Archived/Merged-Inventory-Layer-Vertical-Slice-Sprint-Plan.md`
- `docs/Source of Truth/invariants.md`
- `docs/Source of Truth/Engine-Editor-Capabilities.md`
- `docs/Source of Truth/testing-charter.md`

## Executive summary

The spike explored whether entity-owned inventories can be composed into durable, navigable topology layers while preserving ordinary Core movement/action semantics. The prototype proved basic merged interiors, three-plus contributors, cardinal seam links, self-wraps, rotations, multi-edge connections, an overlap-enabled eight-room loop, semantic aligned joins for mismatched room/hallway edges, and a flagship folded-house scenario that works well with the debug/test topology POV renderer.

The most important constraint discovered so far is that topology must remain directionally unique: for each resolved cell and direction, Core consumers may see zero or one neighbor. Whenever authoring could create multiple neighbors for one `(cell, direction)`, Content validation must reject it or the model must provide an explicit deterministic resolution policy before movement, adjacency, targeting, POV, or rendering consumes the graph.

## Goals and requirements discussed

### Runtime/topology goals

- Allow multiple entity-owned inventory spaces to behave as one durable navigable interior layer.
- Treat traversal across internal contribution boundaries as ordinary movement, not Enter/Exit, Pickup/Drop, Give/Take, or Transfer.
- Preserve local-owner semantics: Enter/Exit crosses the exterior boundary of the owner whose source inventory cell currently contains the actor.
- Keep merged layers durable even when contributing owners move externally or separate in exterior space.
- Prevent destructive lifecycle actions from orphaning active layer contributors in the prototype.
- Support more than two contributors; the spike has already exercised three-plus contributor layers.
- Support non-Euclidean cardinal seam links, including:
  - Pacman-style self wrap: East↔West and North↔South;
  - rotational self mapping, such as East↔North;
  - multiple different edges connecting the same two spaces;
  - an eight-room loop: A east→B, B south→C, C west→D, D north→E, E east→F, F south→G, G west→H, H north→A.
- Preserve hard directional uniqueness for all resolved topology.

### Authoring/content/editor goals

- Author merged layers in YAML and shared editor services rather than hard-coded runtime fixtures.
- Keep canonical authoring explicit enough that validation can reject ambiguous topology.
- Expose merged layers through Content registry, YAML load/save, editable documents, editor service operations, agent API snapshots/operations, frontend editor summaries, and scenario materialization.
- Provide actionable validation diagnostics for:
  - unknown or non-contributing owner endpoints;
  - unusable inventory contributors;
  - duplicate owner participation in one layer;
  - one owner participating in multiple layers;
  - overlapping layout cells unless explicitly opted in;
  - disconnected layers;
  - non-cardinal seams;
  - seam edge-length mismatches;
  - seam-vs-seam and seam-vs-Euclidean directional conflicts.
- Keep Beta manual showcase scenarios compact and demonstrable.

### Overlap/Möbius-loop goals

- Allow visual/layout overlap without making overlapped cells the same runtime cell.
- Treat runtime cell identity as source-inventory based for the current experiment: layer + owner + source coordinate.
- Require explicit opt-in for overlap via `allowLayoutOverlap: true`.
- In overlap-enabled layers, disable coordinate-derived cross-contributor Euclidean adjacency, preserve ordinary movement inside each contributor's own inventory, and rely on explicit seam/link topology for contributor-to-contributor movement/connectivity.
- Use 1x1 rooms as the first proof of the loop before introducing true Möbius index reversal, sheet IDs, or facing transforms.

### Frontend/rendering goals discussed

- Topology-aware rendering appears technically possible according to frontend-owner feedback.
- Ideal future renderer should be based on POV/current-actor facts instead of raw Euclidean layer coordinates.
- For the experiment, it is enough to hide cells outside the current actor's visible/target range; a full POV-first renderer is out of scope for now.
- Frontends must not invent topology semantics; Core/Content should expose enough facts for the frontend to render or hide cells without redefining adjacency.

## Current prototype state

- Core model includes merged inventory layers, source-space contributions, cardinal edge seams, explicit source-cell links, aligned joins resolved by Content, and `AllowLayoutOverlap`.
- Core topology service composes merged-layer topology into normal movement/adjacency queries.
- Content YAML supports top-level `mergedLayers`, `spaces`, `seams`, `cellLinks`, `joins`, and `allowLayoutOverlap`.
- Scenario materialization copies validated merged layers into `WorldState`.
- Editor service, agent API, and frontend editor summaries can inspect/author the prototype shape.
- Beta topology showcase content includes:
  - base merged-layer acceptance scenarios;
  - three-room/three-contributor scenarios;
  - seam wrap, rotational, multi-edge scenarios;
  - overlap-enabled eight-room loop scenario `delta-merged-layer-overlap-loop`.
  - flagship folded-house scenario `delta-merged-layer-flagship-folded-house` with six 3x3 rooms, hallways, centered joins, and distinct corner objects.

## Content-editor authorship findings

The current content shape is a good spike substrate but not yet a comfortable final authoring language. It is explicit, validated, and hard to make ambiguous, but authors currently have to think in topology-engineering terms: authored entity instance IDs, layer origins, whole-edge seams, and overlap mode switches.

### Authorship experiments tried

- **Explicit Euclidean placement.** `MergedInventoryLayerShowcase.yaml` and `MergedInventoryLayerThreeRoomShowcase.yaml` use `spaces[].owner` plus exact `origin` coordinates to fuse rooms into one layer. This works well for rectangular rooms and hallways that literally touch in layer coordinates. It is deterministic and easy to validate, but it asks authors to calculate layout positions by hand.
- **Three-plus contributor layers.** The three-room linear and bent showcases proved that authoring more than two spaces is viable. The authoring burden grows linearly, but readability remains acceptable while the layer is small and Euclidean.
- **Whole-edge seams.** `MergedInventoryLayerSeamShowcase.yaml` proved self-wraps, rotational self mapping, and multi-edge links using `seams[].first/second` endpoint pairs. This is powerful for same-sized edge joins and compact non-Euclidean tests.
- **Overlap-enabled explicit topology.** The overlap loop uses `allowLayoutOverlap: true` and eight 1x1 rooms at the same layout origin, with explicit seams providing contributor-to-contributor movement. This cleanly separates visual/layout overlap from runtime cell identity and proves impossible loops without coordinate ambiguity. Later 3x3-room/long-hall tests refined the rule so each contributor's own inventory remains internally walkable.
- **Flagship room/hall loop assessment.** A requested flagship shape used 3x3 rooms, 1x1 short halls, and 1x5/5x1 long halls in a non-Euclidean loop. The shape is conceptually excellent, but it exposed a current authoring gap: a 3x3 room edge cannot connect directly to a 1-wide hallway end through the existing whole-edge same-length seam model. The desired authoring primitive is a partial-edge/doorway seam, not another exact-placement workaround.

### Content-authoring friction

- **Instance IDs versus template IDs.** Merged layer spaces reference authored entity IDs placed in carried layouts, not template IDs. This is technically correct because layers are composed from concrete contributor instances, but it is easy to confuse in YAML when entity IDs and template IDs have similar names.
- **Coordinates obscure design intent.** Exact `origin` placement is precise, but authors usually think “Room A connects east to Hall AB” rather than “Room A starts at x=0,y=0 and Hall AB starts at x=3,y=1.” Coordinates are best as a resolved/debug representation, not the first language for authored topology.
- **Current seams are too coarse for doorways.** Whole cardinal edges with same-index, same-length mapping cannot express “middle cell of Room A's east wall connects to the west end of a 1-wide hallway.” This blocks natural room-to-corridor non-Euclidean content unless authors add awkward adapter cells or make all connected edges the same length.
- **Seam notation is precise but terse.** `first: roomA.East`, `second: hallAB.West` records the engine mapping, but not the design sentence: “walking east out of Room A's center doorway enters the hallway from the west.” Optional seam labels/descriptions or higher-level joins would make authored files easier to review.
- **`allowLayoutOverlap` is a semantic mode switch.** In normal layers, origins imply Euclidean adjacency. In overlap-enabled layers, origins are layout-only for cross-contributor relationships; each contributor remains internally walkable by its own inventory grid, and contributor-to-contributor movement is explicit seam/link topology. The rule is useful, but authors need explicit docs/tooling warnings because the same `origin` field means different things in the two modes.
- **Prototype examples prove topology more than place design.** The 1x1 overlap loop is a strong invariant test, but weak as flagship content. Authors need examples with recognizable rooms, corridors, objects, and review instructions once partial-doorway joins exist.

### Partial-doorway/aligned-join findings

The next spike slice added the missing room-to-hallway primitive: a semantic `join` can connect mismatched edges and resolve into lower-level source-cell links. This means a 3x3 room's east edge can connect to a 5x1 hallway's west end with `align: Center`; the resolved topology links the room's middle east cell to the hallway's single west-end cell.

Current behavior:

- `joins[].from` and `joins[].to` reference contributor owner IDs plus cardinal edges.
- `align: Start|Center|End` deterministically aligns the shorter edge span against the longer edge.
- Optional `offset` and `length` exist for explicit partial spans; these are still spike-level and need more authoring examples before promotion.
- Joins resolve to `MergedInventoryLayerCellLink` records. Core consumes cell links, not the authoring sugar.
- Cell links preserve directional uniqueness by naming the exact source cell and direction at each endpoint.

This directly addresses the flagship scenario's first blocker: room edges and hallway ends no longer need equal edge lengths.

### Flagship folded-house finding

The aligned-join slice was sufficient to author the requested flagship shape without adapter cells. The scenario uses six 3x3 rooms connected by a mix of 1x1 short halls, 5x1 east-west hallways, and 1x5 north-south hallways. All room-to-hall doorways use centered joins, while `allowLayoutOverlap: true` permits the loop to fold back to A without requiring Euclidean coordinate closure.

Important nuance: larger overlap-enabled contributors forced a refinement to overlap semantics. The engine must preserve ordinary movement inside each contributor's own inventory grid, otherwise rooms and long hallways become unusable. The current rule is now: overlap mode disables coordinate-derived cross-contributor adjacency only; contributor interiors remain locally walkable, and cross-contributor movement comes from joins/seams/cell links.

The flagship content passed validation and materialization smoke. It remains a manual/player-choice demonstration rather than a fully automated traversal test.

### Recommended ideal authoring shape

For normal content, prefer **semantic relative joins with deterministic alignment** as the author-facing source shape. Exact placement should remain available as an advanced/debug escape hatch and as a possible resolved canonical output. Random placement should not be part of ordinary authored scenario YAML; if needed, it belongs in a separate seeded generation layer.

Suggested idealized shape:

```yaml
layers:
  foldedHouse:
    placementMode: AutoDeterministic

    spaces:
    - id: roomA
      template: room3x3
    - id: hallAB
      template: hallway5x1
    - id: roomB
      template: room3x3

    joins:
    - from: roomA.East
      to: hallAB.West
      align: Center

    - from: hallAB.East
      to: roomB.West
      align: Center
```

For unequal edge sizes and doorways, the same model should allow explicit offset/length when deterministic alignment is not enough:

```yaml
    joins:
    - from: { space: roomA, edge: East, offset: 1, length: 1 }
      to: { space: hallAB, edge: West }
```

Recommended authoring tiers:

1. **Default semantic joins:** `roomA.East -> hallAB.West` with `align: Start|Center|End`. This is the clearest content-editor vocabulary for rooms and corridors.
2. **Deterministic offset joins:** optional `offset` and `length` for doorways, partial edges, or asymmetric joins. Validation must still preserve directional uniqueness.
3. **Exact placement escape hatch:** explicit coordinates for tests, debug fixtures, generated canonical output, or rare handcrafted topology that cannot be expressed cleanly with joins.
4. **Seeded generation only:** any randomized layout choice should be isolated under a generation block with a stable seed and should resolve to deterministic joins/placements for validation and review.

This combination keeps the authoring language close to design intent while preserving the current spike's most important invariant: before Core consumes a layer, every resolved `(cell, direction)` must have zero or one neighbor.

## Friction encountered so far

### Directional ambiguity

The earliest ambiguity came from source inventories being able to map to multiple layer coordinates if the same owner appeared more than once or in more than one layer. The prototype now rejects duplicate owner participation in one layer and multi-layer participation for one owner.

Seams and cell links introduced additional ambiguity: a seam/link can conflict with another seam/link or with an ordinary Euclidean neighbor. Validation now rejects directional conflicts. For overlap-enabled layers, coordinate-derived cross-contributor Euclidean adjacency is disabled, so contributor-to-contributor neighbors come only from explicit seams or cell links while each contributor's own inventory grid remains internally walkable.

### Coordinate identity versus layout coordinates

The initial model treated layer coordinates as both layout and topology identity, which made overlap invalid. The overlap loop forced a distinction: overlapping origins can be layout-only, while runtime movement uses source inventory cell identity plus ordinary same-contributor movement and explicit seam/cell-link movement between contributors.

This is enough for 1x1 loop experiments, but it is not a complete graph/sheet model. Future true Möbius support may need explicit sheet/instance identity, index transforms, and facing transforms.

### Seam limitations

Current seams are cardinal-only, symmetric, and same-index. This deliberately avoids ambiguous interpolation/stretching but limits expressiveness. Edge-length mismatch is rejected in Content validation. True Möbius behavior will probably need index reversal and optional facing transforms.

### Connectivity validation changed with overlap

Euclidean placement connectivity works for ordinary layers. Overlap-enabled layers need source-cell connectivity rather than coordinate connectivity, otherwise all overlapped cells would appear connected or ambiguous by coordinate. Validation now uses same-contributor inventory adjacency plus explicit seams/cell links when `allowLayoutOverlap` is true.

### Frontend POV rendering spike findings

SadConsole can consume the new topology shapes well enough for useful debug experiments, but the current renderer is still layer-first rather than POV-first. The frontend experiments added an `F8` topology POV debug mode in Consumer Play mode. In that mode, the current-place grid becomes a depth-2 actor-relative topology view: the controlled actor is centered, only topologically nearby cells are shown, and display slots that contain multiple distinct destination cells are shown as yellow count markers.

What fit cleanly into the current renderer:

- The existing `ActorPovPlayScreenModel` and current-place component were a good seam for a debug POV mode. The experiment could be toggled without changing Core movement/action semantics or reviving Console workflows.
- Core's merged-layer topology services were sufficient to produce a bounded local traversal from the actor. The frontend did not need to reimplement movement legality; it only projected topology-neighbor facts into actor-relative display slots.
- Restricting the displayed cells to a small actor-relative range worked well in ordinary open spaces and in overlap-enabled loop rooms. It avoided drawing the whole merged layer and made the local topology easier to reason about.
- Counting only **distinct destination `PlaneCoord`s** for a rendered-slot overlap was the right debug rule. Earlier route-count markers over-reported ordinary grid path multiplicity; distinct-cell markers better identify actual topology/render ambiguity.
- A simple count marker is readable enough for the spike: it communicates “there are multiple real cells in this apparent slot” without requiring final diagonal or polygon rendering.

What caused friction:

- The current inventory-space renderer is fundamentally one-logical-cell / one-primary-visual. It can show count markers, but it does not naturally represent multiple topology cells inside one rendered slot.
- A quick split-cell experiment using larger `3x3` cells and diagonal mini-glyphs caused rendering and visibility breakage in manual review. The result was not readable, and in some rooms the topology POV stopped displaying useful space. This suggests diagonal slicing is a renderer-level feature, not something to keep hacking into `InventorySpaceViewModel`.
- The current mixed-scale inventory renderer path is optimized for single-glyph cells and does not support subcell clipping, multiple branch lanes, or triangle masks.
- SadConsole could probably support true split-cell rendering through lower-level MonoGame draw calls, custom surfaces, pixel-positioned overlays, or tile-atlas tricks, but that would push the project below SadConsole's comfortable tile/component abstraction. Engines with native sprites, polygons, masks, shaders, cameras, and scene graphs, such as Godot or Unity, would likely make diagonal slicing and Ape Out-style geometry easier to program.
- Conversely, if the design accepts a rule that each rendered cell is either fully visible or hidden, never partially split, SadConsole fits much better. Full-cell visibility keeps the presentation aligned with glyph/background/decorator rendering, viewport clipping, distance dimming, warning/count markers, and debug panels.
- Existing inspection panels and debug connectors are not topology-POV-aware. Adjacent inspection panels can still say “No adjacent” while the topology POV shows nearby cells, and connector lines can cut through or anchor misleadingly against the actor-relative view.
- The current projection uses display slots derived from accumulated direction offsets. That is good enough for depth-2 debug work, but it is not a stable final model for larger ranges, loops, branch folds, or facing-relative presentation.

Patterns that seem well-suited to this game:

- **Actor-relative local topology** is a promising default. It matched the design goal that an entity can move around non-Euclidean corners and still see only what is topologically close, not the whole layer.
- **Visible range as topological distance** is a better player-facing concept than Euclidean layer extent. Depth-limited traversal gave useful darkness/visibility boundaries even in ordinary rooms.
- **Overlap markers as ambiguity warnings** work as debug presentation. A final UI may replace them with sliced geometry, but count markers are a good fallback/diagnostic layer.
- **Full-cell visibility is the SadConsole-friendly presentation path.** If the game can communicate topology with actor-relative unfolding, hidden/shown cells, distance tinting, and explicit ambiguity markers rather than partial-cell geometry, SadConsole remains viable for much more of the final play surface.
- **Path identity matters.** For future rendering, visible facts should preserve route/branch information long enough to decide how cells are unfolded or split. Flattening too early to absolute coordinates or layer coordinates hides the topology the player needs to understand.
- **Presentation should stay entity-neutral.** The controlled actor can be centered/focused by frontend state, but the underlying visible-cell facts should work for any observer entity.

Recommended approach for a future POV-first renderer:

1. Add or promote a shared frontend-neutral POV visibility projection before renderer work becomes durable. Inputs should include world, observer entity, range/source of visual range, topology service, and presentation lookup. Outputs should include visible topology nodes, distance, source `PlaneCoord`, occupant/entity appearance, route/path metadata, overlap groups, and diagnostics. This keeps Core/Content ownership of topology facts while leaving layout and styling to the frontend.
2. Treat the rendered view as an **unfolded local topology**, not a cropped merged-layer map. The renderer should place cells by actor-relative branch geometry, preserving multiple branches that land in the same screen slot until a visual conflict policy resolves them.
3. Decide whether the durable presentation is full-cell or split-cell. A full-cell rule points toward continuing in SadConsole with actor-relative unfolding, count/warning markers, and distance tinting. A split-cell/diagonal-geometry rule points toward either a dedicated lower-level MonoGame renderer inside the SadConsole app or a future frontend in a sprite/polygon-native engine such as Godot or Unity.
4. If split-cell presentation remains the long-term target, build a dedicated POV surface renderer instead of extending the current inventory-space renderer further. The new renderer should support multiple visuals per apparent cell, branch lanes, subcell/polygon clipping, diagonal/triangular splits, and distance-based shading.
5. Target an Ape Out / Hotline Miami style: a bold actor-centered play surface, limited local visibility, strong silhouettes, high-contrast walls/cells, and later distance fade/darkness. Topological ambiguity should look intentional, like folded space, not like duplicated UI text.
6. Keep count markers and debug overlays as fallback diagnostics. Even after diagonal slicing exists, debug mode should be able to show distinct-cell counts, path labels, source plane/coord, and overlap-group membership.
7. Integrate interaction/inspection with the same projection. Adjacent/nearby inspection panels, valid action highlighting, mouse hit-testing, and action-preview arrows should all consume the POV visibility projection rather than separate current-place/absolute-coordinate queries.
8. Prototype visual conflict policies in a component gallery before making them canonical. Useful candidates include diagonal split for two branches, quadrant split for up to four branches, count-marker fallback for dense overlaps, and branch-colored edge wedges for line-of-sight folds.

### Cross-discipline alignment check

The content-editor and frontend-owner findings agree with the Core spike results:

- Content wants semantic relative joins with deterministic alignment as the default author-facing language. Core's lower-level finding supports this: resolved source-cell links are a cleaner runtime primitive than making authors express every doorway as coordinates or whole-edge seams.
- Content wants exact placement as a debug/escape-hatch representation rather than the normal source language. Core's overlap and directional-uniqueness findings support that: coordinates alone are not stable topology identity once folded/overlapping spaces exist.
- Frontend wants a POV/topological-distance view rather than a full Euclidean merged-layer map. Core's graph-like source-cell/seam/link model supports that better than a single global coordinate plane.
- Frontend found full-cell actor-relative visibility to be the SadConsole-friendly path, with count markers/debug overlays for ambiguity. This matches the current recommendation to defer split-cell/polygon rendering and first promote a shared, frontend-neutral visible-topology projection.
- All three perspectives depend on the same hard invariant: every resolved `(cell, direction)` must be unique before movement, validation, authoring previews, or rendering consume the topology.

## Wrap-up recommendation

Recommended path: keep the current spike branch (`topology-spike`) as reference evidence, save this findings document to main, and re-implement merged topology layers cleanly from the recommendations rather than hardening the spike branch in place.

Rationale:

- The spike intentionally moved fast across Core, Content, authored YAML, and SadConsole debug rendering. It proved the concept, but the code shape includes spike-era names and surfaces that should be reconsidered before becoming durable API.
- A clean implementation can start from the promoted lessons: semantic joins first, resolved source-cell links as the runtime representation, overlap as layout-only cross-contributor behavior, explicit directional conflict validation, and a future shared topology-POV projection.
- Advanced presentation work should remain backlog/deferred until the Core/Content topology contract is stable. The debug renderer is good evidence, not yet the final rendering architecture.

Suggested prioritized implementation plan for a clean branch:

1. Core: define stable source-cell topology node/link primitives and preserve movement/adjacency semantics over them.
2. Content validation: author semantic joins, resolve them to source-cell links, and reject directional ambiguity.
3. Content/editor parity: expose stable YAML/editor/agent shapes only after names and validation semantics are settled.
4. Scenarios: rebuild a compact room-hall flagship and only then restore richer folded-house showcase content.
5. Frontend: add a shared visible-topology/POV projection before promoting any SadConsole-specific debug rendering.

### Windows build/file locks

Several verification runs hit transient file locks from long-lived `GameGameGame.Content.Tools` or SadConsole processes. Stopping the locking process and rerunning resolved the issue. Avoid parallel builds/tests against the same projects while a tool/frontend process is still running.

### Content authoring permissions/workflow

Core-owner could update Core/Content code and tests directly, but authored YAML showcase edits were delegated to content-editor due repository permission rules around `src/GameGameGame.Content/**/*.yaml`. This worked well and kept content-authoring validation separate from engine/editor implementation.

## Open questions / follow-up design decisions

- Should future seams support explicit directionality instead of always symmetric links?
- Should true Möbius loops transform actor facing on traversal, or should facing remain the submitted movement direction?
- What mapping modes are needed beyond same-index: reverse-index, offset, scale/stretch rejection, or explicit cell-pair maps?
- Do larger overlapped spaces require explicit sheet/instance IDs to preserve runtime identity?
- Should coordinate-derived cross-contributor Euclidean adjacency ever be optionally re-enabled in overlap-enabled layers through explicit override rules, or should explicit joins/links remain the only cross-contributor topology in overlap mode?
- What Core/Content projection should the frontend consume for topology-aware rendering: raw graph facts, POV-limited visible cells, or both?
- How should editor previews show overlapping rooms, seam arrows, local ownership, and hidden/non-visible cells without duplicating Core semantics?
