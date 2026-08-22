---
id: source.capability-gap-log
title: Capability Gap Log
kind: source-of-truth
subkind: gap-log
status: active
truth_rank: 55
truth_domains: [gap-workflow, planning-priority]
owners: [content-editor]
audience: [content-editor, core-owner, frontend-owner]
lane: content-authoring
read_when:
  - deciding whether a scenario or vignette should request engine editor/API reporting or frontend work
  - reviewing why a showcase is headless-only partially playable or intentionally blocked
  - promoting repeated scenario pressure into an implementation plan
related:
  - source.content-authoring-manual
  - source.engine-editor-capabilities
  - source.invariants
---
# Capability Gap Log

Status: Living source-of-truth log for scenario-discovered capability gaps, not an active implementation plan. New work should only be promoted from this log through the active Core, Content, or Frontend rolling board after the relevant owner prioritizes it.

Read when:

- deciding whether a scenario or vignette should request engine, editor/API, reporting, or Console/frontend work;
- reviewing why a showcase is headless-only, partially playable, or intentionally blocked;
- promoting repeated scenario pressure into an implementation plan.

Related plans:

- `docs/Source of Truth/Content-Authoring-Manual.md`
- `docs/Source of Truth/Engine-Editor-Capabilities.md`
- `docs/Source of Truth/invariants.md`
- `docs/Archived/Beta-Content-Exploration-Plan.md`
- `docs/Archived/Sprint-12-Beta-Primitive-Showcases.md`
- `docs/Archived/Sprint-14-Gate-2-Targeting-Showcases.md`
- `docs/Archived/Sprint-16-Gate-3-Distance-Movement.md`

## Open gaps

### GAP-001: `CreateFacing` placeholder entities lack content-template/presentation assignment

- **Discovered in:** Sprint 12 `beta-create-showcase`.
- **Scenario/content:** `src/GameGameGame.Content/Beta/CurrentTools/CreateFacingShowcase.yaml`, scenario `beta-create-showcase`.
- **Desired behavior:** A successful `CreateFacing` action should create an entity that remains renderable/inspectable in Console and future frontends.
- **Current behavior:** Headless scenario reports can observe the created `Placeholder Rock`, but Console rendering/inspection can crash after the action because the runtime-created `placeholderRock` entity has no content-template assignment in `PrototypeContentRegistry`.
- **Current workaround:** Treat successful `CreateFacing` as headless-only for Sprint 12 manual testing. Console-safe creation demos must avoid successful creation, which does not showcase the primitive.
- **Missing capability:** Runtime-created entities need content presentation/template binding, or `CreateFacing` should become template-backed, such as `CreateFacing(templateId)` / `SpawnTemplateFacing`.
- **Unlocks:** Console-playable create/spawner showcases; authored builders; authored traps/bombs/projectiles; summons/clones; future template-spawning vignettes.
- **Classification:** New engine/content integration capability; also a Console/frontend stability gap.
- **Priority:** Medium-high for beta if creation/spawning remains in the current-tool showcase set; otherwise promote at the template-spawning gate.

### GAP-002: Agent/headless API cannot directly run persisted scenarios by scenario ID

Status: Resolved in Sprint 20 first slice for headless/editor-agent scenario reports.

- **Discovered in:** Sprint 14 `beta-collector` follow-up.
- **Scenario/content:** `src/GameGameGame.Content/Beta/Targeting/CollectorShowcase.yaml`, scenario `beta-collector`.
- **Desired behavior:** Content authors should be able to run a persisted scenario by ID through the agent/headless API, including scenario root, player template, player entity ID, player start, action plans, and active scenario plane.
- **Current behavior:** `AgentContentEditorApi.RunScenarioById` and `ScenarioRunService.Run(... PersistedScenarioRunRequest ...)` run persisted scenarios by scenario ID using shared scenario materialization, including player insertion and materialized action plans. The older `RunScenario` root-template path remains available as root-only compatibility simulation.
- **Current workaround:** None for headless/editor-agent reports. Use root-only runs only when intentionally isolating a scenario-root template without persisted scenario setup.
- **Missing capability:** A scenario-ID run command such as `RunScenario("beta-collector", turnCount)` or equivalent request shape that uses persisted scenario materialization.
- **Unlocks:** Faster validation of player-involved vignettes; less custom fixture code; clearer evidence for Console-playable beta scenarios.
- **Classification:** Reporting/tooling request; editor/agent API workflow request.
- **Priority:** High if more player-involved beta vignettes are authored.

