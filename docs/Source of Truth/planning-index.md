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
- Frontend UX lane: `docs/Source of Truth/Frontend-UX-Invariants.md`, `docs/Source of Truth/Frontend-UX-Standards.md`, `docs/Source of Truth/Frontend-UX-Decisions.md`, `docs/Source of Truth/Frontend-Editor-Simulation-Flow.mmd`, and `docs/Source of Truth/Entity-Panel-UX-Spec.md` record frontend UX constraints, handoff boundaries, UI-bible standards, decisions, diagrams, and the entity-panel/breadcrumb/log model.
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
6. `docs/Plans/Gamma-Frontend-Demo-Plan.md`
   - Supporting Gamma tester/demo context. Console-specific next work is no longer the default unless explicitly re-selected.
7. `docs/Plans/SadConsole-Frontend-Roadmap.md`
   - Active roadmap for paving frontend contracts and promoting SadConsole as the canonical debug/editor browser direction. This is the next-sprint selection authority for frontend work.
8. `docs/Plans/Beta-Capability-Gap-Log.md`
   - Reference log for scenario-discovered beta gaps, including headless-only, Console/frontend, reporting, Action Step, and engine/system gaps. Not an active implementation plan.
9. `docs/Plans/Beta-Design-Quirks-and-Gotchas.md`
   - Reference log for surprising, emergent, or currently-undocumented beta behavior that is not necessarily a bug or missing capability.
10. `docs/Plans/Sprint-Retrospective.md`
    - Recent process observations and open retrospective questions.
11. `docs/Archived/`
   - Historical context only. Read archived plans when current docs link to them or when investigating why an existing system was shaped a certain way.
     - Includes completed Sprint 17 scenario/tooling decoupling, Sprint 18 tech-debt cleanup, Sprint 19 Gate 4 peer-transfer, Sprint 20 scenario run/report polish, Sprint 21 Console scenario catalog, Sprint 22 Gamma containment path service plans, Frontend Sprint 2 SadConsole balanced Simulation UX, archived SadConsole prototype/assessment plans, the archived/paused Beta content exploration plan, and historical Agent Editor API planning.

## Current strategic priority

Alpha MVP is complete and Beta produced several authored gameplay vignettes plus scenario/catalog tooling. Gamma/frontend work now prioritizes the SadConsole frontend roadmap: first clean frontend UX docs and shared session/action/target/log/entity-panel contracts, then promote SadConsole as the canonical debug/editor browser direction. Console remains fallback/minimal tooling unless explicitly re-selected.

## Current highest-priority backlog buckets

1. SadConsole/frontend contract paving, as described in `docs/Plans/SadConsole-Frontend-Roadmap.md`.
2. Gamma tester/demo frontend readiness, using `docs/Plans/Gamma-Frontend-Demo-Plan.md` as supporting context rather than next-sprint authority.
3. Scenario/testing/tooling feedback loop, especially inspection-path readability, compact summaries, recorded artifact review, and remaining non-blocking tooling polish that supports the frontend roadmap.
4. Scenario/content packaging beyond alpha, especially curated scenario organization and manifest/index policy needed by frontend scenario browsing.
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
- Cross-layer implementation navigation belongs in `vertical-slice-map.md`.
- Priorities, backlog buckets, dependencies, defer reasons, and promotion triggers belong in `High-Level-Roadmap.md`.
- Active implementation details belong in an active sprint plan under `docs/Plans/`.
- Completed implementation plans should move to `docs/Archived/` and be summarized, not duplicated, in active planning docs.
- Retrospective/process observations belong in `Sprint-Retrospective.md` until a consolidated sprint workflow document supersedes scattered process notes.
- Avoid duplicating long explanations across documents; link to the authoritative doc instead.
