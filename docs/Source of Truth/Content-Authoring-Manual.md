# Content Authoring Manual

Status: Source of truth for content-editor-facing authoring capabilities and workflows.

Audience:

- content-editing agents;
- future human authors secondarily.

Read when:

- starting any content-authoring or content-review session;
- deciding what can be expressed with current content tools;
- writing beta scenarios, primitive showcases, or capability-gap notes.

Do not read when:

- changing Core behavior or tests; use `docs/Source of Truth/invariants.md` first;
- checking implementation-layer support tiers; use `docs/Source of Truth/Engine-Editor-Capabilities.md`;
- researching future/planned capability priority; use `docs/Plans/Beta-Capability-Gap-Log.md` and current planning docs.

This document is optimized for fast agent onboarding. It records what content-editing agents can safely author now. If a desired capability is not listed here as authorable, do not assume it is available because it exists in Core, archived plans, tests, or low-level descriptors. Use the capability-gap workflow instead.

## Agent quick start

Minimum read path for content work:

1. `Agent quick start`
2. `Authoring decision rules`
3. `Current authoring surface`
4. `Currently authorable Action Step catalog`
5. `Scenario authoring`, when working on scenarios
6. `Capability gap workflow`, when blocked

Default workflow:

1. Author normal content definitions: entity templates, presentations, inventories, action plans, and persisted scenarios.
2. Prefer canonical ordered behavior chains for new behavior.
3. Use currently authorable Action Steps from this manual.
4. Prefer direct `ggg_content_*` tools backed by `AgentContentEditorApi` when available: open/create a session, inspect existing content, make semantic edits, validate, review the snapshot diff, and save deliberately.
5. Validate after edits.
6. Preview action plans when behavior changes.
7. Materialize or run scenarios when behavior needs inspection; use SadConsole/manual play for visual inspection and treat the current PNG/GIF recorder as legacy fallback tooling.
8. Log a capability gap when desired content cannot be expressed cleanly with the authoring surface listed here.

## Authoring decision rules

- Treat this manual as the content-facing authority for current authorability.
- Use editor/content services and agent/editor APIs when available; do not bypass them with ad-hoc YAML changes unless explicitly requested.
- Use canonical behavior chains for new normal action plans.
- Do not create new content that depends on arbitrary action-plan variables, `SetVariable`, or legacy low-level check/effect authoring.
- Legacy low-level plans may remain loadable/editable for compatibility, but they are not the preferred model for new content.
- Scenarios should compose normal templates, presentations, inventories, action plans, and player insertion data. Do not invent scenario-only scripting.
- If the engine can do something but this manual does not list it as authorable, classify or log the missing content workflow instead of relying on implementation details.

## Current authoring surface

| Area | Currently authorable |
|---|---|
| Documents | Create, open, save, reload, validate, preview content documents, and request combined scenario review reports. Content-editing agents may use session-aware `ggg_content_*` tools from `GameGameGame.Content.Tools` for direct AgentContentEditorApi-backed open/create/snapshot/validate/save workflows. |
| Entity templates | Create, edit, duplicate, delete, and reorder templates. |
| Presentations | Assign/edit presentation data used by authored templates. |
| Inventory / containment | Inventory dimensions, bulk, aperture, and carried entity layout. |
| Actor state | Initial actor `Facing` through `actionStateDefaults.facing`; target-selection rules through `targetingRules`. |
| Action-plan assignment | Assign or clear an entity template's default action plan. |
| Action plans | Create, edit, delete, reorder, preview, and validate action plans. |
| Canonical behavior chains | Add, remove, and reorder catalog-backed Action Steps. |
| Scenarios | Persist scenario name/root/player template/player entity ID/player start placement, plus first-slice authored player-control bindings from player/input IDs to materialized entity IDs. |
| Scenario materialization | Materialize persisted scenarios through shared content/editor services. |
| Playable session launch | Create frontend-neutral playable sessions from persisted scenarios or catalog entries through the shared Content launcher, including world, registry/presentation lookup, action plans, player entity, active plane/container, diagnostics, runtime failures, and capability gaps. |
| Scenario execution | Launch persisted scenarios in the official SadConsole frontend directly by content file/scenario ID or through the SadConsole scenario browser populated from one file, folder discovery, or a curated manifest. Scenario catalog candidate scanning lives in Content through `ScenarioCatalogScanService`/`ScenarioCatalog.ScanCandidates`, but curated manifests are the author-facing source for sections, ordering, descriptions, status/lifecycle, tags, provenance, and browsing intent. Run persisted scenario reports by scenario ID with final-state and inventory/containment summaries; request compact player narrative projection message IDs/args through `ggg_content_run_scenario_player_log_by_id`; request combined validation/preview/materialization/run reports; run root-only compatibility reports when intentionally inspecting a scenario-root template without player insertion. |
| Scenario recording | Legacy fallback: record persisted scenarios to PNG frames and GIF artifacts when reports/SadConsole are insufficient. Future visual recording should prefer history playback / SadConsole-rendered export. |
| Gap logging | Record unsupported desired behavior in the active capability gap log. |