### GAP-003: `AcquireNearestTarget` has no authorable target filters

Status: Resolved by entity-authored `targetingRules` and numeric target slots.

- **Discovered in:** Sprint 14 targeting showcases.
- **Scenario/content:** `src/GameGameGame.Content/Beta/Targeting/*.yaml`.
- **Desired behavior:** Authors should be able to restrict target acquisition to a simple intended set, such as player, items, specific template IDs, or excluding props, without relying only on room layout.
- **Current behavior:** New canonical content should define entity-level `targetingRules` with a numeric slot, target template ID, range, and optional author hint. `TargetingService` refreshes those target slots before plan resolution, so behavior steps can consume the intended slot through `targetSlot` instead of running `AcquireNearestTarget` inside the plan.
- **Current workaround:** None for template-targeted acquisition. The legacy `AcquireNearestTarget` step remains runnable for old content, but is no longer the preferred authoring model.
- **Sprint 16 note:** Gate 3 distance-movement showcases continued to work around this by using sparse layouts, far-away player starts, and a separate one-row fallback-lane scenario for `beta-kiting-orbiter` so unfiltered acquisition would select the intended target instead of helper/blocker entities.
- **Missing capability:** Broader relationship/category targeting, target priority policies, and scenario-specific overrides remain future work if content needs them.
- **Unlocks:** More complex targeting rooms; follower/chaser variants; collector scenarios with props; target selection that can prefer player or item roles.
- **Classification:** New Action Step/primitive authoring extension using existing engine state, potentially content/runtime binding if template IDs are needed at runtime.
- **Priority:** Medium-high when targeting rooms become complex enough that sparse-layout workarounds are fragile.

### GAP-004: Scenario definitions cannot author initial action-state overrides per entity

- **Discovered in:** Sprint 14 targeting planning discussion.
- **Scenario/content:** Future targeting/follower/locked-on vignettes.
- **Desired behavior:** Scenario definitions should be able to set initial actor action state, especially `Target`, for specific placed or inserted entities.
- **Current behavior:** Entity templates can define `actionStateDefaults`, but persisted scenario definitions currently only select root/player/start data and cannot override an individual runtime entity's initial `Target` at scenario setup.
- **Current workaround:** Use `AcquireNearestTarget` or custom test setup to establish targets at runtime.
- **Missing capability:** Scenario-level action-state overrides applied during materialization, with validation for missing entity IDs, self-targets if unsupported, and references that do not exist in the materialized scenario.
- **Unlocks:** Seek-only isolation tests; locked-on hunters; followers that start targeting the player; demonstrations where the desired target is not simply the nearest entity.
- **Classification:** New scenario/materialization capability.
- **Priority:** Medium; promote when a vignette needs pre-seeded targets rather than acquisition behavior.

### GAP-005: Scenario reports do not summarize carried inventory/containment richly enough

Status: Resolved in Sprint 20 for headless/editor-agent scenario reports.

