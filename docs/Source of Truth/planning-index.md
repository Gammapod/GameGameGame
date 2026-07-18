# Planning Index

Status: Source of truth for planning-document navigation.

Read when:

- starting a planning, backlog, sprint-selection, or wrap-up session;
- deciding which planning documents are current;
- onboarding into the roadmap before implementation work.

Do not read when:

- making a narrow code/content edit whose relevant active plan is already known.

## Documentation lanes

- Core-owner/TDD lane: `docs/Source of Truth/invariants.md` records stable behavior contracts and test traces.
- Capability matrix lane: `docs/Source of Truth/Engine-Editor-Capabilities.md` records maintainer-facing capability support tiers and layer coverage.
- Content-authoring lane: `docs/Source of Truth/Content-Authoring-Manual.md` records content-editor-facing authoring capabilities, workflows, limits, and gap logging.
- Action logic lane: `docs/Source of Truth/Action-Step-Outcome-And-Affordance-Logic.md` records canonical Action Step outcome rules and actor/actee/spatial verb-affordance logic.
- Frontend game-text lane: `docs/Source of Truth/Frontend-Game-Text.md` records draft player-facing log message ID slots for current Action Steps, including success/failure and ratio-bucket variant IDs; final wording is intentionally deferred.
- Frontend UX lane: start with `docs/Source of Truth/Frontend-UX-Invariants.md` for frontend/shared-service boundaries and test traces, then `docs/Source of Truth/Frontend-UX-Standards.md` for concrete presentation and interaction guidance. Use `docs/Source of Truth/Frontend-UX-Decisions.md` for chronological rationale, `docs/Source of Truth/Frontend-Editor-Simulation-Flow.mmd` for the Editor/Simulation context diagram, and `docs/Source of Truth/Entity-Panel-UX-Spec.md` for the entity-panel/breadcrumb/log model.
- Glossary lane: `docs/Source of Truth/glossary.md` records shared terminology such as spatial direction and adjacency vocabulary.
- Planning lane: `docs/Plans/High-Level-Roadmap.md`, active plans, and the gap log record priorities, promotion triggers, and selected work.

## Required reading order for planning/content work

1. `docs/Source of Truth/Content-Authoring-Manual.md`
   - Source of truth for what content authors and content-editing agents can safely author today.
   - Use this before authoring scenarios or classifying content-facing gaps.
2. `docs/Source of Truth/Engine-Editor-Capabilities.md`
   - Source of truth for maintainer-facing engine/editor capability support tiers and layer coverage.
   - Trust this over roadmap or plan documents for implemented support status.
3. `docs/Source of Truth/invariants.md`
   - Source of truth for core-owner-facing TDD invariant/test traces.
   - Use this before planning changes to stable behavior.
4. `docs/Plans/High-Level-Roadmap.md`
    - Canonical source of truth for active strategic direction, prioritized backlog buckets, deferred ideas, dependencies, and promotion triggers.
5. `docs/Source of Truth/vertical-slice-map.md`
    - Cross-layer navigation map for implementation work that touches Core, Content, Editor, Agent API, GUI, tests, and docs.
    - Read selectively when a planned slice spans multiple layers.
6. `docs/Plans/Canonical-Actions-Vertical-Slice-Plan.md`
   - Active release plan focused on freezing the current Action Step catalog as legacy/prototype-compatible and promoting canonical actions through vertical slices. The canonical `Move` slice, first Pickup/Drop Action Choice interaction seam, and componentized play-mode refactor are complete enough to serve as reference workflow evidence; the next likely promotion target is `EnterTarget`/`ExitFacing`, with `Teleport` as an advanced/stretch relocation candidate.
7. `docs/Archived/Delta-Point-of-View-Release-Plan.md`
   - Archived Delta release plan focused on arbitrary-entity point-of-view: breadcrumb-backed current place, bulk/aperture ratio, frontend/content projection, and affordance/adjective groundwork. Treat as foundation/reference unless follow-up POV work is explicitly selected.
8. `docs/Plans/Gamma-Editor-MVP-Plan.md`
   - On-hold Gamma release plan focused on the SadConsole Editor -> Preview -> Simulation -> Return loop. Treat as backlog/context until future roadmap selection promotes it again.
9. `docs/Plans/SadConsole-Frontend-Roadmap.md`
    - Broader frontend backlog/reference for SadConsole/debug-browser contracts. Not the active implementation plan while canonical action vertical slices are selected, except for the componentized play-mode replacement called out by that plan and focused frontend refactor sprints.
10. `docs/Plans/Beta-Capability-Gap-Log.md`
   - Reference log for scenario-discovered beta gaps, including headless-only, Console/frontend, reporting, Action Step, and engine/system gaps. Not an active implementation plan.
11. `docs/Plans/Beta-Design-Quirks-and-Gotchas.md`
   - Reference log for surprising, emergent, or currently-undocumented beta behavior that is not necessarily a bug or missing capability.
12. `docs/Plans/Sprint-Retrospective.md`
    - Recent process observations and open retrospective questions.