For maintainer-facing layer coverage, support tiers, and parity details, use `docs/Source of Truth/Engine-Editor-Capabilities.md`.

## Entity template authoring

Author entity templates as reusable normal content. Current safe operations include:

- create, edit, duplicate, delete, and reorder templates;
- assign presentation data;
- assign or clear a default action plan;
- configure inventory dimensions, bulk, and aperture;
- place, remove, move, replace, and overwrite carried entities in authored inventory layouts through supported editor/API workflows;
- set initial actor `Facing` when the template acts through action plans;
- configure target-selection rules for target-consuming Action Steps.

Prefer reusable templates over scenario-specific one-off definitions. Scenarios should reference templates rather than encoding special behavior outside normal content structures.

## Inventory and containment authoring

Current inventory authoring supports:

- inventory dimensions;
- entity bulk;
- aperture;
- authored carried-entity layout, including placement, removal, movement, template replacement, and coordinate overwrite through supported editor/API workflows;
- pickup/drop-oriented content using supported Action Steps.

Use constrained inventory behavior where possible:

- `TransformAdjacentToInventory` attempts to place the current adjacent `Target` into actor inventory. `PickupTarget` remains a compatibility name for the same semantics.
- `TransformInventoryToAdjacent` attempts to drop a carried entity in the actor's facing direction. `DropFacing` remains a compatibility name for the same semantics.
- `GiveTarget` transfers the actor's first carried entity into the current `Target` inventory.
- `TakeTarget` transfers the current `Target`'s first carried entity into actor inventory.
- `EnterTarget` moves the actor into the adjacent current `Target` inventory.
- `ExitFacing` moves the actor out of its current containing entity toward `Facing`.

Bulk/Aperture checks apply to every inventory boundary crossed by these constrained behaviors. For nested interiors, entering from inside one entity into another crosses both the source containing entity's aperture and the destination entity's aperture; exiting crosses the current containing entity's aperture. This is intentional: use larger apertures for interiors meant to allow passage, or use `Teleport` for exceptional movement that should bypass aperture rules.

For player-controlled actors, the shared Core Action Choice seam can expose authored `TransformAdjacentToInventory`/`PickupTarget` as selectable adjacent pickup targets plus inventory-slot destinations, and authored `TransformInventoryToAdjacent`/`DropFacing` as selectable carried sources plus adjacent map destinations. Player-facing menu vocabulary may present them as Pickup and Drop.

Use report `InventorySummaryLines`, direct validation output, or recorded scenarios to confirm containment behavior. Scenario reports summarize carried contents with inventory coordinates and guard against recursive containment cycles.

## Actor state authoring

Current content-facing actor state:

| State | Authorable now | Use |
|---|---:|---|
| `Facing` | Yes | Initial facing direction; after a successful directional movement, runtime facing updates to the movement direction. |
| `Target` slots | Yes, through targeting rules | Runtime targets used by target-based steps. |

Author initial `Facing` through `actionStateDefaults.facing` or the corresponding editor/API workflow.