- **Discovered in:** Sprint 14 `beta-collector` follow-up.
- **Scenario/content:** `src/GameGameGame.Content/Beta/Targeting/CollectorShowcase.yaml`, scenario `beta-collector`.
- **Desired behavior:** Final reports should clearly state when carried entities end up inside another entity, including inventory coordinates, e.g. `Collector inventory: collectibleGem at (0,0), betaPlayer at (1,0)`.
- **Current behavior:** Scenario reports expose `InventorySummaryLines` with carried entity names, IDs, and inventory coordinates. Summaries recurse into nested carried inventories and guard against recursive containment cycles.
- **Current workaround:** None for report-visible carried contents in supported headless/editor-agent scenario reports. Direct state inspection remains useful for low-level engine tests.
- **Missing capability:** Broader compact world/state diffs and polished saved runlog formatting remain future report work.
- **Unlocks:** Easier review of collector, transfer, containment, give/take, and enter/exit vignettes without custom assertions or manual state inspection.
- **Classification:** Reporting/tooling request.
- **Priority:** Medium-high for inventory/containment-heavy beta work.

### GAP-006: No single preview-plus-simulate report for persisted scenarios

Status: Resolved in Sprint 20 for editor-agent persisted scenario review reports.

- **Discovered in:** Sprint 14 targeting workflow.
- **Scenario/content:** All beta scenario fixture workflows.
- **Desired behavior:** A single agent/editor workflow should combine plan preview, scenario validation/materialization, simulation trace, final state, inventory summary, and capability-gap notes for a persisted scenario.
- **Current behavior:** `AgentContentEditorApi.PreviewAndRunScenarioById` returns document validation, canonical authoring validation, action-plan previews, scenario materialization, persisted scenario run traces, final state, inventory summaries, observations/failures, and capability gaps in one report.
- **Current workaround:** None for editor-agent persisted scenario review reports. Separate preview, materialization, and run calls remain useful when authors need only one section.
- **Missing capability:** Polished saved runlog/golden-run formatting and future frontend UI presentation remain future work.
- **Unlocks:** Faster content iteration and less token-heavy/manual report interpretation.
- **Classification:** Reporting/tooling request; editor/agent API workflow request.
- **Priority:** Medium; depends on persisted-scenario run support and report-summary improvements.

### GAP-007: Root-only versus persisted-scenario simulation terminology is easy to confuse

Status: Resolved in Sprint 20 first slice for headless/editor-agent scenario report setup labels.

- **Discovered in:** Sprint 14 `beta-direct-chase` and `beta-collector` testing.
- **Scenario/content:** `src/GameGameGame.Content/Beta/Targeting/*.yaml`.
- **Desired behavior:** Agent/API commands and report text should make it obvious whether a run is root-only or uses a persisted scenario definition with player insertion.
- **Current behavior:** Root-template reports are labeled `Run mode: Root-only compatibility simulation`; persisted scenario-ID reports are labeled `Run mode: Persisted scenario simulation` and include persisted scenario setup/player lines.
- **Current workaround:** None for the supported report paths; authors should choose persisted scenario-ID runs whenever scenario setup or player insertion matters.
- **Missing capability:** Clearer command names, request shape, report labels, or documentation distinguishing root-template simulation from persisted-scenario simulation.
- **Unlocks:** Lower-friction content authoring and fewer mistaken assumptions during agent handoffs.
- **Classification:** Reporting/tooling request; API ergonomics/documentation issue.
- **Priority:** Medium; may be resolved together with GAP-002.

### GAP-008: Placed/carried entity instances cannot author per-instance initial state/facing

