---
id: source.content-authoring-manual
title: Content Authoring Manual
kind: source-of-truth
subkind: authoring-manual
status: active
owners: [content-editor]
audience: [content-editor, core-owner, frontend-owner]
lane: content-authoring
truth_rank: 30
truth_domains: [authorability, content-workflow, gap-workflow]
read_when:
  - starting any content-authoring or content-review session
  - deciding what can be expressed with current content tools
  - writing beta scenarios primitive showcases or capability-gap notes
do_not_read_when:
  - changing Core behavior or tests
  - checking implementation-layer support tiers
  - researching future/planned capability priority
related:
  - source.planning-index
  - source.engine-editor-capabilities
  - source.capability-gap-log
---
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
- researching future/planned capability priority; use `docs/Source of Truth/Capability-Gap-Log.md` and current planning docs.

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

## Agent tool discovery quick start

Fresh agent sessions should use the local `ggg-content` MCP server and its `ggg_content_*` tools instead of guessing YAML shapes. The tool server is configured in `.opencode/opencode.json` and exposes a discoverable catalog through normal MCP `tools/list`.

Recommended first calls:

1. `ggg_content_get_authoring_guide` for the start-here workflow, source-of-truth doc paths, current authoring surface, and safety rules.
2. `ggg_content_list_workflows` for machine-readable recipes such as open/review, behavior-plan editing, scenario run review, manifest maintenance, and the safe save loop.
3. `ggg_content_describe_schema` when an input object is not obvious. Supported concepts include `entityTemplateUpdate`, `scenario`, `coord`, `behaviorStep`, and `cost`; the response lists fields, enum values, clear/null semantics, and examples.
4. `ggg_content_list_examples` for useful content/example locations to inspect before authoring.
5. `ggg_content_list_action_steps` after opening/creating a session when choosing behavior-chain Action Step kinds and supported authored fields.

Keep the normal safe save loop: validate, canonical-validate, review `ggg_content_snapshot` diff/dirty state, then call `ggg_content_save` or `ggg_content_save_as` deliberately. Discovery/list/preview tools are read-only; mutation tools should preserve the `{ ok, data, error, summary }` response envelope and session-based workflow.

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
| Inventory / containment | Inventory dimensions, bulk, aperture, carried entity layout, nullable inventory-boundary policies, experimental topology policies, and first-slice merged inventory layer joins. |
| Actor state | Initial actor `Facing` through `actionStateDefaults.facing`; target-selection rules through preferred `targeting` profiles or legacy `targetingRules`. |
| Action-plan assignment | Assign or clear an entity template's default action plan. |
| Action plans | Create, edit, delete, reorder, preview, and validate action plans. |
| Canonical behavior chains | Add, remove, reorder, preview, and validate catalog-backed Action Steps, including current promoted canonical steps and prototype-compatible experiment steps. |
| Scenarios | Persist scenario name/root/player template/player entity ID/player start placement, plus first-slice authored player-control bindings from player/input IDs to materialized entity IDs. |
| Scenario materialization | Materialize persisted scenarios through shared content/editor services. |
| Playable session launch | Create frontend-neutral playable sessions from persisted scenarios or catalog entries through the shared Content launcher, including world, registry/presentation lookup, action plans, player entity, active plane/container, diagnostics, runtime failures, and capability gaps. |
| Scenario execution | Launch persisted scenarios in the official SadConsole frontend directly by content file/scenario ID or through the SadConsole scenario browser populated from one file, folder discovery, or a curated manifest. Scenario catalog candidate scanning lives in Content through `ScenarioCatalogScanService`/`ScenarioCatalog.ScanCandidates`, but curated manifests are the author-facing source for sections, ordering, descriptions, status/lifecycle, tags, provenance, and browsing intent. Run persisted scenario reports by scenario ID with final-state and inventory/containment summaries; request compact player narrative projection message IDs/args through `ggg_content_run_scenario_player_log_by_id`; request combined validation/preview/materialization/run reports; run root-only compatibility reports when intentionally inspecting a scenario-root template without player insertion. |
| Scenario recording | Legacy fallback: record persisted scenarios to PNG frames and GIF artifacts when reports/SadConsole are insufficient. Future visual recording should prefer history playback / SadConsole-rendered export. |
| Ecology experiments | Author spatial individual-based ecology vignettes with existing target rules, pickup/drop/transfer, template-backed creation, polymorph, destroy, and action-step costs; cooldowns, age timers, density gates, and structured ecological time-series remain gaps. |
| Gap logging | Record unsupported desired behavior in the active capability gap log. |

For maintainer-facing layer coverage, support tiers, and parity details, use `docs/Source of Truth/Engine-Editor-Capabilities.md`.

## Entity template authoring

Author entity templates as reusable normal content. Current safe operations include:

