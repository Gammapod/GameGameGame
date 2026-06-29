# Beta Capability Gap Log

Status: Active beta exploration log.

Read when:

- deciding whether a beta vignette should request engine, editor/API, reporting, or Console/frontend work;
- reviewing why a showcase is headless-only, partially playable, or intentionally blocked;
- promoting repeated scenario pressure into an implementation plan.

Related plans:

- `docs/Source of Truth/Content-Authoring-Manual.md`
- `docs/Source of Truth/Engine-Editor-Capabilities.md`
- `docs/Source of Truth/invariants.md`
- `docs/Plans/Beta-Content-Exploration-Plan.md`
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