Author target selection on entity templates with `targetingRules`. Each rule has a numeric `slot`, recommended stable `label`, optional content-facing `hint`, optional `targetTemplateId`, zero or more `targetCapabilities`, and `range`. Think of the rule as a sentence: the label is the content-defined verb (`loves`), the target template is the noun (`gold`), and target capabilities are adjectives (`portable` via `PickupTarget`, `enterable` via `EnterTarget`, etc.). At the beginning of an entity turn, before its action plan is evaluated, each rule selects the nearest same-plane entity within range that matches the optional target template and all configured capabilities, then writes that entity into both the numeric target slot and the label when present. Target-consuming Action Steps should reference `targetLabel` for normal content; numeric `targetSlot` remains an advanced compatibility escape hatch. If a referenced label has no current target on the executing actor, the step fails/falls through. In frontend/editor workflows, target labels are read as requirements from the selected/default Action Plan; authors normally edit only the template rule's target template, target capabilities, and range for each required label. Template rules whose labels are not referenced by the current default Action Plan are preserved as authored data and should be shown as unused/orphaned rather than deleted automatically.

Do not author arbitrary state variables for new content. Scenario-level per-entity initial `Target` overrides are not part of the current normal authoring surface; use template target rules instead, or track the need through the gap log when rules are insufficient.

## Action plan authoring

Use canonical ordered behavior chains for new action plans.

Current preferred shape:

```yaml
actionPlans:
  examplePlan:
    id: examplePlan
    behavior:
      steps:
        - kind: MoveFacing
        - kind: PickupTarget
```

Authoring model:

- An entity template may assign a default action plan.
- Supported editor/API workflows may create, duplicate, delete unreferenced, and create passive action plans.
- A canonical behavior chain is an ordered list of engine-defined Action Steps.
- Steps are attempted in order.
- A successful turn-consuming step produces the observable action for that root resolution.
- A failed/non-acting step may fall through to the next step, depending on the step.
- Use Action Step order to express simple fallback behavior.
- Target acquisition is not authored as an Action Step for new content. Use entity-template `targetingRules` instead, and set target-consuming Action Step `targetLabel` values to the matching stable labels when a plan needs multiple target concepts.
- Facing changes after successful directional movement; use the promoted canonical `Move` Action Step for new adjacent movement where possible. Existing prototype-compatible movement Action Steps such as `MoveFacing`, `Backstep`, `StrafeClockwise`, and `StrafeAnticlockwise` remain available while migration continues.

Do not use for new normal content:

- arbitrary action-plan variables;
- new `SetVariable` effects;
- metadata-setting Action Steps such as `AcquireNearestTarget`, `TurnLeft`, `TurnRight`, and `ReverseFacing`;
- legacy low-level check/effect construction;
- linked fallback plans as a substitute for ordered canonical steps.

Maintainer/runtime details for canonical, transitional, and legacy action-plan forms live in `docs/Source of Truth/Engine-Editor-Capabilities.md`.

## Currently authorable Action Step catalog

This table is the content-facing catalog of currently authorable behavior-chain Action Steps. Keep rows compact when adding new steps. Put layer details and deep runtime semantics in `Engine-Editor-Capabilities.md` instead.

Planning note: the active canonical-actions release plan freezes the current broad Action Step catalog as legacy/prototype-compatible for release purposes, then promotes actions one at a time through complete vertical slices. Until a step is explicitly promoted under `docs/Plans/Canonical-Actions-Vertical-Slice-Plan.md`, authors may still use the currently supported steps below, but content intended to prove release-canonical behavior should follow the new two-room fixture requirement for the selected action.

### Movement and facing

| Step | Reads | Writes | Author-facing behavior | Common use |
|---|---|---|---|---|
| `Move` | `directionMode`; `Facing` for relative modes | position; post-action `Facing` becomes actual moved direction | Promoted canonical adjacent movement. `directionMode` is required and may be one of the 8 absolute directions or the 8 relative modes. Failed movement preserves position/Facing and does not write `Target`. | ordinary movement, canonical movement rooms, player-controlled movement foundation |
| `MoveFacing` | `Facing` | `Target` when blocked by entity; post-action `Facing` remains movement direction | Move one cell in facing direction; falls through when movement cannot act. | wandering, bump discovery, approach chains |
| `Backstep` | `Facing` | `Target` when blocked by entity; post-action `Facing` becomes movement direction | Move one cell opposite facing; falls through when movement cannot act. | retreat, spacing, obstacle response |

### Inventory and adjacent interaction

