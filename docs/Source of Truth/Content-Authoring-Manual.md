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
4. `Canonical Action Step catalog`
5. `Scenario authoring`, when working on scenarios
6. `Capability gap workflow`, when blocked

Default workflow:

1. Author normal content definitions: entity templates, presentations, inventories, action plans, and persisted scenarios.
2. Prefer canonical ordered behavior chains for new behavior.
3. Use currently authorable Action Steps from this manual.
4. Validate after edits.
5. Preview action plans when behavior changes.
6. Materialize, run, or record scenarios when behavior needs inspection.
7. Log a capability gap when desired content cannot be expressed cleanly with the authoring surface listed here.

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
| Documents | Create, open, save, reload, validate, preview content documents, and request combined scenario review reports. |
| Entity templates | Create, edit, duplicate, delete, and reorder templates. |
| Presentations | Assign/edit presentation data used by authored templates. |
| Inventory / containment | Inventory dimensions, weight, carrying capacity, and carried entity layout. |
| Actor state | Initial actor `Facing` through `actionStateDefaults.facing`. |
| Action-plan assignment | Assign or clear an entity template's default action plan. |
| Action plans | Create, edit, delete, reorder, preview, and validate action plans. |
| Canonical behavior chains | Add, remove, and reorder catalog-backed Action Steps. |
| Scenarios | Persist scenario name/root/player template/player entity ID/player start placement. |
| Scenario materialization | Materialize persisted scenarios through shared content/editor services. |
| Scenario execution | Launch persisted scenarios in Console; run persisted scenario reports by scenario ID headlessly with final-state and inventory/containment summaries; request combined validation/preview/materialization/run reports; run root-only compatibility reports when intentionally inspecting a scenario-root template without player insertion. |
| Scenario recording | Record persisted scenarios to PNG frames and GIF artifacts. |
| Gap logging | Record unsupported desired behavior in the active capability gap log. |

For maintainer-facing layer coverage, support tiers, and parity details, use `docs/Source of Truth/Engine-Editor-Capabilities.md`.

## Entity template authoring

Author entity templates as reusable normal content. Current safe operations include:

- create, edit, duplicate, delete, and reorder templates;
- assign presentation data;
- assign or clear a default action plan;
- configure inventory dimensions, weight, and carrying capacity;
- place carried entities in authored inventory layouts;
- set initial actor `Facing` when the template acts through action plans.

Prefer reusable templates over scenario-specific one-off definitions. Scenarios should reference templates rather than encoding special behavior outside normal content structures.

## Inventory and containment authoring

Current inventory authoring supports:

- inventory dimensions;
- entity weight;
- carrying capacity;
- authored carried-entity layout;
- pickup/drop-oriented content using supported Action Steps.

Use constrained inventory behavior where possible:

- `PickupTarget` attempts to place the current `Target` into actor inventory.
- `DropFacing` attempts to drop a carried entity in the actor's facing direction.
- `GiveTarget` transfers the actor's first carried entity into the current `Target` inventory.
- `TakeTarget` transfers the current `Target`'s first carried entity into actor inventory.

Use report `InventorySummaryLines`, direct validation output, or recorded scenarios to confirm containment behavior. Scenario reports summarize carried contents with inventory coordinates and guard against recursive containment cycles.

## Actor state authoring

Current content-facing actor state:

| State | Authorable now | Use |
|---|---:|---|
| `Facing` | Yes | Initial facing direction for facing-based behavior. |
| `Target` | Indirectly through Action Steps | Runtime target used by target-based steps. |

Author initial `Facing` through `actionStateDefaults.facing` or the corresponding editor/API workflow.

Do not author arbitrary state variables for new content. `Target` is used by canonical Action Steps such as `AcquireNearestTarget`, `PickupTarget`, `SeekTarget`, `FleeTarget`, `MaintainChebyshevDistanceTwo`, `StrafeClockwise`, `StrafeAnticlockwise`, `GiveTarget`, and `TakeTarget`. Scenario-level per-entity initial `Target` overrides are not part of the current normal authoring surface; track that need through the gap log when it blocks content.

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
- A canonical behavior chain is an ordered list of engine-defined Action Steps.
- Steps are attempted in order.
- A successful turn-consuming step produces the observable action for that root resolution.
- A failed/non-acting step may fall through to the next step, depending on the step.
- Use Action Step order to express simple fallback behavior.

Do not use for new normal content:

- arbitrary action-plan variables;
- new `SetVariable` effects;
- legacy low-level check/effect construction;
- linked fallback plans as a substitute for ordered canonical steps.

Maintainer/runtime details for canonical, transitional, and legacy action-plan forms live in `docs/Source of Truth/Engine-Editor-Capabilities.md`.

## Canonical Action Step catalog

This table is the content-facing catalog of currently authorable canonical Action Steps. Keep rows compact when adding new steps. Put layer details and deep runtime semantics in `Engine-Editor-Capabilities.md` instead.

### Movement and facing