- **Discovered in:** SadConsole Editor parity review; reinforced by user-authored `user-ratbarn-ratcatcher` scenario.
- **Scenario/content:** `src/GameGameGame.Content/Beta/User/RatBarnScenario.yaml`, scenario `user-ratbarn-ratcatcher`; future inventory-authored actors/items where each placed or carried instance needs distinct initial facing, tags, or state.
- **Desired behavior:** Inventory/placement authorship should own per-instance state for placed/carried entities, such as initial `Facing`, and future tags/states. The same rat template should be placeable twice with different authored starting directions because facing belongs to that specific placement in the parent inventory/room, not inherently to the reusable rat template.
- **Current behavior:** Entity templates expose `actionStateDefaults`, while carried entity summaries/editing currently focus on template, coordinate, controller, glyph/color, and diagnostics. Persisted placement data does not expose authored action-state overrides, so starting `Facing` is inherited from the referenced template during materialization.
- **Current workaround:** Author separate templates when different initial defaults are required, e.g. one north-facing rat template and one west-facing rat template that otherwise share behavior and presentation.
- **Missing capability:** Shared content/editor model and frontend/agent projection/mutation support for per-placed/per-carried-instance initial state overrides, including YAML schema, validation, materialization behavior, scenario reports, and conflict/merge rules with template defaults. A possible shape is placement-local data such as `actionStateOverrides: { facing: North }`, later extendable to tags/states.
- **Unlocks:** Placing multiple instances of one template with different initial facing/state; cleaner inventory-authored room setup; less template duplication; future authoring where local scenario/room placement supplies instance-specific tags, flags, or starting state.
- **Classification:** Content/editor/materialization capability.
- **Priority:** Medium; promote when authored layouts need per-instance state.

### GAP-009: Action-step parameter editing lacks typed frontend projection/mutation design

- **Discovered in:** SadConsole Action Plan editor rebuild.
- **Scenario/content:** Action plans that need more than choosing/reordering stable behavior-step primitives.
- **Desired behavior:** Screen 4 should eventually expose typed editable parameters for highlighted action steps, with current values, allowed choices, validation, and shared mutation services.
- **Current behavior:** `FrontendEditorActionPlanStepSummary` exposes step index, kind, and display name. The rebuilt editor can insert, replace, delete, and move canonical behavior-chain steps, but does not expose check/effect/parameter editing.
- **Current workaround:** Use existing content files or lower-level editor/API tooling for parameter-level changes where available.
- **Missing capability:** A typed action-step parameter projection and mutation contract, plus UX design for how parameters fit into 4.2/4.2.x without overloading primitive replacement.
- **Unlocks:** Full action-plan authoring from SadConsole; fewer raw YAML edits; richer behavior composition.
- **Classification:** Editor/API and frontend UX capability.
- **Priority:** Medium-high once content authoring requires parameter-level edits through the frontend.

### GAP-010: Curated scenario manifest edit sessions need deliberate save/move operations

- **Discovered in:** Scenario curation request after Beta became a dumping ground.
- **Scenario/content:** Future `Scenarios/LegacyBeta`, `Scenarios/Delta`, `Scenarios/User`, and `Scenarios/Canonical` organization, plus current curated manifests.
- **Desired behavior:** Content tools should provide a first-class manifest editing session: open/create manifest, list/add/update/move sections, add/update/move entries, scan candidates, validate, snapshot diff, and save deliberately without rewriting unrelated content files.
- **Current behavior:** `ScenarioCatalog` can load/save sectioned manifests, flatten entries for existing launch consumers, scan candidates without making them authoritative, and validate paths/scenario IDs/duplicates/descriptions/status placement/unclassified candidates. `GameGameGame.Content.Tools` exposes open, candidate scan, and validation operations. Full session-backed edit/diff/save operations remain follow-up.
- **Current workaround:** Authors can curate sectioned manifests by YAML edits and use the scan/validate tools to check them before launch/browsing.
- **Missing capability:** A dedicated manifest session registry and semantic mutation tools parallel to `AgentContentEditorApi` document sessions.
- **Unlocks:** Safer frontend/user-generated scenario curation, deliberate promotion from user/delta to canonical, and reduced accidental Manifest.yaml rewrites.
- **Classification:** Content/package organization issue; editor/agent API workflow request.
- **Priority:** Medium-high if scenario browsing/editing becomes a regular content workflow.

### GAP-011: Authorable Action Step cooldowns and initial cooldown state for ecology pacing