| Step | Reads | Writes | Author-facing behavior | Common use |
|---|---|---|---|---|
| `TransformAdjacentToInventory` | `Target` | carried inventory state | Preferred name for pickup semantics: transform the current adjacent target into actor inventory; falls through when pickup cannot act. | collectors, item pickup after bump/acquisition |
| `TransformInventoryToAdjacent` | `Facing` | carried/world placement | Preferred name for adjacent drop semantics: transform the first carried entity into the facing adjacent cell; falls through when drop cannot act. | droppers, stash behavior, inventory demos |
| `PickupTarget` | `Target` | carried inventory state | Compatibility name for `TransformAdjacentToInventory`. | existing/prototype pickup content |
| `DropFacing` | `Facing` | carried/world placement | Compatibility name for `TransformInventoryToAdjacent`. | existing/prototype drop content |
| `GiveTarget` | `Target` | actor/target inventory state | Transfer the first actor-carried entity into target inventory; falls through when target/inventory/space/aperture checks fail. | peer transfer, offering, handoff demos |
| `TakeTarget` | `Target` | target/actor inventory state | Transfer the first target-carried entity into actor inventory; falls through when target contents or actor inventory/space/aperture checks fail. | taking from containers, stealing prototypes |
| `EnterTarget` | `Target` | actor/container inventory state | Enter an adjacent target's inventory at the first open row-major coordinate; falls through when target adjacency, inventory, space, or aperture checks fail. | rooms-inside-entities, containers as spaces |
| `ExitFacing` | `Facing` | actor/container/world placement | Exit the current containing entity toward facing; falls through when not contained, blocked, out of bounds, or aperture checks fail. | leaving entered containers/rooms |
| `PushFacing` | `Facing` | world positions | Push blocking entity one cell in facing direction, then move actor into blocker original cell; consumes the turn on success. | shovers, obstacle interaction |

### Target-relative movement

| Step | Reads | Writes | Author-facing behavior | Common use |
|---|---|---|---|---|
| `SeekTarget` | target label preferred; target slot default `1` | position; post-action `Facing` becomes movement direction | Move one cardinal step that reduces Manhattan distance to target; falls through if target is invalid or no reducing move can act. | chasing, following, collecting |
| `FleeTarget` | target label preferred; target slot default `1` | position; post-action `Facing` becomes movement direction | Move one cardinal step that increases Manhattan distance from target; falls through if no valid escape move can act. | fleeing, avoidance |
| `MaintainChebyshevDistanceTwo` | target label preferred; target slot default `1` | position; post-action `Facing` becomes movement direction | Move toward or away from target to approach Chebyshev distance 2; falls through when already at distance 2 or unable to improve. | kiting, spacing, ranged-position demos |
| `StrafeClockwise` | target label preferred; target slot default `1` | position; post-action `Facing` becomes movement direction | Attempt clockwise perpendicular movement relative to the seek direction toward target. | orbiting, evasive movement |
| `StrafeAnticlockwise` | target label preferred; target slot default `1` | position; post-action `Facing` becomes movement direction | Attempt anticlockwise perpendicular movement relative to the seek direction toward target. | orbiting, evasive movement |

### World mutation / prototype utility

| Step | Reads | Writes | Author-facing behavior | Common use |
|---|---|---|---|---|
| `DestroyTarget` | `Target` | world/entity state | Destroy current target when valid; current first pass rejects self-destruction. | destructive actors, cleanup demos |
| `CreateFacing` | `Facing` | world/entity state | Create a placeholder entity in the facing cell when open. | prototype creation/spawning demos |
| `ApplyPrePlan` | target label preferred or target slot default `1`; `planId` | target action-plan override state | Apply the referenced action plan as the target entity's one-turn pre-plan, replacing any existing pre-plan override; the applying actor's turn is consumed on success. | fear/confusion-style temporary behavior override |
| `ApplyMainPlan` | target label preferred or target slot default `1`; `planId` | target action-plan override state | Apply the referenced action plan as the target entity's one-turn main-plan override, replacing its default main behavior for the next turn. | temporary possession-like/simple behavior replacement |
| `ApplyPostPlan` | target label preferred or target slot default `1`; `planId` | target action-plan override state | Apply the referenced action plan as the target entity's one-turn post-plan, tried after its main plan falls through. | temporary fallback/cleanup behavior |