- create, edit, duplicate, delete, and reorder templates;
- assign presentation data;
- assign presentation-only material metadata (`metal`, `wood`, `stone`, or undefined);
- assign or clear a default action plan;
- configure inventory dimensions, bulk, and aperture;
- place, remove, move, replace, and overwrite carried entities in authored inventory layouts through supported editor/API workflows;
- set initial actor `Facing` when the template acts through action plans;
- configure target-selection rules for target-consuming Action Steps.

Prefer reusable templates over scenario-specific one-off definitions. Scenarios should reference templates rather than encoding special behavior outside normal content structures.

Template `material` is presentation-only in the current MVP. Valid explicit YAML values are `metal`, `wood`, and `stone`; omit or clear `material` to leave it undefined. SadConsole uses an entity's material when rendering that entity's inventory cells: undefined uses `gridDotted` as a debug/fallback backdrop, `metal` uses `gridMetal`, `stone` uses `gridCave`, and `wood` uses `gridWood`. Material does not affect inventory rules, action legality, targeting, bulk, aperture, or any other mechanics.

## Inventory and containment authoring

Current inventory authoring supports:

- inventory dimensions;
- entity bulk;
- aperture;
- nullable `enterPolicy` and `exitPolicy` on entity templates, with missing values defaulting to `FirstUnoccupiedRowMajor` and `AnyCell` respectively;
- authored carried-entity layout, including placement, removal, movement, template replacement, and coordinate overwrite through supported editor/API workflows;
- pickup/drop-oriented content using supported Action Steps.

Use constrained inventory behavior where possible:

- `TransformAdjacentToInventory` attempts to place the current adjacent `Target` into actor inventory. `PickupTarget` remains a compatibility name for the same semantics.
- `TransformInventoryToAdjacent` attempts to drop a carried entity in the actor's facing direction. `DropFacing` remains a compatibility name for the same semantics.
- `Transfer` is the preferred canonical peer-inventory transfer step. It moves a selected concrete entity between the actor and an adjacent counterparty using `transferDirection: ActorToTarget` or `TargetToActor`.
- `GiveTarget` transfers the actor's first carried entity into the current `Target` inventory.
- `TakeTarget` transfers the current `Target`'s first carried entity into actor inventory.
- `EnterTarget` moves the actor into the adjacent current `Target` inventory.
- `ExitFacing` moves the actor out of its current containing entity toward `Facing`.

Bulk/Aperture checks and inventory-boundary policies apply to every inventory boundary crossed by these constrained behaviors. Pickup and Drop additionally require the actor's bulk to be greater than the picked/dropped entity's aperture; if the actor can enter an entity, that same entity is not portable by that actor. `EnterPolicy` controls placement into the destination inventory for Pickup, Give, Take, Enter, and future constrained transforms. `ExitPolicy` controls egress out of a source inventory for Drop, Exit, and peer transfers that leave an inventory. For nested interiors, entering from inside one entity into another crosses both the source containing entity's aperture and the destination entity's aperture; exiting crosses the current containing entity's aperture. This is intentional: use larger apertures for interiors meant to allow passage, or use `Teleport` for exceptional movement that should bypass aperture and policy rules.

Supported `enterPolicy` values are `FirstUnoccupiedRowMajor` and `FarthestFromOccupied`; the latter breaks ties row-major, left-to-right/top-to-bottom. Supported `exitPolicy` values are `AnyCell` and `EdgeAlignedWithExitDirection`; the latter requires the carried/source coordinate to be on the edge or corner matching the selected exit direction.

Experimental `topologyPolicy` values are `None`, `ConnectsInward`, `ConnectsOutward`, and `ConnectsInwardAndOutward`. `ConnectsOutward` makes matching edge/corner cells in the entity's inventory topologically adjacent to the exterior cell in the same direction from the inventory owner. `ConnectsInward` makes exterior cells adjacent to the owner lead inward to preferred inventory edge cells; cardinal inward links choose the second-from-left cell on north/south edges and the second-from-top cell on west/east edges when available, while intercardinal links choose inventory corners. Directed topology policy affects normal movement, player movement choices/affordances, and adjacency-based interactions such as pickup. It is intentionally separate from `enterPolicy`/`exitPolicy`, which still govern explicit constrained Enter/Exit-style inventory transitions.