| Step | Reads | Writes | Author-facing behavior | Common use |
|---|---|---|---|---|
| `MoveFacing` | `Facing` | `Target` when blocked by entity | Move one cell in facing direction; falls through when movement cannot act. | wandering, bump discovery, approach chains |
| `Backstep` | `Facing` | `Target` when blocked by entity | Move one cell opposite facing while preserving facing; falls through when movement cannot act. | retreat, spacing, obstacle response |
| `TurnLeft` | `Facing` | `Facing` | Rotate facing 90 degrees counter-clockwise; consumes the turn on success. | patrols, scanning, simple loops |
| `TurnRight` | `Facing` | `Facing` | Rotate facing 90 degrees clockwise; consumes the turn on success. | patrols, scanning, simple loops |
| `ReverseFacing` | `Facing` | `Facing` | Reverse facing direction; consumes the turn on success. | bounce behavior, patrol reversal |

### Inventory and adjacent interaction

| Step | Reads | Writes | Author-facing behavior | Common use |
|---|---|---|---|---|
| `PickupTarget` | `Target` | carried inventory state | Attempt to pick up current target into actor inventory; falls through when pickup cannot act. | collectors, item pickup after bump/acquisition |
| `DropFacing` | `Facing` | carried/world placement | Drop the first carried entity into the facing cell; falls through when drop cannot act. | droppers, stash behavior, inventory demos |
| `GiveTarget` | `Target` | actor/target inventory state | Transfer the first actor-carried entity into target inventory; falls through when target/inventory/space/capacity checks fail. | peer transfer, offering, handoff demos |
| `TakeTarget` | `Target` | target/actor inventory state | Transfer the first target-carried entity into actor inventory; falls through when target contents or actor inventory/space/capacity checks fail. | taking from containers, stealing prototypes |
| `PushFacing` | `Facing` | world positions | Push blocking entity one cell in facing direction, then move actor into blocker original cell; consumes the turn on success. | shovers, obstacle interaction |

### Target acquisition and target-relative movement

| Step | Reads | Writes | Author-facing behavior | Common use |
|---|---|---|---|---|
| `AcquireNearestTarget` | same-plane positions | `Target` | Select nearest same-plane non-self entity; no authorable filters; continues to next step when possible. | chasers, fleers, collectors, orbiters |
| `SeekTarget` | `Target` | position | Move one cardinal step that reduces Manhattan distance to target; falls through if target is invalid or no reducing move can act. | chasing, following, collecting |
| `FleeTarget` | `Target` | position | Move one cardinal step that increases Manhattan distance from target; falls through if no valid escape move can act. | fleeing, avoidance |
| `MaintainChebyshevDistanceTwo` | `Target` | position | Move toward or away from target to approach Chebyshev distance 2; falls through when already at distance 2 or unable to improve. | kiting, spacing, ranged-position demos |
| `StrafeClockwise` | `Target` | position | Attempt clockwise perpendicular movement relative to the seek direction toward target. | orbiting, evasive movement |
| `StrafeAnticlockwise` | `Target` | position | Attempt anticlockwise perpendicular movement relative to the seek direction toward target. | orbiting, evasive movement |

### World mutation / prototype utility

| Step | Reads | Writes | Author-facing behavior | Common use |
|---|---|---|---|---|
| `DestroyTarget` | `Target` | world/entity state | Destroy current target when valid; current first pass rejects self-destruction. | destructive actors, cleanup demos |
| `CreateFacing` | `Facing` | world/entity state | Create a placeholder entity in the facing cell when open. | prototype creation/spawning demos |

Common chain patterns:

| Goal | Chain |
|---|---|
| Move and pick up blockers/items | `MoveFacing -> PickupTarget` |
| Acquire and chase nearest entity | `AcquireNearestTarget -> SeekTarget` |
| Acquire and flee nearest entity | `AcquireNearestTarget -> FleeTarget` |
| Keep distance before fallback behavior | `AcquireNearestTarget -> MaintainChebyshevDistanceTwo -> StrafeClockwise` |
| Try to move, then push blocker | `MoveFacing -> PushFacing` |
| Drop carried entity forward, otherwise move | `DropFacing -> MoveFacing` |
| Give to a targeted peer, otherwise try taking from them | `GiveTarget -> TakeTarget` |

`AcquireNearestTarget` currently targets any same-plane non-self entity. Use sparse scenario layouts when target filtering matters, and log a capability gap when sparse layout is not sufficient.

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

Preferred scenario workflow:

1. Define the vignette goal in terms of observable content behavior.
2. Create or reuse entity templates, presentations, inventories, and action plans.
3. Assign default action plans and initial `Facing` as needed.
4. Create a persisted scenario using scenario root, player template, player entity ID, and start coordinate.
5. Validate the content document.
6. Materialize and run the scenario.
7. Record frames/GIF when spatial behavior needs visual review.
8. Log gaps for unsupported behavior or insufficient reporting.

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
| Inspect spatial layout over time | Record scenario frames/GIF. |
| Confirm Console playability | Launch Console with content file and scenario ID. |

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
- `docs/Plans/Beta-Capability-Gap-Log.md`: active beta gaps, not-yet-authorable requested capabilities, workarounds, classifications, and priority signals.
- `docs/Source of Truth/planning-index.md`: documentation lanes and required reading order.
- `docs/Source of Truth/invariants.md`: Core behavior contracts and TDD traces; not normally needed for content authoring.