Common chain patterns:

| Goal | Chain |
|---|---|
| Move and pick up blockers/items | `MoveFacing -> PickupTarget` |
| Chase a selected target | `SeekTarget`, with a template `targetingRules` slot selecting the desired target type |
| Flee a selected target | `FleeTarget`, with a template `targetingRules` slot selecting the desired target type |
| Keep distance before fallback behavior | `MaintainChebyshevDistanceTwo -> StrafeClockwise`, with template `targetingRules` selecting the target |
| Try to move, then push blocker | `MoveFacing -> PushFacing` |
| Make a selected target try a temporary behavior next turn | `ApplyPrePlan`, with `targetSlot` selecting the affected entity and `planId` referencing the one-turn pre-plan |
| Temporarily replace or append selected target behavior next turn | `ApplyMainPlan` or `ApplyPostPlan`, with `targetSlot` selecting the affected entity and `planId` referencing the one-turn override plan |
| Drop carried entity forward, otherwise move | `DropFacing -> MoveFacing` |
| Give to a targeted peer, otherwise try taking from them | `GiveTarget -> TakeTarget` |
| Move into a bumped/targeted container, then later leave it | `MoveFacing -> EnterTarget`; contained actor can use `ExitFacing` |

Targeting rules currently select by optional template ID, target-capability adjectives, same-plane Manhattan range, and nearest deterministic tie-break. A rule may be noun-only (`thief loves gold`), adjective-only (`thief loves portables`), or noun-plus-adjective (`thief loves portable gold`). Supported target capabilities are the Action Steps that expose non-mutating target affordance checks: `PickupTarget`, `EnterTarget`, `GiveTarget`, `TakeTarget`, `DestroyTarget`, and `PushFacing`. Capability rules are validated against the template's default behavior chain: the referenced capability step should exist and consume the same target label/slot. Use stable labels such as `danger`, `home`, `food`, or `shelter` when one entity needs different content-defined target concepts; numeric slots are still stored for compatibility but should not be the primary reference in new action steps.

`GiveTarget` and `TakeTarget` use first-item deterministic selection only. They do not support authorable item filters, barter/trade permissions, or transfer restrictions yet. Runtime reports identify transferred entity ID/name and coordinates; template IDs are not shown because runtime entities do not currently carry template IDs.

`CreateFacing` creates a placeholder entity rather than an authored template-backed spawn. Use it for prototype creation showcases only.

## Scenario authoring

Author scenarios as small compositions of normal content.

Current persisted scenario fields:

| Field | Purpose |
|---|---|
| Scenario name/ID | Stable scenario selection. |
| Scenario root template | Template whose inventory/play plane becomes the scenario space. |
| Player template | Template inserted as the runtime player. |
| Player entity ID | Deterministic runtime ID for the inserted player. |
| Player start coordinate | Requested placement in the scenario-root inventory/play plane. |
| Player controls | Optional authored binding from a player/input ID such as `player-1` to one or more materialized entity IDs. Each authored binding must name at least one existing materialized entity; duplicate entity IDs within one binding and conflicting assignment of one entity to multiple players are invalid. Materialization resolves valid bindings for launch/session consumers; existing player-insertion scenarios default to `player-1` controlling the inserted player when no binding is authored. |

Curated scenario manifests are now first-class content artifacts for scenario browsing and packaging. A manifest may use curated `sections` instead of only a flat `scenarios` cache. Supported section IDs are `legacy`, `delta`, `user`, and `canonical`. Supported entry statuses are `legacy`, `active-delta`, `user`, `canonical-candidate`, and `canonical`. Each entry should include `contentPath`, `scenarioId`, `name`, a required reviewer-facing `description`, `status`, optional `tags`, and optional `source`/provenance. Folder scanning remains useful for reconciliation and candidate discovery, but it is not the authority for curated section membership, ordering, lifecycle, or descriptions.

Use this naming convention for new scenarios: `<section>-<feature-or-action>-<purpose>`, for example `legacy-beta-targeting-acquire-target`, `delta-canonical-move-outcomes`, `delta-canonical-move-player-interaction`, `user-my-room`, or `canonical-main-opening-room`. Descriptions should state the behavior/experience demonstrated, what a reviewer/player should observe, lifecycle/provenance, and caveats or known limitations.

