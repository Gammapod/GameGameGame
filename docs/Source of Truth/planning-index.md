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
   - Active plan for preparing current scenarios for tester feedback through scenario curation, breadcrumb/path inspection, and short-term Console frontend polish.
7. `docs/Plans/Beta-Capability-Gap-Log.md`
   - Active log for scenario-discovered beta gaps, including headless-only, Console/frontend, reporting, Action Step, and engine/system gaps.
8. `docs/Plans/Beta-Design-Quirks-and-Gotchas.md`
   - Active log for surprising, emergent, or currently-undocumented beta behavior that is not necessarily a bug or missing capability.
9. `docs/Plans/Sprint-Retrospective.md`
   - Recent process observations and open retrospective questions.
10. `docs/Archived/`
   - Historical context only. Read archived plans when current docs link to them or when investigating why an existing system was shaped a certain way.
     - Includes completed Sprint 17 scenario/tooling decoupling, Sprint 18 tech-debt cleanup, Sprint 19 Gate 4 peer-transfer, Sprint 20 scenario run/report polish, Sprint 21 Console scenario catalog, Sprint 22 Gamma containment path service plans, and the archived/paused Beta content exploration plan.

## Current strategic priority

Alpha MVP is complete and Beta produced several authored gameplay vignettes plus scenario/catalog tooling. Gamma now targets a tester-shareable frontend demo: curate current scenarios, improve Console/breadcrumb inspection enough for feedback, and use that feedback before adding more mechanics.

## Current highest-priority backlog buckets

1. Gamma tester/demo frontend readiness, as described in `docs/Plans/Gamma-Frontend-Demo-Plan.md`.
2. Scenario/testing/tooling feedback loop, especially inspection-path readability, compact summaries, recorded artifact review, and remaining non-blocking tooling polish.
3. Scenario/content packaging beyond alpha, especially curated scenario organization and manifest/index policy.
4. Future integrated game/editor frontend requirements discovered from Gamma feedback.
5. Foundational movement and peer interaction primitives, with unstarted mechanics gates deferred until feedback re-promotes them.
6. Inventory, containment, and transfer mechanics.
7. Spawning, creation, and template materialization.
8. Runtime architecture and simulation scale.
9. Behavior authoring reuse and organization.
10. Reactions and cross-entity behavior.
11. Future player control and action choice model.
12. Long-horizon diegetic/meta systems.

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