Experimental merged inventory layer `joins` can connect separated contributed inventory spaces by source cell instead of by overlapping layout coordinates. The currently supported YAML shape is `joins: [{ from: { owner: roomA, edge: East }, to: { owner: hallAB, edge: West }, align: Center }]`; edges must be cardinal and `Center` selects the center cell of each referenced owner edge, such as a 3x3 room east-middle cell to a 5x1 hallway west endpoint. Validation rejects conflicting links that give the same source cell/direction two destinations. Editable documents, the shared editor service, agent API, and frontend editor snapshots preserve and list this shape. Movement can cross these explicit seams cardinally and can also cross them as part of an intercardinal move composed from one local cardinal step plus one cardinal source-cell-link seam step, matching the normal diagonal rule that only two occupied orthogonal corners block the diagonal. Current examples: `src/GameGameGame.Content/Beta/Topology/TopologyPolicyShowcase.yaml`, `src/GameGameGame.Content/Beta/Topology/RoomHallAlignedJoinShowcase.yaml`, and `src/GameGameGame.Content/Beta/Topology/MergedInventoryLayerShowcase.yaml`. Merged-layer `spaces[].origin` values are projection/layout metadata only: contributor rooms remain internally walkable, but cross-contributor movement, POV, pathing, and adjacency require explicit joins/source-cell links. Do not rely on accidental touching or overlapping layout projection as authored semantics; explicit overlap/fold vocabulary, non-center joins, asymmetric joins, and topology-aware targeting distance are active follow-up topics on the rolling boards.

For player-controlled actors, the shared Core Action Choice seam can expose authored `TransformAdjacentToInventory`/`PickupTarget` as selectable adjacent pickup targets plus inventory-slot destinations, authored `TransformInventoryToAdjacent`/`DropFacing` as selectable carried sources plus adjacent map destinations, authored `EnterTarget` as Enter target choices, authored `ExitFacing` as Exit direction choices, and authored `Transfer` as a counterparty-then-item workflow over the actor and counterparty inventories. Player-facing menu vocabulary may present them as Pickup, Drop, Enter, Exit, and Transfer/Give/Take as appropriate.

Use report `InventorySummaryLines`, direct validation output, or recorded scenarios to confirm containment behavior. Scenario reports summarize carried contents with inventory coordinates and guard against recursive containment cycles.

## Actor state authoring

Current content-facing actor state:

| State | Authorable now | Use |
|---|---:|---|
| `Facing` | Yes | Initial facing direction; after a successful directional movement, runtime facing updates to the movement direction. |
| `Target` slots | Yes, through targeting rules | Runtime targets used by target-based steps. |

Author initial `Facing` through `actionStateDefaults.facing` or the corresponding editor/API workflow.

Author target selection on entity templates with the preferred `targeting` profile or legacy `targetingRules`. A `targeting` profile has one shared `range` for the actor, optional `defaultLocality`, and a set of rules; each rule has a numeric `slot`, recommended stable `label`, optional content-facing `hint`, optional `targetTemplateId`, zero or more `targetCapabilities`, and optional `locality.origins` such as `CurrentPlace`, `OwnInventory`, or `PeerInventories`. Think of the rule as a sentence: the label is the content-defined verb (`loves`), the target template is the noun (`gold`), target capabilities are adjectives (`portable` via `PickupTarget`, `enterable` via `EnterTarget`, etc.), and locality says where the actor looks. At the beginning of an entity turn, before its action plan is evaluated, each rule selects the nearest candidate within the actor's targeting range that matches the optional template and all configured capabilities, then writes that entity into both the numeric target slot and the label when present. Shared candidate preview can report matching candidates and provenance without mutating target slots. Legacy `targetingRules[].range` remains loadable as compatibility data. Target-consuming Action Steps should reference `targetLabel` for normal content, `targetSelf: true` when the actor is intentionally its own target, and numeric `targetSlot` only as an advanced compatibility escape hatch. If a referenced label has no current target on the executing actor, the step fails/falls through. In frontend/editor workflows, target labels are read as requirements from the selected/default Action Plan; authors normally edit only the template rule's target template, target capabilities, locality, and shared range for each required label. Template rules whose labels are not referenced by the current default Action Plan are preserved as authored data and should be shown as unused/orphaned rather than deleted automatically.

Current targeting limits matter for merged-space and ecology experiments: rules select one nearest candidate, not a set; ordering still uses current same-plane coordinate distance rather than the upcoming topology-aware targeting distance; and there are no authorable count/density predicates. Use layout, costs, inventory limits, or separate templates as workarounds, and log a gap when a scenario needs local population counts, crowding, morale, or graph-distance target selection.

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
        - kind: Move
          directionMode: Forward
        - kind: TransformAdjacentToInventory