Preferred scenario workflow:

1. Define the vignette goal in terms of observable content behavior.
2. Create or reuse entity templates, presentations, inventories, and action plans.
3. Assign default action plans and initial `Facing` as needed.
4. Create a persisted scenario using scenario root, player template, player entity ID, start coordinate, and player-control bindings when the controlled actor should be explicit.
5. Validate the content document.
6. Materialize and run the scenario.
7. Use SadConsole/manual play when spatial behavior needs visual review; use legacy frames/GIF recording only when an artifact is specifically needed.
8. Add or update the scenario in the curated manifest section (`legacy`, `delta`, `user`, or `canonical`) with description, status, tags, and provenance.
9. Run scenario manifest validation; resolve missing paths/scenario IDs, duplicate scenario IDs, misplaced statuses, missing descriptions, and scanned unclassified candidates.
10. Log gaps for unsupported behavior or insufficient reporting.

Keep scenarios focused. Prefer multiple small vignettes over one scenario that depends on unclear interactions or unsupported filters.

## Validation, simulation, recording, and review

Use the shortest review loop that answers the content question.

| Need | Workflow |
|---|---|
| Check schema/content mistakes | Validate the content document. |
| Inspect one behavior definition | Preview the action plan. |
| Check scenario setup/player insertion | Materialize the persisted scenario. |
| Inspect turn-by-turn behavior | Run a persisted headless scenario report by scenario ID when scenario setup/player insertion matters; use root-only compatibility runs only for scenario-root template isolation. |
| Review a scenario end-to-end | Request a combined persisted scenario review report for document validation, canonical validation, action-plan previews, scenario materialization, run traces, final state, inventory summaries, and diagnostics. |
| Inspect spatial layout over time | Prefer SadConsole/manual play; use legacy scenario frames/GIF only when a shareable artifact is specifically needed. |
| Confirm interactive playability | Launch SadConsole with content file and scenario ID, or use the SadConsole scenario browser. Curated `Manifest.yaml` files may contain sections and entry lifecycle metadata; folder scanning should be used to find candidates/unclassified scenarios rather than to replace curated ordering and descriptions. By default, SadConsole reads `src\GameGameGame.Content\Beta\Manifest.yaml` when present and otherwise discovers scenarios under `src\GameGameGame.Content\Beta`. |

Treat validation diagnostics and runtime observations as content feedback. Expected in-simulation inability to act is not automatically a failed scenario; decide based on the vignette goal.

## Capability gap workflow

Use a capability-gap entry when desired content cannot be expressed cleanly with the current authoring surface in this manual.

Record at least:

- scenario or vignette;
- desired behavior;
- current workaround, if any;
- missing capability;
- scenario count or scenario value unlocked;
- requested priority;
- classification.

Classify gaps as:

- content-only authoring friction;
- reporting/tooling request;
- new Action Step or primitive using existing engine state;
- new engine capability or system;
- content/package organization issue.

The active gap log is `docs/Plans/Beta-Capability-Gap-Log.md`. Keep not-yet-authorable/requested-capability tables there or in a separate referenced gap document, not in this manual. This manual should stay focused on what is currently possible.

Promote a request when repeated scenario pressure, one flagship blocked vignette, hard-to-interpret reports, or repeated authoring friction shows that new support is worth planning.

## External references

- `docs/Source of Truth/Engine-Editor-Capabilities.md`: maintainer-facing support tiers, layer coverage, runtime/editor/API parity, and detailed Action Step semantics.
- `docs/Source of Truth/Action-Step-Outcome-And-Affordance-Logic.md`: compact Action Step success/failure/fallthrough rules and actor/actee/spatial verb-affordance tables.
- `docs/Plans/Beta-Capability-Gap-Log.md`: active beta gaps, not-yet-authorable requested capabilities, workarounds, classifications, and priority signals.
- `docs/Source of Truth/planning-index.md`: documentation lanes and required reading order.
- `docs/Source of Truth/invariants.md`: Core behavior contracts and TDD traces; not normally needed for content authoring.
