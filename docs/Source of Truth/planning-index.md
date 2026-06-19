# Planning Index

Status: Source of truth for planning-document navigation.

Read when:

- starting a planning, backlog, sprint-selection, or wrap-up session;
- deciding which planning documents are current;
- onboarding into the roadmap before implementation work.

Do not read when:

- making a narrow code/content edit whose relevant active plan is already known.

## Required reading order for planning/content work

1. `docs/Source of Truth/Engine-Editor-Capabilities.md`
   - Canonical source of truth for implemented and authorable engine/editor capabilities.
   - Trust this over roadmap or plan documents if there is a conflict.
2. `docs/Plans/High-Level-Roadmap.md`
   - Canonical source of truth for active strategic direction, prioritized backlog buckets, deferred ideas, dependencies, and promotion triggers.
3. `docs/Source of Truth/vertical-slice-map.md`
   - Cross-layer navigation map for implementation work that touches Core, Content, Editor, Agent API, GUI, tests, and docs.
   - Read selectively when a planned slice spans multiple layers.
4. Active or planned numbered sprint plan under `docs/Plans/`, when one exists
   - Current expected implementation slice and testable outcomes.
   - As of this writing: Sprint 10 is complete and archived; Sprint 11 has not yet been selected.
5. `docs/Plans/Sprint-Retrospective.md`
   - Recent process observations and open retrospective questions.
6. `docs/Archived/`
   - Historical context only. Read archived plans when current docs link to them or when investigating why an existing system was shaped a certain way.

## Current strategic priority

Prioritize the alpha release target before broad new mechanics: the game can launch and be played in an arbitrary authored scenario, and a player entity can be inserted into scenarios. Sprint 10 established the first scenario-root inventory runner and richer behavior trace feedback; Sprint 11 should begin the smallest alpha scenario package/materialization slice, adding scenario-report polish only where it directly reduces alpha risk.

## Current highest-priority backlog buckets

1. Alpha scenario package/materialization path, as described in `High-Level-Roadmap.md`.
2. Scenario/testing/tooling feedback loop.
3. Foundational movement and peer interaction primitives.
4. Inventory, containment, and transfer mechanics.
5. Spawning, creation, and template materialization.
6. Scenario/content packaging beyond alpha.
7. Runtime architecture and simulation scale.
8. Behavior authoring reuse and organization.
9. Future integrated game/editor frontend.
10. Reactions and cross-entity behavior.
11. Future player control and action choice model.
12. Long-horizon diegetic/meta systems.

See `docs/Plans/High-Level-Roadmap.md` for relative priority order, dependencies, defer reasons, and promotion triggers within each bucket.

## Planning conventions

- Capabilities and supported authoring tiers belong in `Engine-Editor-Capabilities.md`.
- Cross-layer implementation navigation belongs in `vertical-slice-map.md`.
- Priorities, backlog buckets, dependencies, defer reasons, and promotion triggers belong in `High-Level-Roadmap.md`.
- Active implementation details belong in an active sprint plan under `docs/Plans/`.
- Completed implementation plans should move to `docs/Archived/` and be summarized, not duplicated, in active planning docs.
- Retrospective/process observations belong in `Sprint-Retrospective.md` until a consolidated sprint workflow document supersedes scattered process notes.
- Avoid duplicating long explanations across documents; link to the authoritative doc instead.