13. `docs/Archived/`
   - Historical context only. Read archived plans when current docs link to them or when investigating why an existing system was shaped a certain way.
   - Includes completed Sprint 17 scenario/tooling decoupling, Sprint 18 tech-debt cleanup, Sprint 19 Gate 4 peer-transfer, Sprint 20 scenario run/report polish, Sprint 21 Console scenario catalog, Sprint 22 Gamma containment path service plans, Frontend Sprint 2 SadConsole balanced Simulation UX, the completed SadConsole UI pattern discovery sprint, the completed SadConsole frontend refactor/consolidation sprint, the completed Core refactor/consolidation sprint, the archived Gamma frontend demo plan, the archived frontend testing strategy proposal, archived SadConsole prototype/assessment plans, the archived SadConsole tile-scaling spike findings, the archived/paused Beta content exploration plan, and historical Agent Editor API planning.

## Current strategic priority

Alpha MVP is complete and Beta produced several authored gameplay vignettes plus scenario/catalog tooling. Delta established the point-of-view foundation needed for arbitrary observer/current-place/bulk-aperture/adjective projection. The former Console frontend has been removed; command-line scenario scanning policy now lives in Content through `ScenarioCatalogScanService`. The active release direction is canonical action vertical slices: freeze the current Action Step catalog as legacy/prototype-compatible, promote actions one at a time with engine rules, POV/affordance facts, frontend log IDs, content test rooms, editor support, componentized play-mode consumption, and Core-owned runtime control-source / Action Choice support for arbitrary controlled entities. The canonical `Move` slice is complete; first Pickup/Drop Action Choice target/source/destination interaction and the componentized play-mode refactor are implemented. The next priority is likely the Enter/Exit containment transition pair, while Throw/Shove-style ranged transform variants should remain backlog until the broader action vocabulary is proven.

## Current highest-priority backlog buckets

The active release plan is `docs/Plans/Canonical-Actions-Vertical-Slice-Plan.md`. Current backlog buckets remain available for reprioritization after the first canonical action and Action Choice slices clarify needs:

1. Canonical action vertical slices, as described in `docs/Plans/Canonical-Actions-Vertical-Slice-Plan.md`; next focus is likely `EnterTarget`/`ExitFacing` promotion after completed `Move` and first Pickup/Drop interaction seams.
2. Canonical runtime control-source / Action Choice model, where control source is mutable runtime state and player-controlled actors choose from their normal authored action steps; Move and first Pickup/Drop choice submission are implemented, with full pre/main/post descriptor composition, target-first menus, and richer choice DTO fields remaining follow-up.
3. Componentized Gamma play-mode follow-through, consuming canonical action/Action Choice/POV/log services; the action-step-first componentized play path is implemented, with remaining work focused on polish, broader action coverage, and demoting/removing any internal legacy stopgap paths.
4. Delta point-of-view follow-through where needed by canonical actions: affordance adjectives, reciprocal awareness, ratio facts, and presentation polish.
5. Gamma SadConsole Editor MVP, as described in `docs/Plans/Gamma-Editor-MVP-Plan.md`.
6. SadConsole/debug-browser contract and UX follow-through, as described in `docs/Plans/SadConsole-Frontend-Roadmap.md`; completed frontend refactor sprint follow-up candidates include optional legacy shell deletion, further `SadConsoleEditorContext` decomposition, `GameplayFrameBuilder`, and cautious mutation-executor expansion.
7. Scenario/testing/tooling feedback loop, especially validation, preview, compact summaries, saved history/runlog direction, and remaining tooling polish that supports canonical action rooms.
8. Scenario/content packaging beyond alpha, especially curated scenario organization and manifest/index policy needed by frontend scenario browsing/editing.
9. Foundational movement and peer interaction primitives, with unstarted mechanics gates deferred until feedback re-promotes them.
10. Inventory, containment, and transfer mechanics.
11. Spawning, creation, and template materialization.
12. Runtime architecture and simulation scale.
13. Behavior authoring reuse and organization.
14. Reactions and cross-entity behavior.
15. Long-horizon diegetic/meta systems.

See `docs/Plans/High-Level-Roadmap.md` for relative priority order, dependencies, defer reasons, and promotion triggers within each bucket.

## Planning conventions

- Stable behavior contracts and test traces belong in `invariants.md`.
- Maintainer-facing capability tiers and layer support belong in `Engine-Editor-Capabilities.md`.
- Content-editor-facing authoring capabilities, workflows, limits, and gap logging belong in `Content-Authoring-Manual.md`.
- Canonical Action Step outcome and verb-affordance decision tables belong in `Action-Step-Outcome-And-Affordance-Logic.md`.
- Cross-layer implementation navigation belongs in `vertical-slice-map.md`.
- Priorities, backlog buckets, dependencies, defer reasons, and promotion triggers belong in `High-Level-Roadmap.md`.
- Active implementation details belong in an active sprint/release plan under `docs/Plans/`; currently that plan is `Canonical-Actions-Vertical-Slice-Plan.md`.
- Completed implementation plans should move to `docs/Archived/` and be summarized, not duplicated, in active planning docs.
- Retrospective/process observations belong in `Sprint-Retrospective.md` until a consolidated sprint workflow document supersedes scattered process notes.
- Avoid duplicating long explanations across documents; link to the authoritative doc instead.