```

Authoring model:

- An entity template may assign a default action plan.
- Supported editor/API workflows may create, duplicate, delete unreferenced, and create passive action plans.
- A canonical behavior chain is an ordered list of engine-defined Action Steps.
- Steps are attempted in order.
- A successful turn-consuming step produces the observable action for that root resolution.
- A failed/non-acting step may fall through to the next step, depending on the step.
- Use Action Step order to express simple fallback behavior.
- Target acquisition is not authored as an Action Step for new content. Use entity-template `targeting` profiles instead, and set target-consuming Action Step `targetLabel` values to the matching stable labels when a plan needs multiple target concepts.
- Facing changes after successful directional movement; use the promoted canonical `Move` Action Step for new adjacent movement in canon-promoted content. Existing movement helpers such as `MoveFacing` and `Backstep` remain prototype-compatible for experiments/old fixtures, while retired coordinate target-movement helpers such as `SeekTarget`, `FleeTarget`, `MaintainChebyshevDistanceTwo`, `StrafeClockwise`, and `StrafeAnticlockwise` should be replaced by `TargetPathMove` in new graph-first content.

Do not use for new normal content:

- arbitrary action-plan variables;
- new `SetVariable` effects;
- metadata-setting Action Steps such as `AcquireNearestTarget`, `TurnLeft`, `TurnRight`, and `ReverseFacing`;
- legacy low-level check/effect construction;
- linked fallback plans as a substitute for ordered canonical steps.

Maintainer/runtime details for canonical, transitional, and legacy action-plan forms live in `docs/Source of Truth/Engine-Editor-Capabilities.md`.

## Currently authorable Action Step catalog

This table is the content-facing catalog of currently authorable behavior-chain Action Steps. Keep rows compact when adding new steps. Put layer details and deep runtime semantics in `Engine-Editor-Capabilities.md` and outcome/fallthrough details in `Action-Step-Outcome-And-Affordance-Logic.md`.

Status vocabulary:

- **Promoted canonical:** preferred for release-facing/canon-promoted content and player-facing action semantics.
- **Prototype-compatible:** implemented in Core/YAML/validation/editor/API and usable for Beta/Delta experiments, but not yet a release-canonical authoring baseline.
- **Legacy/retired:** loadable or historical compatibility only; do not use in new normal content unless explicitly maintaining old fixtures.

### Canon-promoted content allowlist

Content under `src/GameGameGame.Content/Canonical` and other canon-promoted fixtures must use this allowlist. Prefer the canonical row even when a compatibility alias currently has identical runtime semantics.

| Player-facing action/need | Canon-promoted Action Step(s) to author | Forbidden in canon-promoted content | Notes |
|---|---|---|---|
| Move | `Move` with explicit `directionMode` | `MoveFacing`, `Backstep`, retired target-relative helpers | Promoted canonical adjacent movement. |
| Pickup | `TransformAdjacentToInventory` | `PickupTarget` | `PickupTarget` is compatibility naming for the same pickup semantics. |
| Drop | `TransformInventoryToAdjacent` | `DropFacing` | `DropFacing` is compatibility naming for the same adjacent-drop semantics. |
| Give | `Transfer` with `transferDirection: ActorToTarget` | `GiveTarget` | Canonical transfer selects a concrete moving entity and adjacent counterparty. |
| Take | `Transfer` with `transferDirection: TargetToActor` | `TakeTarget` | Canonical transfer selects a concrete moving entity from the counterparty. |
| Enter | `EnterTarget` | none yet | Current promoted enter command has no renamed replacement yet. |
| Exit | `ExitFacing` | none yet | Current promoted exit command has no renamed replacement yet. |
| Push | `Push` with explicit `directionMode` | `PushFacing` | Canonical Push moves only the target. `PushFacing` is the older bump/facing shortcut. |
| Lifecycle/state alteration | `CreateEntity`, `PolymorphTarget`, `DestroyTarget` | `CreateFacing` | Canonical for computer-controlled lifecycle/state management. Player-facing workflows are not polished yet; use mostly for autonomous actors unless a scenario explicitly accepts rough interaction. |

If canon-promoted content appears to need a forbidden step, first try to express it through the allowed row. If the allowed row cannot express the scenario, log a capability gap instead of adding new canonical content that depends on the forbidden step.

### Promoted canonical steps

| Step | Reads | Writes | Author-facing behavior | Examples / notes |
|---|---|---|---|---|
| `Move` | `directionMode`; `Facing` for relative modes | position; post-action `Facing` becomes actual moved direction | Resolves an absolute or relative 8-way direction and moves one adjacent step when legal/open. Failed movement preserves position/Facing and does not write `Target`. | Use for ordinary movement and canon-promoted movement rooms. |
| `TransformAdjacentToInventory` | `Target` | carried inventory state | Preferred pickup name. Moves the current adjacent target into actor inventory using row-major destination selection; falls through when target/inventory/space/aperture checks fail. | `PickupTarget` remains compatibility naming. |
| `TransformInventoryToAdjacent` | `Facing` | carried/world placement | Preferred drop name. Moves the first carried entity into the adjacent facing destination; falls through when no carried entity or destination/policy/aperture checks fail. | `DropFacing` remains compatibility naming. |
| `Transfer` | moving entity target label/slot; adjacent counterparty direction; `transferDirection` | actor/counterparty inventory state | Atomically transfers a selected concrete entity between actor and adjacent counterparty. `ActorToTarget` checks counterparty enter policy; `TargetToActor` checks counterparty/source exit policy. | Use for give/take/handoff. Does not yet support authoring an inventory-internal predicate such as “first potion in chest”; target the concrete moving entity where possible. |
| `EnterTarget` | `Target` | actor/container inventory state | Enters an adjacent target inventory at the coordinate selected by target `enterPolicy`; falls through on missing/non-adjacent/non-enterable/full/aperture failure. | See topology examples under `src/GameGameGame.Content/Beta/Topology/`. |
| `ExitFacing` | `Facing` | actor/container/world placement | Exits the current containing entity toward `Facing`, respecting source `exitPolicy` and aperture. | Usually authored on actors that may already be contained. |
| `Push` | `Target`; `directionMode` | target position | Forces an adjacent selected target to move one adjacent step in target-relative `directionMode` when the target bulk fits actor aperture and the destination is legal/open. The actor does not move. | Use instead of `PushFacing` for new player-facing push semantics. |
| `DestroyTarget` | `Target` | world/entity state | Recursively destroys the current target and its inventory descendants; current behavior rejects self-destruction. | Canonical lifecycle/state alteration for autonomous actors; player-facing workflow polish remains follow-up. Direct destroy can be opaque for ecology; prefer material pickup-plus-cost conversion when modeling consumption. |
| `CreateEntity` | `templateId`; `createPlacement`; `directionMode` when placement is `Facing` | world/entity state | Creates a runtime entity from an authored template. Default placement is first open adjacent cell; `createPlacement: Facing` uses resolved `directionMode`. New entities receive template name, inventory dimensions, bulk, aperture, policies, topology policy, presentation-only material, default action plan, initial facing, runtime template identity for content/presentation lookup, and inventory plane when applicable. | Canonical lifecycle/spawning for autonomous actors; player-facing workflow polish remains follow-up. Template-backed spawning/reproduction. See lifecycle and ecology examples. |
| `PolymorphTarget` | `Target` or `targetSelf`; `templateId` | target entity data/default plan | Changes the selected entity to another authored template while preserving runtime entity ID, current location, current inventory contents, and existing facing. It applies the new template name/bulk/aperture/policies/topology policy/material/default action plan/template identity. | Canonical lifecycle/state alteration for autonomous actors; player-facing workflow polish remains follow-up. Lifecycle phases such as egg -> caterpillar -> cocoon -> butterfly in `CreateDestroyPolymorphShowcase.yaml`. |

### Prototype-compatible steps for experiments and existing Beta/Delta content

| Step | Reads | Writes | Author-facing behavior | Examples / notes |
|---|---|---|---|---|
| `MoveFacing` | `Facing` | position; `Target` when blocked by entity | Moves one cell in `Facing`; when blocked by an entity, writes that blocker to `Target` and falls through. | Useful for legacy bump-discovery chains, but prefer `Move` for canon-promoted content. |
| `Backstep` | `Facing` | position; `Target` when blocked by entity | Moves opposite `Facing` while preserving original `Facing`; falls through on failure. | Retreat/spacing experiments. |
| `PickupTarget` | `Target` | carried inventory state | Compatibility name for `TransformAdjacentToInventory`. | Existing Beta content may still use it. |
| `DropFacing` | `Facing` | carried/world placement | Compatibility name for `TransformInventoryToAdjacent`. | Existing Beta content may still use it. |
| `GiveTarget` | `Target` | actor/target inventory state | Transfers the actor's first carried entity to the current target inventory using deterministic row-major selection. | Prefer `Transfer` when authoring new canonical peer transfer. |
| `TakeTarget` | `Target` | target/actor inventory state | Transfers the target's first carried entity into actor inventory using deterministic row-major selection. | Used by prototype theft/economy scenarios; prefer `Transfer` when the moving entity can be selected explicitly. |
| `TargetPathMove` | `Target`; `pathMode`; `desiredDistance` for MaintainDistance/Orbit; `orbitDirection` for Orbit | position; post-action `Facing` becomes actual moved direction | Graph-native target-relative movement. `SeekAdjacency` and `FleeAdjacency` move relative to target adjacency; `MaintainDistance` and `Orbit` use authored integer distance bands. Can cross source-cell-link topology. | Used in `src/GameGameGame.Content/Beta/Ecology/EcologyVignettes.yaml` and `src/GameGameGame.Content/Beta/User/RatBarnScenario.yaml`. Orbit still uses coordinate projection geometry for angular ordering. |
| `ApplyPrePlan` / `ApplyMainPlan` / `ApplyPostPlan` | target label/slot or `targetSelf`; `planId` | target one-turn action-plan override | Installs the referenced plan as the target's one-turn pre/main/post override, replacing any existing override in that slot; the applying actor consumes its turn on success. | Temporary fear/confusion/possession-style experiments. |

### Legacy/retired compatibility only

Do not use these in new normal content. They may remain in old fixtures or tests while migration continues.

| Step | Status | Replacement / guidance |
|---|---|---|
| `CreateFacing` | Prototype placeholder spawner; legacy for new spawning | Creates an untemplated placeholder-like entity in the facing cell. Prefer template-backed `CreateEntity`. `GAP-001` remains relevant only to this placeholder path. |
| `PushFacing` | Compatibility push shortcut | Pushes the facing blocker and moves the actor into the blocker original cell. Prefer canonical `Push`, which moves only the target. |
| `AcquireNearestTarget` | Retired metadata-setting targeting step for new authoring | Use template `targeting` profiles/rules with stable labels. |
| `SeekTarget`, `FleeTarget`, `MaintainChebyshevDistanceTwo`, `StrafeClockwise`, `StrafeAnticlockwise` | Retired coordinate target-movement helpers for new graph-first authoring | Use `TargetPathMove` with `pathMode` instead. Existing old showcase files may remain compatibility references. |
| `TurnLeft`, `TurnRight`, `ReverseFacing` | Legacy metadata-only facing steps | Prefer movement/facing consequences or explicit content scenarios that do not require turn-only facing mutation. |
| Legacy low-level checks/effects and linked primitive/fallback plans | Compatibility shape | Prefer ordered `behavior.steps` chains. |

### Action-step costs

Behavior steps may include optional `costs` entries, each with `templateId` and positive `quantity`. Costs are paid from the actor's carried inventory recursively by runtime template ID. If any required quantity is missing, the step fails/falls through with a missing-cost reason and no cost is consumed. When the step itself succeeds, the selected cost entities are recursively destroyed. Duplicate cost template IDs are invalid; combine quantities into one entry.

Common uses:

```yaml
- kind: CreateEntity
  templateId: glowcapGrub
  createPlacement: AdjacentOpen
  costs:
  - templateId: glowcapSpore
    quantity: 2
