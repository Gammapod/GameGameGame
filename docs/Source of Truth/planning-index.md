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
- Frontend UX lane: start with `docs/Source of Truth/Frontend-UX-Invariants.md` for frontend/shared-service boundaries and test traces, then `docs/Source of Truth/Frontend-UX-Standards.md` for concrete presentation and interaction guidance. Use `docs/Source of Truth/Frontend-UX-Decisions.md` for chronological rationale, `docs/Source of Truth/Frontend-Editor-Simulation-Flow.mmd` for the Editor/Simulation context diagram, and `docs/Source of Truth/Entity-Panel-UX-Spec.md` for the entity-panel/breadcrumb/log model.
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
6. `docs/Plans/Gamma-Editor-MVP-Plan.md`
   - Active Gamma release plan focused on the SadConsole Editor -> Preview -> Simulation -> Return loop. This is the next-sprint selection authority for Gamma work.
7. `docs/Plans/SadConsole-Frontend-Roadmap.md`
   - Broader roadmap for paving frontend contracts and promoting SadConsole as the canonical debug/editor browser direction.
8. `docs/Plans/Beta-Capability-Gap-Log.md`
   - Reference log for scenario-discovered beta gaps, including headless-only, Console/frontend, reporting, Action Step, and engine/system gaps. Not an active implementation plan.
9. `docs/Plans/Beta-Design-Quirks-and-Gotchas.md`
   - Reference log for surprising, emergent, or currently-undocumented beta behavior that is not necessarily a bug or missing capability.
10. `docs/Plans/Sprint-Retrospective.md`
     - Recent process observations and open retrospective questions.
11. `docs/Archived/`
   - Historical context only. Read archived plans when current docs link to them or when investigating why an existing system was shaped a certain way.
     - Includes completed Sprint 17 scenario/tooling decoupling, Sprint 18 tech-debt cleanup, Sprint 19 Gate 4 peer-transfer, Sprint 20 scenario run/report polish, Sprint 21 Console scenario catalog, Sprint 22 Gamma containment path service plans, Frontend Sprint 2 SadConsole balanced Simulation UX, the completed SadConsole UI pattern discovery sprint, the archived Gamma frontend demo plan, the archived frontend testing strategy proposal, archived SadConsole prototype/assessment plans, the archived/paused Beta content exploration plan, and historical Agent Editor API planning.

## Current strategic priority

Alpha MVP is complete and Beta produced several authored gameplay vignettes plus scenario/catalog tooling. Gamma now prioritizes the SadConsole Editor MVP: an Editor -> Preview -> Simulation -> Return loop over shared content/editor and runtime services. The former Console frontend has been removed; command-line scenario scanning policy now lives in Content through `ScenarioCatalogScanService`.

## Current highest-priority backlog buckets

1. Gamma SadConsole Editor MVP, as described in `docs/Plans/Gamma-Editor-MVP-Plan.md`.
2. SadConsole/debug-browser contract and UX follow-through, as described in `docs/Plans/SadConsole-Frontend-Roadmap.md`.
3. Scenario/testing/tooling feedback loop, especially validation, preview, compact summaries, saved history/runlog direction, and remaining tooling polish that supports the editor loop.
4. Scenario/content packaging beyond alpha, especially curated scenario organization and manifest/index policy needed by frontend scenario browsing/editing.
5. Future integrated game/editor frontend requirements discovered from SadConsole/Gamma feedback.
6. Foundational movement and peer interaction primitives, with unstarted mechanics gates deferred until feedback re-promotes them.
7. Inventory, containment, and transfer mechanics.
8. Spawning, creation, and template materialization.
9. Runtime architecture and simulation scale.
10. Behavior authoring reuse and organization.
11. Reactions and cross-entity behavior.
12. Future player control and action choice model.
13. Long-horizon diegetic/meta systems.

See `docs/Plans/High-Level-Roadmap.md` for relative priority order, dependencies, defer reasons, and promotion triggers within each bucket.

## Planning conventions

- Stable behavior contracts and test traces belong in `invariants.md`.
- Maintainer-facing capability tiers and layer support belong in `Engine-Editor-Capabilities.md`.
- Content-editor-facing authoring capabilities, workflows, limits, and gap logging belong in `Content-Authoring-Manual.md`.
- Canonical Action Step outcome and verb-affordance decision tables belong in `Action-Step-Outcome-And-Affordance-Logic.md`.
- Cross-layer implementation navigation belongs in `vertical-slice-map.md`.
- Priorities, backlog buckets, dependencies, defer reasons, and promotion triggers belong in `High-Level-Roadmap.md`.
- Active implementation details belong in an active sprint plan under `docs/Plans/`.
- Completed implementation plans should move to `docs/Archived/` and be summarized, not duplicated, in active planning docs.
- Retrospective/process observations belong in `Sprint-Retrospective.md` until a consolidated sprint workflow document supersedes scattered process notes.
- Avoid duplicating long explanations across documents; link to the authoritative doc instead.