- **Discovered in:** Pocket Bazaar ecology vignette testbed, especially `ecology-glowcap-grubarium`.
- **Scenario/content:** `src/GameGameGame.Content/Beta/Ecology/EcologyVignettes.yaml`, cave ecology experiments with fungus, spores, grubs, bats, and guano.
- **Desired behavior:** Authors should be able to pace repeated ecological actions such as spore creation, egg laying, reproduction, feeding, and recovery with a general cooldown/lockout mechanism. A possible brainstorm shape is an action step with cooldown metadata, e.g. `CreateEntity { templateId: glowcapSpore, cooldownTurns: 5 }`, where the step fails/falls through while locked out after a successful use. Newly created entities should optionally begin with authored cooldown state already active, e.g. a newborn grub cannot use its own `CreateEntity egg` step until 20 turns after creation.
- **Current behavior:** Authors can approximate handling time with `PickupTarget -> costed CreateEntity/PolymorphTarget`, inventory limits, and deterministic lifecycle phases. These approximations do not express true timed recovery, juvenile maturation, reproductive cooldowns, or starvation timers.
- **Current workaround:** Increase resource costs, add intermediate lifecycle templates, reduce initial population, or rely on spatial/inventory friction. These knobs work but can create threshold behavior: extinction on one side and runaway growth on the other.
- **Missing capability:** General per-action or per-step cooldown state; materialization support for initial cooldown state on spawned entities; validation and reporting for cooldown-gated fallthrough; frontend/editor exposure for authored cooldown metadata. Starvation and reproductive cooldown might both be modeled as specialized uses of this broader feature if actions can become available/unavailable over time.
- **Unlocks:** Stable ecology loops; age/maturation gates; spell/action pacing; factories with production rates; creatures that must recover after reproducing; stationary producers whose output rate is not one per turn; more realistic starvation/maintenance behaviors when paired with resource-consumption actions.
- **Classification:** New engine capability plus content/editor/API authoring support.
- **Priority:** High for ecology/economy authoring; promote when Pocket Bazaar ecology exhibits become a selected release target.

### GAP-012: Awareness-count and density-dependent action conditions

- **Discovered in:** Pocket Bazaar ecology vignette testbed and design discussion around density-dependent reproduction, resource seeking, and group threat response.
- **Scenario/content:** Future ecology/economy/faction vignettes; examples include grubs limiting egg laying by nearby egg count, goblins changing behavior based on number of visible gold items, and trolls attacking or fleeing based on visible human count.
- **Desired behavior:** Targeting/awareness should optionally expose multiple matching targets per authored relationship instead of only a single nearest target. Authors should then be able to gate actions on awareness counts or density thresholds. Brainstorm examples: `Goblin loves Gold` tracks all visible gold; `Seek loves` may default to the first target, while `Seek [loves] >= 2` only acts when at least two gold items are known. `Grub inhibits Egg` plus `Create [egg] <= 5` would lay eggs only while local egg density is below a threshold. `Troll fears Humans` could attack when `[fears] <= 3` and flee when `[fears] > 3`.
- **Current behavior:** Targeting profiles select the nearest matching candidate per rule/label. Authors can express nearest-target pursuit, pickup, flee, destroy, and transfer behaviors, but cannot directly ask how many relevant targets are visible/nearby or gate an action on that count.
- **Current workaround:** Use layout, resource costs, carrying capacity, or fixed initial populations to influence density indirectly. This is insufficient for local carrying capacities, crowd avoidance, morale/group fear, or density-dependent reproduction.
- **Missing capability:** Multi-target awareness arrays or counted target sets; action-step predicates over target counts and possibly local ranges; validation for threshold syntax; clear fallthrough semantics when count gates fail; report/debug projection of awareness counts so authors can tune scenarios.
- **Unlocks:** Population caps as content; local carrying capacity; group morale and intimidation; richer resource-seeking behavior; swarm avoidance; ecologies that stabilize through local density instead of global hard caps.
- **Classification:** New engine/action/targeting capability plus content/editor/API authoring support.
- **Priority:** High for ecology and faction behavior authoring; likely pairs well with cooldown support.