```

Costs are useful for ecology/economy loops such as `spores -> grub`, `grub -> guano`, or `coin -> hired bruiser` in `src/GameGameGame.Content/Beta/Ecology/EcologyVignettes.yaml`. They are not cooldowns, timers, hunger, durability, or barter permissions; use the gap workflow for those needs.

Common chain patterns:

| Goal | Chain |
|---|---|
| Move and pick up blockers/items | `Move -> TransformAdjacentToInventory` |
| Move along graph topology toward/away from a selected target | `TargetPathMove` with `pathMode: SeekAdjacency` or `FleeAdjacency`, plus template targeting rule for the label |
| Maintain or orbit a distance band around a selected target | `TargetPathMove` with `pathMode: MaintainDistance` or `Orbit`, `desiredDistance`, and `orbitDirection` where required |
| Try to push a selected adjacent target | `Push` with explicit `directionMode`, plus a targeting rule selecting a pushable target |
| Template-backed reproduction/spawning | `CreateEntity` with `templateId`, optional `createPlacement`, and optional `costs` |
| Lifecycle transition | `PolymorphTarget` with `targetSelf: true` or a selected target label |
| Material consumption/conversion | `PickupTarget` / `TransformAdjacentToInventory` before costed `CreateEntity`, `DestroyTarget`, or `PolymorphTarget` |
| Make a selected target try a temporary behavior next turn | `ApplyPrePlan`, with `targetLabel` and `planId` referencing the one-turn pre-plan |
| Temporarily replace or append selected target behavior next turn | `ApplyMainPlan` or `ApplyPostPlan`, with `targetLabel` and `planId` |
| Drop carried entity forward, otherwise move | `TransformInventoryToAdjacent -> Move` |
| Transfer a specific targeted item to or from an adjacent counterparty | `Transfer`, with `targetLabel`/`targetSlot`, `directionMode`, and `transferDirection` |
| Move into a targeted container, then later leave it | `Move -> EnterTarget`; contained actor can use `ExitFacing` |

Targeting rules currently select by optional template ID, target-capability adjectives, same-plane coordinate range, and nearest deterministic tie-break. A rule may be noun-only (`thief loves gold`), adjective-only (`thief loves portables`), or noun-plus-adjective (`thief loves portable gold`). Supported target capabilities are the Action Steps that expose non-mutating target affordance checks: `TransformAdjacentToInventory`/`PickupTarget`, `EnterTarget`, `GiveTarget`, `TakeTarget`, `DestroyTarget`, and canonical `Push`; `PushFacing` remains compatibility-only for older bump-push content. Capability rules are validated against the template's default behavior chain: the referenced capability step should exist and consume the same target label/slot. Use stable labels such as `danger`, `home`, `food`, or `shelter` when one entity needs different content-defined target concepts; numeric slots are still stored for compatibility but should not be the primary reference in new action steps.

`Transfer` currently selects a concrete moving entity through the actor's target label/slot state. It does not yet support authoring an item predicate such as "first potion in this inventory"; use targeting rules when a target can select the moving entity directly, or log a capability gap when inventory-internal item matching is needed. Legacy `GiveTarget` and `TakeTarget` use first-item deterministic selection only. None of the current peer-transfer steps support barter/trade permissions or transfer restrictions yet.

## Scenario authoring

Author scenarios as small compositions of normal content.

For new composed/exhibit scenarios, treat the scenario root as a world/level container rather than a gameplay room. Players and ordinary interactable objects should normally be placed inside room-like child entities contained by the root, with playable actors authored as placed instances using `controller: Player`. Use direct placement in the scenario-root plane only for root-only compatibility tests, small engine/editor fixtures, intentionally container-like scenarios, or legacy content awaiting migration.

Current persisted scenario fields:

| Field | Purpose |
|---|---|
| Scenario name/ID | Stable scenario selection. |
| Scenario root template | Template whose inventory/play plane becomes the scenario space. |
| Placed entity controller | Preferred playable-start authoring model. Any entity instance placed in an authored inventory layout may declare nullable `controller: Player` or `controller: Computer`; missing/null defaults to `Computer`. During scenario materialization, placed instances with `controller: Player` initialize to runtime `PlayerChoice`, including nested instances and multiple entities. SadConsole play mode advances initiative and prompts each `PlayerChoice` actor when its turn arrives; headless runs report pending player prompts rather than auto-resolving them. |
| Player template | Legacy fallback template inserted as the runtime player only when no placed instance declares `controller: Player` and the legacy player template/entity/start tuple is complete. |
| Player entity ID | Legacy deterministic runtime ID for the inserted player, and observer/default focus metadata where applicable; not the source of control authority when placed controllers exist. |
| Player start coordinate | Nullable legacy fallback placement in the scenario-root inventory/play plane. Missing start plus no placed `controller: Player` produces a playerless scenario rather than implicit `(0,0)` insertion. |
| Player controls | Compatibility binding from a player/input ID such as `player-1` to one or more materialized entity IDs. Prefer placed-instance `controller` for new content. Materialization still resolves valid legacy bindings for launch/session consumers; existing player-insertion scenarios default to `player-1` controlling the inserted player when no placed controller or explicit binding is authored. |

Curated scenario manifests are now first-class content artifacts for scenario browsing and packaging. A manifest may use curated `sections` instead of only a flat `scenarios` cache. Supported section IDs are `legacy`, `delta`, `user`, and `canonical`. Supported entry statuses are `legacy`, `active-delta`, `user`, `canonical-candidate`, and `canonical`. Each entry should include `contentPath`, `scenarioId`, `name`, a required reviewer-facing `description`, `status`, optional `tags`, and optional `source`/provenance. Folder scanning remains useful for reconciliation and candidate discovery, but it is not the authority for curated section membership, ordering, lifecycle, or descriptions.

Use this naming convention for new scenarios: `<section>-<feature-or-action>-<purpose>`, for example `legacy-beta-targeting-acquire-target`, `delta-canonical-move-outcomes`, `delta-canonical-move-player-interaction`, `user-my-room`, or `canonical-main-opening-room`. Descriptions should state the behavior/experience demonstrated, what a reviewer/player should observe, lifecycle/provenance, and caveats or known limitations.

Preferred scenario workflow:

1. Define the vignette goal in terms of observable content behavior.
2. Create or reuse entity templates, presentations, inventories, and action plans.
3. Assign default action plans and initial `Facing` as needed.
4. Mark controlled placed entity instances with `controller: Player` when the controlled actor should be explicit. Use legacy player template/entity/start only for compatibility scenarios that still insert a player into the root plane.
   - For new exploratory or exhibit-style scenarios, include an explicit placed player/observer entity with a basic action loadout unless the scenario is intentionally playerless. The current standard loadout is a reusable player/observer template with `controller: Player`, initial `Facing`, and a default action plan ordered as `Move` Forward, `PickupTarget`, `DropFacing`, then `Transfer` `TargetToActor` Forward. This keeps scenarios manually inspectable and provides a consistent baseline for player interaction while AI actors demonstrate the authored behavior.
5. Validate the content document.
6. Materialize and run the scenario.
7. Use SadConsole/manual play when spatial behavior needs visual review; use legacy frames/GIF recording only when an artifact is specifically needed.
8. Add or update the scenario in the curated manifest section (`legacy`, `delta`, `user`, or `canonical`) with description, status, tags, and provenance.
9. Run scenario manifest validation; resolve missing paths/scenario IDs, duplicate scenario IDs, misplaced statuses, missing descriptions, and scanned unclassified candidates.
10. Log gaps for unsupported behavior or insufficient reporting.

Keep scenarios focused. Prefer multiple small vignettes over one scenario that depends on unclear interactions or unsupported filters.

## Ecology authoring notes

Status: Early content-authoring practice from the Pocket Bazaar ecology vignette testbed. These notes are not invariants; treat them as practical knobs to try when authoring isolated ecologies with the current Action Step surface.

Ecology vignettes should be understood as spatial, individual-based systems. Track what each entity can produce, consume, carry, convert, and remove per turn. Prefer material flows such as `resource -> carrier inventory -> costed lifecycle action` over direct deletion when modeling eating, predation, nutrient recycling, or economic spending.

### Currently modellable ecology knobs

1. **Reproduction requiring multiple food units.**
   - Author by giving the reproducing entity enough inventory/carrying capacity and adding multiple resource costs to its `CreateEntity` or lifecycle action.
   - Example: the cave test changed grub reproduction from `1 glowcapSpore -> 1 glowcapGrub` to `2 glowcapSpore -> 1 glowcapGrub` by increasing grub capacity to 2 and setting the reproduction cost quantity to 2.
   - Finding: this is a strong population-control knob. It slowed grub growth and made foraging more visible, but with two bats the grubs tended toward extinction and with one bat they could still overrun. Food cost controls rate; it does not by itself solve indefinite parent survival.

2. **Handling time / lifecycle processing delay.**
   - Author by ordering `PickupTarget` before a costed output action, so an entity must spend at least one action collecting the resource/prey and another action converting it.
   - Example: bats should prefer `PickupTarget prey -> CreateEntity guano` with `glowcapGrub` as the cost, rather than `DestroyTarget prey`.
   - Finding: replacing direct bat destruction with pickup-plus-costed guano made the cave much more legible and prevented immediate grub extinction. This pattern creates explicit handled states such as “bat carrying grub” and exposes placement/inventory constraints as real ecological pressure.

3. **Waste or incomplete conversion.**
   - Author by making costs exceed outputs, e.g. `2 spores -> 1 grub`, `2 grubs -> 1 guano`, or `2 guano -> 1 spore`.
   - Finding: the current `2 spores -> 1 grub` cave test shows that lossy conversion can prevent instant bloom, but it is a very sensitive knob. Use it to dampen runaway growth, and expect to retune predator count, starting population, or input rate afterward.

4. **Predator or regulator reproduction tied to consumption.**
   - Author structurally with the same pattern as other costed creation: a predator carries prey/resources, then spends them to `CreateEntity` another predator/regulator or helper.
   - Related evidence: the Goblin Coin Table uses `Coin -> Hired Bruiser` as an economic analogue. It proved that costed creation is easy to author, but also that unchecked regulator/helper creation can dominate the scenario and clog space.
   - Finding: consumption-tied predator reproduction is authorable, but should usually be paired with a limiting factor such as high cost, limited input, small inventory, lossy conversion, or future density/cooldown support.

5. **Limited lifespan or staged lifecycle chains, approximately.**
   - Author with deterministic lifecycle phases using `PolymorphTarget`, optionally combined with costed actions. Existing lifecycle showcases demonstrate chains such as egg/silkworm/cocoon/moth.
   - Finding: this can model phase changes, one-shot reproductive states, or “spent” forms, but it is not a true age or timed lifespan. Without turn counters or duration controls, each phase tends to resolve on the entity's next successful action unless blocked by costs, movement, or inventory constraints.

### Current ecology authoring lessons

- Treat production, conversion, and removal as throughput limits. Count the maximum per-turn rates: resource creation, resource-to-consumer conversion, and consumer removal.
- Fixed top predators remove a roughly linear amount of prey, while self-reproducing consumers can grow superlinearly once enough food and space exist.
- Direct `DestroyTarget` is usually too opaque for ecology. Prefer pickup-plus-costed output so consumed material remains inspectable before conversion.
- Food costs, predator count, and input rate are strong knobs, but they can produce threshold behavior: extinction on one side and runaway growth on the other.
- Closing the material loop with local resources, e.g. `guano -> spore -> grub -> guano`, should usually be tested before adding more species or more predators.
- Add an intentionally playerless analysis scenario only when headless turn traces are more important than manual play. Otherwise use the standard placed player/observer loadout above.

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

The active gap log is `docs/Source of Truth/Capability-Gap-Log.md`. Keep not-yet-authorable/requested-capability tables there or in a separate referenced gap document, not in this manual. This manual should stay focused on what is currently possible.

Promote a request when repeated scenario pressure, one flagship blocked vignette, hard-to-interpret reports, or repeated authoring friction shows that new support is worth planning.

## External references

- `docs/Source of Truth/Engine-Editor-Capabilities.md`: maintainer-facing support tiers, layer coverage, runtime/editor/API parity, and detailed Action Step semantics.
- `docs/Source of Truth/Action-Step-Outcome-And-Affordance-Logic.md`: compact Action Step success/failure/fallthrough rules and actor/actee/spatial verb-affordance tables.
- `docs/Source of Truth/Capability-Gap-Log.md`: active scenario-discovered gaps, not-yet-authorable requested capabilities, workarounds, classifications, and priority signals.
- `docs/Source of Truth/planning-index.md`: documentation lanes and required reading order.
- `docs/Source of Truth/invariants.md`: Core behavior contracts and TDD traces; not normally needed for content authoring.
